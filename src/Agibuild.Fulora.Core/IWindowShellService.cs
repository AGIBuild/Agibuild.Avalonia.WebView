using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Agibuild.Fulora;

/// <summary>
/// Framework-level shell-window service contract. Provides snapshot, update, and stream
/// access to the global shell-window state (theme, transparency, chrome metrics).
/// </summary>
[JsExport]
public interface IWindowShellService
{
    Task<WindowShellState> GetWindowShellState();
    Task<WindowShellState> UpdateWindowShellSettings(WindowShellSettings settings);
    IAsyncEnumerable<WindowShellState> StreamWindowShellState(CancellationToken cancellationToken = default);
}
