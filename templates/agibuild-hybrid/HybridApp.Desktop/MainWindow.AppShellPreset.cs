using System.IO;
using Agibuild.Avalonia.WebView;
using Agibuild.Avalonia.WebView.Shell;
using Avalonia.Input;
using HybridApp.Bridge;

namespace HybridApp.Desktop;

public partial class MainWindow
{
    private EventHandler<KeyEventArgs>? _shortcutHandler;
    private WebViewShellExperience? _shell;
    private DesktopHostService? _desktopHostService;

    partial void InitializeShellPreset()
    {
        if (_shell is not null)
            return;

        // App-shell preset: provide common host-side shell governance defaults.
        var capabilityBridge = new WebViewHostCapabilityBridge(
            new TemplateHostCapabilityProvider(),
            new TemplateHostCapabilityPolicy());
        _shell = new WebViewShellExperience(WebView, new WebViewShellExperienceOptions
        {
            NewWindowPolicy = new NavigateInPlaceNewWindowPolicy(),
            PermissionPolicy = new DelegatePermissionPolicy((_, e) =>
            {
                if (e.PermissionKind == WebViewPermissionKind.Notifications)
                    e.State = PermissionState.Deny;
            }),
            DownloadPolicy = new DelegateDownloadPolicy((_, e) =>
            {
                if (e.Cancel || !string.IsNullOrWhiteSpace(e.DownloadPath))
                    return;

                var fileName = string.IsNullOrWhiteSpace(e.SuggestedFileName) ? "download.bin" : e.SuggestedFileName!;
                e.DownloadPath = Path.Combine(Path.GetTempPath(), fileName);
            }),
            HostCapabilityBridge = capabilityBridge
        });
        _desktopHostService = new DesktopHostService(_shell);

        _shortcutHandler = async (_, e) =>
        {
            if (_shell is null)
                return;

            var handled = await TryHandleShellShortcutAsync(_shell, e);
            if (handled)
            {
                e.Handled = true;
            }
        };

        KeyDown += _shortcutHandler;
    }

    partial void RegisterShellPresetBridgeServices()
    {
        if (_desktopHostService is null)
            throw new InvalidOperationException("Shell preset must be initialized before registering shell bridge services.");

        WebView.Bridge.Expose<IDesktopHostService>(_desktopHostService);
    }

    partial void DisposeShellPreset()
    {
        if (_shortcutHandler is not null)
            KeyDown -= _shortcutHandler;

        _shortcutHandler = null;
        _desktopHostService = null;

        _shell?.Dispose();
        _shell = null;
    }

    private static async Task<bool> TryHandleShellShortcutAsync(WebViewShellExperience shell, KeyEventArgs e)
    {
        var primaryModifier = OperatingSystem.IsMacOS() ? KeyModifiers.Meta : KeyModifiers.Control;
        var normalizedModifiers = e.KeyModifiers & (KeyModifiers.Control | KeyModifiers.Shift | KeyModifiers.Alt | KeyModifiers.Meta);

        if (e.Key == Key.F12 || (e.Key == Key.I && normalizedModifiers == (primaryModifier | KeyModifiers.Shift)))
            return await shell.OpenDevToolsAsync();

        if (normalizedModifiers == primaryModifier)
        {
            return e.Key switch
            {
                Key.C => await shell.ExecuteCommandAsync(WebViewCommand.Copy),
                Key.X => await shell.ExecuteCommandAsync(WebViewCommand.Cut),
                Key.V => await shell.ExecuteCommandAsync(WebViewCommand.Paste),
                Key.A => await shell.ExecuteCommandAsync(WebViewCommand.SelectAll),
                Key.Z => await shell.ExecuteCommandAsync(WebViewCommand.Undo),
                Key.Y => await shell.ExecuteCommandAsync(WebViewCommand.Redo),
                _ => false
            };
        }

        if (e.Key == Key.Z && normalizedModifiers == (primaryModifier | KeyModifiers.Shift))
            return await shell.ExecuteCommandAsync(WebViewCommand.Redo);

        return false;
    }

    private sealed class DesktopHostService : IDesktopHostService
    {
        private readonly WebViewShellExperience _shell;

        public DesktopHostService(WebViewShellExperience shell)
        {
            _shell = shell;
        }

        public Task<DesktopClipboardProbeResult> ReadClipboardText()
        {
            var result = _shell.ReadClipboardText();
            return Task.FromResult(new DesktopClipboardProbeResult
            {
                Outcome = MapOutcome(result.Outcome),
                ClipboardText = result.Value,
                DenyReason = result.DenyReason,
                Error = result.Error?.Message
            });
        }

        public Task<DesktopClipboardWriteResult> WriteClipboardText(string text)
        {
            var result = _shell.WriteClipboardText(text);
            return Task.FromResult(new DesktopClipboardWriteResult
            {
                Outcome = MapOutcome(result.Outcome),
                DenyReason = result.DenyReason,
                Error = result.Error?.Message
            });
        }

        private static DesktopCapabilityOutcome MapOutcome(WebViewHostCapabilityCallOutcome outcome)
            => outcome switch
            {
                WebViewHostCapabilityCallOutcome.Allow => DesktopCapabilityOutcome.Allow,
                WebViewHostCapabilityCallOutcome.Deny => DesktopCapabilityOutcome.Deny,
                WebViewHostCapabilityCallOutcome.Failure => DesktopCapabilityOutcome.Failure,
                _ => DesktopCapabilityOutcome.Failure
            };
    }

    private sealed class TemplateHostCapabilityPolicy : IWebViewHostCapabilityPolicy
    {
        public WebViewHostCapabilityDecision Evaluate(in WebViewHostCapabilityRequestContext context)
            => context.Operation switch
            {
                WebViewHostCapabilityOperation.ClipboardReadText => WebViewHostCapabilityDecision.Allow(),
                WebViewHostCapabilityOperation.ClipboardWriteText => WebViewHostCapabilityDecision.Allow(),
                _ => WebViewHostCapabilityDecision.Deny("template-capability-not-enabled")
            };
    }

    private sealed class TemplateHostCapabilityProvider : IWebViewHostCapabilityProvider
    {
        private string? _clipboardText;

        public string? ReadClipboardText()
            => _clipboardText;

        public void WriteClipboardText(string text)
            => _clipboardText = text;

        public WebViewFileDialogResult ShowOpenFileDialog(WebViewOpenFileDialogRequest request)
            => throw new NotSupportedException("Open file dialog is not enabled in this template preset.");

        public WebViewFileDialogResult ShowSaveFileDialog(WebViewSaveFileDialogRequest request)
            => throw new NotSupportedException("Save file dialog is not enabled in this template preset.");

        public void OpenExternal(Uri uri)
            => throw new NotSupportedException("External open is not enabled in this template preset.");

        public void ShowNotification(WebViewNotificationRequest request)
            => throw new NotSupportedException("Notification is not enabled in this template preset.");
    }
}
