using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using AvaloniAiChat.Bridge.Services;
using Microsoft.Extensions.AI;

namespace AvaloniAiChat.Desktop;

/// <summary>
/// AI chat service that wraps <see cref="IChatClient"/> to stream LLM responses
/// token-by-token via <see cref="IAsyncEnumerable{T}"/>.
/// </summary>
public sealed class AiChatService(
    IChatClient chatClient,
    string backendName,
    Uri ollamaEndpoint,
    string requiredModel,
    bool useEchoMode) : IAiChatService
{
    private string? _lastDroppedFilePath;
    private readonly HttpClient _pullHttpClient = new() { Timeout = Timeout.InfiniteTimeSpan };
    private readonly HttpClient _probeHttpClient = new() { Timeout = TimeSpan.FromSeconds(4) };
    private readonly object _downloadGate = new();
    private readonly SemaphoreSlim _modelStateSignal = new(0, 256);
    private readonly SemaphoreSlim _droppedFileSignal = new(0, 32);
    private Task? _downloadTask;
    private string _downloadStatus = "";
    private string _downloadStage = "idle";
    private int _downloadProgressPercent;
    private string? _downloadError;
    private bool? _ollamaInstalledCache;
    private DateTimeOffset _ollamaInstalledCheckedAt = DateTimeOffset.MinValue;

    public async IAsyncEnumerable<string> StreamCompletion(
        string prompt,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ChatMessage[] messages = [new(ChatRole.User, prompt)];
        await using var iterator = chatClient
            .GetStreamingResponseAsync(messages, cancellationToken: cancellationToken)
            .GetAsyncEnumerator(cancellationToken);

        while (true)
        {
            bool moved;
            ChatResponseUpdate update;
            try
            {
                moved = await iterator.MoveNextAsync();
                if (!moved)
                    break;
                update = iterator.Current;
            }
            catch (Exception ex) when (!useEchoMode && IsModelMissingError(ex))
            {
                throw new InvalidOperationException(
                    $"AI model '{requiredModel}' is missing. Open settings and download the model before retrying.",
                    ex);
            }

            if (update.Text is { Length: > 0 } text)
                yield return text;
        }
    }

    public Task<string> GetBackendInfo() => Task.FromResult(backendName);

    /// <summary>
    /// Called by MainWindow when a native file drop occurs.
    /// </summary>
    internal void SetDroppedFile(string path)
    {
        _lastDroppedFilePath = path;
        NotifyDroppedFileChanged();
    }

    public Task<DroppedFileResult?> ReadDroppedFile()
    {
        var path = Interlocked.Exchange(ref _lastDroppedFilePath, null);
        if (path is null || !File.Exists(path))
            return Task.FromResult<DroppedFileResult?>(null);

        var content = File.ReadAllText(path);
        var fileName = Path.GetFileName(path);
        return Task.FromResult<DroppedFileResult?>(new DroppedFileResult
        {
            FileName = fileName,
            Content = content
        });
    }

    public async IAsyncEnumerable<DroppedFileResult> StreamDroppedFiles(
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await _droppedFileSignal.WaitAsync(cancellationToken);
            }
            catch (OperationCanceledException)
            {
                yield break;
            }

            var dropped = await ReadDroppedFile();
            if (dropped is not null)
                yield return dropped;
        }
    }

    public async Task<AiModelState> GetModelState()
    {
        if (useEchoMode)
        {
            return new AiModelState
            {
                Endpoint = "echo://local",
                RequiredModel = "echo-demo",
                IsModelAvailable = true,
                IsDownloading = false,
                IsOllamaInstalled = true,
                IsOllamaRunning = true,
                NextStep = "ready",
                DownloadStage = "ready",
                DownloadProgressPercent = 100,
                StatusMessage = "Echo mode does not require model download."
            };
        }

        var probe = await ProbeOllamaAsync(requiredModel);
        var available = probe.ModelAvailable;
        var downloading = IsDownloading();
        string status;
        string stage;
        int progress;
        lock (_downloadGate)
        {
            stage = _downloadStage;
            progress = _downloadProgressPercent;
            status = _downloadStatus;
        }

        if (downloading)
        {
            if (status.Length == 0)
                status = $"Downloading model '{requiredModel}'...";
        }
        else if (!probe.Installed)
        {
            stage = "missing-runtime";
            progress = 0;
            status = "Ollama is not installed. Install Ollama, start it, then download the model.";
        }
        else if (!probe.Running)
        {
            stage = "service-offline";
            progress = 0;
            status = "Ollama is installed but not running. Start it (ollama serve) then download the model.";
        }
        else if (available)
        {
            stage = "ready";
            progress = 100;
            status = $"Model '{requiredModel}' is ready.";
        }
        else
        {
            lock (_downloadGate)
            {
                if (!string.IsNullOrWhiteSpace(_downloadError))
                {
                    stage = "failed";
                    progress = Math.Clamp(progress, 0, 99);
                    status = $"Last download failed: {_downloadError}";
                }
                else
                {
                    stage = "missing";
                    progress = 0;
                    status = $"Model '{requiredModel}' is missing. Open settings to download.";
                }
            }
        }

        return new AiModelState
        {
            Endpoint = ollamaEndpoint.ToString(),
            RequiredModel = requiredModel,
            IsModelAvailable = available,
            IsDownloading = downloading,
            IsOllamaInstalled = probe.Installed,
            IsOllamaRunning = probe.Running,
            NextStep = ResolveNextStep(probe.Installed, probe.Running, available, downloading),
            DownloadStage = stage,
            DownloadProgressPercent = progress,
            StatusMessage = status
        };
    }

    public async IAsyncEnumerable<AiModelState> StreamModelState(
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var current = await GetModelState();
        var lastSignature = BuildModelStateSignature(current);
        yield return current;

        while (!cancellationToken.IsCancellationRequested)
        {
            var waitInterval = current.IsDownloading
                ? TimeSpan.FromSeconds(1)
                : current.IsModelAvailable
                    ? TimeSpan.FromSeconds(12)
                    : TimeSpan.FromSeconds(4);
            try
            {
                _ = await _modelStateSignal.WaitAsync(waitInterval, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                yield break;
            }

            current = await GetModelState();
            var signature = BuildModelStateSignature(current);
            if (!string.Equals(signature, lastSignature, StringComparison.Ordinal))
            {
                lastSignature = signature;
                yield return current;
            }
        }
    }

    public async Task<AiModelState> DownloadRequiredModel()
    {
        if (useEchoMode)
            return await GetModelState();

        if (IsDownloading())
            return await GetModelState();

        var probe = await ProbeOllamaAsync(requiredModel);
        if (!probe.Installed)
        {
            UpdateDownloadState(
                "missing-runtime",
                0,
                "Ollama is not installed. Install Ollama from https://ollama.com/download first.",
                "Ollama runtime is missing.");
            return await GetModelState();
        }

        if (!probe.Running)
        {
            UpdateDownloadState(
                "service-offline",
                0,
                "Ollama service is not running. Start it with `ollama serve` and retry.",
                "Ollama service is offline.");
            return await GetModelState();
        }

        if (probe.ModelAvailable)
            return await GetModelState();

        lock (_downloadGate)
        {
            _downloadError = null;
            _downloadStage = "queued";
            _downloadProgressPercent = 0;
            _downloadStatus = $"Queued model download for '{requiredModel}'.";
            _downloadTask = Task.Run(() => PullModelInternalAsync(requiredModel));
        }
        NotifyModelStateChanged();
        return await GetModelState();
    }

    private bool IsDownloading()
    {
        lock (_downloadGate)
            return _downloadTask is { IsCompleted: false };
    }

    private async Task PullModelInternalAsync(string model)
    {
        var requestPayload = JsonSerializer.Serialize(new
        {
            name = model,
            stream = true
        });
        try
        {
            UpdateDownloadState("starting", 0, $"Starting download for '{model}'...", null);
            using var request = new HttpRequestMessage(HttpMethod.Post, new Uri(ollamaEndpoint, "api/pull"))
            {
                Content = new StringContent(requestPayload, Encoding.UTF8, "application/json")
            };
            using var response = await _pullHttpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
            if (!response.IsSuccessStatusCode)
            {
                var payload = await response.Content.ReadAsStringAsync();
                UpdateDownloadState("failed", _downloadProgressPercent, $"Download failed: {response.StatusCode}", payload);
                return;
            }

            await using var stream = await response.Content.ReadAsStreamAsync();
            using var reader = new StreamReader(stream, Encoding.UTF8);
            while (true)
            {
                var line = await reader.ReadLineAsync();
                if (line is null)
                    break;
                if (string.IsNullOrWhiteSpace(line))
                    continue;

                JsonDocument? doc = null;
                try
                {
                    doc = JsonDocument.Parse(line);
                }
                catch
                {
                    continue;
                }

                using (doc)
                {
                    if (doc.RootElement.TryGetProperty("error", out var errorNode))
                    {
                        var errorText = errorNode.GetString() ?? "Unknown model pull failure.";
                        UpdateDownloadState("failed", _downloadProgressPercent, $"Download failed: {errorText}", errorText);
                        return;
                    }

                    var status = doc.RootElement.TryGetProperty("status", out var statusNode)
                        ? statusNode.GetString() ?? "downloading"
                        : "downloading";
                    var completed = ReadLong(doc.RootElement, "completed");
                    var total = ReadLong(doc.RootElement, "total");
                    var stage = ResolveDownloadStage(status);
                    var percent = ResolveDownloadProgress(status, completed, total);
                    var statusMessage = percent > 0
                        ? $"{status} ({percent}%)"
                        : status;

                    UpdateDownloadState(stage, percent, statusMessage, null);
                }
            }

            var available = await CheckModelExistsAsync(model);
            if (!available)
            {
                UpdateDownloadState("failed", _downloadProgressPercent, "Model pull finished but model was not detected.", "Model not present after pull.");
                return;
            }

            UpdateDownloadState("ready", 100, $"Model '{model}' downloaded and ready.", null);
        }
        catch (Exception ex)
        {
            var endpointError = $"Cannot connect to Ollama endpoint '{ollamaEndpoint}'. {ex.Message}";
            UpdateDownloadState("failed", _downloadProgressPercent, endpointError, endpointError);
        }
    }

    private async Task<OllamaProbeResult> ProbeOllamaAsync(string model)
    {
        var installed = await CheckOllamaInstalledCachedAsync();
        if (!installed)
            return new OllamaProbeResult(false, false, false);

        var running = await CheckOllamaReachableAsync();
        if (!running)
            return new OllamaProbeResult(true, false, false);

        var exists = await CheckModelExistsAsync(model);
        return new OllamaProbeResult(true, true, exists);
    }

    private async Task<bool> CheckOllamaInstalledCachedAsync()
    {
        lock (_downloadGate)
        {
            if (_ollamaInstalledCache.HasValue
                && (DateTimeOffset.UtcNow - _ollamaInstalledCheckedAt) < TimeSpan.FromSeconds(15))
            {
                return _ollamaInstalledCache.Value;
            }
        }

        var installed = await CheckOllamaInstalledAsync();
        lock (_downloadGate)
        {
            _ollamaInstalledCache = installed;
            _ollamaInstalledCheckedAt = DateTimeOffset.UtcNow;
        }
        return installed;
    }

    private static async Task<bool> CheckOllamaInstalledAsync()
    {
        try
        {
            using var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "ollama",
                    Arguments = "--version",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                }
            };
            if (!process.Start())
                return false;

            var waitTask = process.WaitForExitAsync();
            var finished = await Task.WhenAny(waitTask, Task.Delay(2000)) == waitTask;
            if (!finished)
            {
                try { process.Kill(entireProcessTree: true); } catch { /* ignore */ }
                return false;
            }

            return process.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }

    private async Task<bool> CheckOllamaReachableAsync()
    {
        try
        {
            var tagsUri = new Uri(ollamaEndpoint, "api/tags");
            using var request = new HttpRequestMessage(HttpMethod.Get, tagsUri);
            using var response = await _probeHttpClient.SendAsync(request);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    private async Task<bool> CheckModelExistsAsync(string model)
    {
        try
        {
            var tagsUri = new Uri(ollamaEndpoint, "api/tags");
            var json = await _probeHttpClient.GetStringAsync(tagsUri);
            using var doc = JsonDocument.Parse(json);
            if (!doc.RootElement.TryGetProperty("models", out var models) || models.ValueKind != JsonValueKind.Array)
                return false;

            foreach (var item in models.EnumerateArray())
            {
                var name = item.TryGetProperty("name", out var nameElement)
                    ? nameElement.GetString()
                    : null;
                if (string.Equals(name, model, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
        }
        catch
        {
            return false;
        }

        return false;
    }

    private static bool IsModelMissingError(Exception ex)
    {
        var text = ex.ToString();
        return text.Contains("model", StringComparison.OrdinalIgnoreCase)
               && (text.Contains("not found", StringComparison.OrdinalIgnoreCase)
                   || text.Contains("pull", StringComparison.OrdinalIgnoreCase));
    }

    private void UpdateDownloadState(string stage, int progressPercent, string statusMessage, string? error)
    {
        lock (_downloadGate)
        {
            _downloadStage = stage;
            _downloadProgressPercent = Math.Clamp(progressPercent, 0, 100);
            _downloadStatus = statusMessage;
            _downloadError = error;
        }
        NotifyModelStateChanged();
    }

    private int ResolveDownloadProgress(string status, long completed, long total)
    {
        if (total > 0)
            return Math.Clamp((int)Math.Round(completed * 100d / total), 0, 100);

        var lowered = status.ToLowerInvariant();
        lock (_downloadGate)
        {
            if (lowered.Contains("success"))
                return 100;
            if (lowered.Contains("verifying"))
                return Math.Max(_downloadProgressPercent, 92);
            if (lowered.Contains("writing"))
                return Math.Max(_downloadProgressPercent, 85);
            if (lowered.Contains("pulling") || lowered.Contains("downloading"))
                return Math.Min(95, Math.Max(_downloadProgressPercent, 5) + 1);
            return _downloadProgressPercent;
        }
    }

    private static string ResolveDownloadStage(string status)
    {
        var lowered = status.ToLowerInvariant();
        if (lowered.Contains("success"))
            return "ready";
        if (lowered.Contains("verify"))
            return "verifying";
        if (lowered.Contains("pulling manifest"))
            return "resolving";
        if (lowered.Contains("pulling") || lowered.Contains("download"))
            return "downloading";
        return "processing";
    }

    private static string ResolveNextStep(bool installed, bool running, bool available, bool downloading)
    {
        if (downloading)
            return "downloading";
        if (!installed)
            return "install-ollama";
        if (!running)
            return "start-ollama";
        if (!available)
            return "download-model";
        return "ready";
    }

    private static long ReadLong(JsonElement root, string propertyName)
    {
        if (!root.TryGetProperty(propertyName, out var element))
            return 0;
        if (element.ValueKind == JsonValueKind.Number && element.TryGetInt64(out var value))
            return value;
        if (element.ValueKind == JsonValueKind.String && long.TryParse(element.GetString(), out value))
            return value;
        return 0;
    }

    private static string BuildModelStateSignature(AiModelState state)
        => $"{state.IsOllamaInstalled}|{state.IsOllamaRunning}|{state.IsModelAvailable}|{state.IsDownloading}|{state.DownloadStage}|{state.DownloadProgressPercent}|{state.StatusMessage}|{state.NextStep}";

    private void NotifyModelStateChanged()
    {
        try
        {
            _modelStateSignal.Release();
        }
        catch (SemaphoreFullException)
        {
            // Burst updates can exceed buffer; latest state is still available via GetModelState().
        }
    }

    private void NotifyDroppedFileChanged()
    {
        try
        {
            _droppedFileSignal.Release();
        }
        catch (SemaphoreFullException)
        {
            // Burst updates can exceed buffer; the latest file path is still retained.
        }
    }

    private readonly record struct OllamaProbeResult(
        bool Installed,
        bool Running,
        bool ModelAvailable);
}
