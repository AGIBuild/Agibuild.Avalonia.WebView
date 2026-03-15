using Agibuild.Fulora;

namespace AvaloniAiChat.Bridge.Services;

/// <summary>
/// Result of reading a dropped file.
/// </summary>
public sealed class DroppedFileResult
{
    /// <summary>
    /// Original file name.
    /// </summary>
    public string FileName { get; init; } = "";

    /// <summary>
    /// UTF-8 text content read from the dropped file.
    /// </summary>
    public string Content { get; init; } = "";
}

/// <summary>
/// Current model/runtime state exposed to the web UI.
/// </summary>
public sealed class AiModelState
{
    /// <summary>
    /// Resolved AI endpoint URL.
    /// </summary>
    public string Endpoint { get; init; } = "";

    /// <summary>
    /// Required model name (for example, qwen2.5:3b).
    /// </summary>
    public string RequiredModel { get; init; } = "";

    /// <summary>
    /// Indicates whether the required model exists locally.
    /// </summary>
    public bool IsModelAvailable { get; init; }

    /// <summary>
    /// Indicates whether a model download is in progress.
    /// </summary>
    public bool IsDownloading { get; init; }

    /// <summary>
    /// Indicates whether Ollama runtime is installed.
    /// </summary>
    public bool IsOllamaInstalled { get; init; }

    /// <summary>
    /// Indicates whether Ollama service is reachable.
    /// </summary>
    public bool IsOllamaRunning { get; init; }

    /// <summary>
    /// Recommended next setup step for the UI.
    /// </summary>
    public string NextStep { get; init; } = "";

    /// <summary>
    /// Installation page URL for Ollama.
    /// </summary>
    public string InstallUrl { get; init; } = "https://ollama.com/download";

    /// <summary>
    /// Suggested command to start the Ollama service.
    /// </summary>
    public string StartCommand { get; init; } = "ollama serve";

    /// <summary>
    /// Current download stage label.
    /// </summary>
    public string DownloadStage { get; init; } = "";

    /// <summary>
    /// Download progress in percentage [0,100].
    /// </summary>
    public int DownloadProgressPercent { get; init; }

    /// <summary>
    /// Human-readable status message.
    /// </summary>
    public string StatusMessage { get; init; } = "";
}

/// <summary>
/// AI chat service exposed to JavaScript. Streaming completion returns tokens one-by-one
/// via <see cref="IAsyncEnumerable{T}"/> which maps to JS <c>AsyncIterable&lt;string&gt;</c>.
/// </summary>
[JsExport]
public interface IAiChatService
{
    /// <summary>
    /// Streams AI completion tokens for the given prompt.
    /// </summary>
    /// <param name="prompt">User input prompt.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Token stream as async enumerable.</returns>
    IAsyncEnumerable<string> StreamCompletion(string prompt, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets current backend display information.
    /// </summary>
    Task<string> GetBackendInfo();

    /// <summary>
    /// Reads the latest dropped file content, if any.
    /// </summary>
    Task<DroppedFileResult?> ReadDroppedFile();

    /// <summary>
    /// Streams dropped files from desktop to web UI.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Dropped file stream.</returns>
    IAsyncEnumerable<DroppedFileResult> StreamDroppedFiles(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets current model state snapshot.
    /// </summary>
    Task<AiModelState> GetModelState();

    /// <summary>
    /// Streams model state updates.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Model state stream.</returns>
    IAsyncEnumerable<AiModelState> StreamModelState(CancellationToken cancellationToken = default);

    /// <summary>
    /// Downloads the required model and returns final state.
    /// </summary>
    Task<AiModelState> DownloadRequiredModel();
}
