using Agibuild.Fulora;

namespace AvaloniAiChat.Bridge.Services;

/// <summary>
/// Result of reading a dropped file.
/// </summary>
public sealed class DroppedFileResult
{
    public string FileName { get; init; } = "";
    public string Content { get; init; } = "";
}

public sealed class AiModelState
{
    public string Endpoint { get; init; } = "";
    public string RequiredModel { get; init; } = "";
    public bool IsModelAvailable { get; init; }
    public bool IsDownloading { get; init; }
    public bool IsOllamaInstalled { get; init; }
    public bool IsOllamaRunning { get; init; }
    public string NextStep { get; init; } = "";
    public string InstallUrl { get; init; } = "https://ollama.com/download";
    public string StartCommand { get; init; } = "ollama serve";
    public string DownloadStage { get; init; } = "";
    public int DownloadProgressPercent { get; init; }
    public string StatusMessage { get; init; } = "";
}

/// <summary>
/// AI chat service exposed to JavaScript. Streaming completion returns tokens one-by-one
/// via <see cref="IAsyncEnumerable{T}"/> which maps to JS <c>AsyncIterable&lt;string&gt;</c>.
/// </summary>
[JsExport]
public interface IAiChatService
{
    IAsyncEnumerable<string> StreamCompletion(string prompt, CancellationToken cancellationToken = default);

    Task<string> GetBackendInfo();

    Task<DroppedFileResult?> ReadDroppedFile();
    IAsyncEnumerable<DroppedFileResult> StreamDroppedFiles(CancellationToken cancellationToken = default);

    Task<AiModelState> GetModelState();

    IAsyncEnumerable<AiModelState> StreamModelState(CancellationToken cancellationToken = default);

    Task<AiModelState> DownloadRequiredModel();
}
