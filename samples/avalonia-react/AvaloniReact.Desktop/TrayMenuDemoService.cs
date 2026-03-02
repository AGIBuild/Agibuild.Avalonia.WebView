using System;
using System.Linq;
using System.Threading.Tasks;
using Agibuild.Fulora.Shell;
using AvaloniReact.Bridge.Services;

namespace AvaloniReact.Desktop;

/// <summary>
/// Desktop implementation of <see cref="ITrayMenuDemoService"/> using
/// <see cref="AvaloniaHostCapabilityProvider"/> for native tray and menu bindings.
/// </summary>
internal sealed class TrayMenuDemoService : ITrayMenuDemoService, IDisposable
{
    private readonly AvaloniaHostCapabilityProvider _provider;

    public TrayMenuDemoService(AvaloniaHostCapabilityProvider provider)
    {
        _provider = provider;
        _provider.TrayClicked += OnTrayClicked;
        _provider.MenuItemClicked += OnMenuItemClicked;
    }

    public Task<TrayUpdateResult> UpdateTray(TrayStateRequest request)
    {
        try
        {
            _provider.UpdateTrayState(new WebViewTrayStateRequest
            {
                IsVisible = request.IsVisible,
                Tooltip = request.Tooltip,
                IconPath = request.IconPath
            });
            return Task.FromResult(new TrayUpdateResult { Success = true });
        }
        catch (Exception ex)
        {
            return Task.FromResult(new TrayUpdateResult { Success = false, Error = ex.Message });
        }
    }

    public Task<MenuApplyResult> ApplyMenu(MenuModelRequest request)
    {
        try
        {
            var webViewRequest = new WebViewMenuModelRequest
            {
                Items = request.Items.Select(MapItem).ToArray()
            };
            _provider.ApplyMenuModel(webViewRequest);
            return Task.FromResult(new MenuApplyResult
            {
                Success = true,
                AppliedItemCount = request.Items.Length
            });
        }
        catch (Exception ex)
        {
            return Task.FromResult(new MenuApplyResult { Success = false, Error = ex.Message });
        }
    }

    private static WebViewMenuItemModel MapItem(MenuItemModel item) => new()
    {
        Id = item.Id,
        Label = item.Label,
        IsEnabled = item.IsEnabled,
        Children = item.Children.Select(MapItem).ToArray()
    };

    private void OnTrayClicked(TrayInteractionEventArgs _)
    {
        // In a real app, this would push a bridge event to the JS side.
    }

    private void OnMenuItemClicked(MenuInteractionEventArgs args)
    {
        // In a real app, this would push a bridge event with args.ItemId to the JS side.
    }

    public void Dispose()
    {
        _provider.TrayClicked -= OnTrayClicked;
        _provider.MenuItemClicked -= OnMenuItemClicked;
    }
}
