using System;
using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Threading;

namespace Agibuild.Fulora.Shell;

/// <summary>
/// Maps <see cref="WebViewMenuItemModel"/> trees to Avalonia <see cref="NativeMenu"/>
/// hierarchies and dispatches click events.
/// </summary>
internal sealed class AvaloniaMenuManager : IDisposable
{
    private NativeMenu? _menu;
    private readonly List<NativeMenuItem> _trackedItems = [];
    private bool _disposed;

    /// <summary>
    /// Raised when a leaf menu item is clicked.
    /// </summary>
    public event Action<MenuInteractionEventArgs>? MenuItemClicked;

    /// <summary>
    /// Gets the current Avalonia <see cref="NativeMenu"/> instance. Created on first apply.
    /// </summary>
    public NativeMenu? Menu => _menu;

    /// <summary>
    /// Applies a menu model by mapping <see cref="WebViewMenuModelRequest"/> to Avalonia NativeMenu.
    /// </summary>
    public void ApplyMenuModel(WebViewMenuModelRequest request)
    {
        if (_disposed) return;

        if (Dispatcher.UIThread.CheckAccess())
        {
            ApplyMenuModelCore(request);
        }
        else
        {
            Dispatcher.UIThread.Post(() => ApplyMenuModelCore(request));
        }
    }

    private void ApplyMenuModelCore(WebViewMenuModelRequest request)
    {
        if (_disposed) return;

        ClearTrackedItems();

        _menu ??= new NativeMenu();
        _menu.Items.Clear();

        foreach (var item in request.Items)
        {
            var nativeItem = MapToNativeItem(item);
            _menu.Items.Add(nativeItem);
        }
    }

    private NativeMenuItem MapToNativeItem(WebViewMenuItemModel model)
    {
        var item = new NativeMenuItem(model.Label)
        {
            IsEnabled = model.IsEnabled
        };

        if (model.Children.Count > 0)
        {
            var submenu = new NativeMenu();
            foreach (var child in model.Children)
                submenu.Items.Add(MapToNativeItem(child));
            item.Menu = submenu;
        }
        else
        {
            item.Click += (_, _) => OnMenuItemClicked(model.Id);
            _trackedItems.Add(item);
        }

        return item;
    }

    private void OnMenuItemClicked(string itemId)
    {
        MenuItemClicked?.Invoke(new MenuInteractionEventArgs(itemId));
    }

    private void ClearTrackedItems()
    {
        _trackedItems.Clear();
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        ClearTrackedItems();

        if (_menu is not null)
        {
            _menu.Items.Clear();
            _menu = null;
        }
    }
}

/// <summary>
/// Event args for menu item interaction events.
/// </summary>
public sealed class MenuInteractionEventArgs : EventArgs
{
    /// <summary>Creates a new instance with the clicked item ID.</summary>
    public MenuInteractionEventArgs(string itemId)
    {
        ItemId = itemId;
    }

    /// <summary>The ID of the clicked menu item.</summary>
    public string ItemId { get; }

    /// <summary>UTC timestamp of the interaction.</summary>
    public DateTimeOffset Timestamp { get; } = DateTimeOffset.UtcNow;
}
