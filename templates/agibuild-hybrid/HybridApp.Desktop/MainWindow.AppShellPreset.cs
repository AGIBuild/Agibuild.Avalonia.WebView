using System.Collections.Generic;
using System.IO;
using System.Linq;
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
            MenuPruningPolicy = new DelegateMenuPruningPolicy((_, context) =>
            {
                // Template policy keeps top-level menu compact and deterministic.
                var topLevel = context.RequestedMenuModel.Items
                    .Where(item => !string.IsNullOrWhiteSpace(item.Id))
                    .GroupBy(item => item.Id, StringComparer.Ordinal)
                    .Select(group => group.First())
                    .Take(4)
                    .ToArray();
                return WebViewMenuPruningDecision.Allow(new WebViewMenuModelRequest
                {
                    Items = topLevel
                });
            }),
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
        _desktopHostService?.Dispose();
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

    private sealed class DesktopHostService : IDesktopHostService, IDisposable
    {
        private readonly WebViewShellExperience _shell;
        private readonly Queue<DesktopSystemIntegrationEvent> _inboundEvents = new();
        private readonly object _eventsLock = new();

        public DesktopHostService(WebViewShellExperience shell)
        {
            _shell = shell;
            _shell.SystemIntegrationEventReceived += OnSystemIntegrationEventReceived;
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

        public Task<DesktopMenuApplyResult> ApplyMenuModel(DesktopMenuModel model)
        {
            ArgumentNullException.ThrowIfNull(model);
            var request = new WebViewMenuModelRequest
            {
                Items = model.Items.Select(MapMenuItem).ToArray()
            };
            var result = _shell.ApplyMenuModel(request);
            if (result.Outcome == WebViewHostCapabilityCallOutcome.Allow)
            {
                _ = _shell.PublishSystemIntegrationEvent(new WebViewSystemIntegrationEventRequest
                {
                    Kind = WebViewSystemIntegrationEventKind.MenuItemInvoked,
                    ItemId = request.Items.FirstOrDefault()?.Id,
                    Context = "template-menu-applied"
                });
            }
            return Task.FromResult(new DesktopMenuApplyResult
            {
                Outcome = MapOutcome(result.Outcome),
                AppliedTopLevelItems = request.Items.Count,
                DenyReason = result.DenyReason,
                Error = result.Error?.Message
            });
        }

        public Task<DesktopTrayUpdateResult> UpdateTrayState(DesktopTrayState state)
        {
            ArgumentNullException.ThrowIfNull(state);
            var result = _shell.UpdateTrayState(new WebViewTrayStateRequest
            {
                IsVisible = state.IsVisible,
                Tooltip = state.Tooltip,
                IconPath = state.IconPath
            });
            if (result.Outcome == WebViewHostCapabilityCallOutcome.Allow)
            {
                _ = _shell.PublishSystemIntegrationEvent(new WebViewSystemIntegrationEventRequest
                {
                    Kind = WebViewSystemIntegrationEventKind.TrayInteracted,
                    ItemId = state.IsVisible ? "tray-visible" : "tray-hidden",
                    Context = state.Tooltip
                });
            }
            return Task.FromResult(new DesktopTrayUpdateResult
            {
                Outcome = MapOutcome(result.Outcome),
                IsVisible = state.IsVisible,
                DenyReason = result.DenyReason,
                Error = result.Error?.Message
            });
        }

        public Task<DesktopSystemActionResult> ExecuteSystemAction(DesktopSystemAction action)
        {
            var result = _shell.ExecuteSystemAction(new WebViewSystemActionRequest
            {
                Action = MapSystemAction(action)
            });
            return Task.FromResult(new DesktopSystemActionResult
            {
                Outcome = MapOutcome(result.Outcome),
                Action = action,
                DenyReason = result.DenyReason,
                Error = result.Error?.Message
            });
        }

        public Task<DesktopSystemIntegrationEventsResult> DrainSystemIntegrationEvents()
        {
            DesktopSystemIntegrationEvent[] snapshot;
            lock (_eventsLock)
            {
                snapshot = _inboundEvents.ToArray();
                _inboundEvents.Clear();
            }

            return Task.FromResult(new DesktopSystemIntegrationEventsResult
            {
                Outcome = DesktopCapabilityOutcome.Allow,
                Events = snapshot
            });
        }

        private static WebViewMenuItemModel MapMenuItem(DesktopMenuItem item)
        {
            ArgumentNullException.ThrowIfNull(item);
            return new WebViewMenuItemModel
            {
                Id = item.Id,
                Label = item.Label,
                IsEnabled = item.IsEnabled,
                Children = item.Children.Select(MapMenuItem).ToArray()
            };
        }

        private static WebViewSystemAction MapSystemAction(DesktopSystemAction action)
            => action switch
            {
                DesktopSystemAction.Quit => WebViewSystemAction.Quit,
                DesktopSystemAction.Restart => WebViewSystemAction.Restart,
                DesktopSystemAction.FocusMainWindow => WebViewSystemAction.FocusMainWindow,
                _ => throw new ArgumentOutOfRangeException(nameof(action), action, "Unsupported desktop system action.")
            };

        private static DesktopCapabilityOutcome MapOutcome(WebViewHostCapabilityCallOutcome outcome)
            => outcome switch
            {
                WebViewHostCapabilityCallOutcome.Allow => DesktopCapabilityOutcome.Allow,
                WebViewHostCapabilityCallOutcome.Deny => DesktopCapabilityOutcome.Deny,
                WebViewHostCapabilityCallOutcome.Failure => DesktopCapabilityOutcome.Failure,
                _ => DesktopCapabilityOutcome.Failure
            };

        private void OnSystemIntegrationEventReceived(object? sender, WebViewSystemIntegrationEventRequest request)
        {
            var mapped = new DesktopSystemIntegrationEvent
            {
                Kind = request.Kind switch
                {
                    WebViewSystemIntegrationEventKind.TrayInteracted => DesktopSystemIntegrationEventKind.TrayInteracted,
                    WebViewSystemIntegrationEventKind.MenuItemInvoked => DesktopSystemIntegrationEventKind.MenuItemInvoked,
                    _ => throw new ArgumentOutOfRangeException(nameof(request), request.Kind, "Unsupported system integration event kind.")
                },
                ItemId = request.ItemId,
                Context = request.Context
            };

            lock (_eventsLock)
                _inboundEvents.Enqueue(mapped);
        }

        public void Dispose()
            => _shell.SystemIntegrationEventReceived -= OnSystemIntegrationEventReceived;
    }

    private sealed class TemplateHostCapabilityPolicy : IWebViewHostCapabilityPolicy
    {
        public WebViewHostCapabilityDecision Evaluate(in WebViewHostCapabilityRequestContext context)
            => context.Operation switch
            {
                WebViewHostCapabilityOperation.ClipboardReadText => WebViewHostCapabilityDecision.Allow(),
                WebViewHostCapabilityOperation.ClipboardWriteText => WebViewHostCapabilityDecision.Allow(),
                WebViewHostCapabilityOperation.MenuApplyModel => WebViewHostCapabilityDecision.Allow(),
                WebViewHostCapabilityOperation.TrayUpdateState => WebViewHostCapabilityDecision.Allow(),
                WebViewHostCapabilityOperation.SystemActionExecute => WebViewHostCapabilityDecision.Deny("template-system-action-not-enabled"),
                WebViewHostCapabilityOperation.TrayInteractionEventDispatch => WebViewHostCapabilityDecision.Allow(),
                WebViewHostCapabilityOperation.MenuInteractionEventDispatch => WebViewHostCapabilityDecision.Allow(),
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

        public void ApplyMenuModel(WebViewMenuModelRequest request)
            => _ = request;

        public void UpdateTrayState(WebViewTrayStateRequest request)
            => _ = request;

        public void ExecuteSystemAction(WebViewSystemActionRequest request)
        {
            _ = request;
            throw new NotSupportedException("System action is not enabled in this template preset.");
        }
    }
}
