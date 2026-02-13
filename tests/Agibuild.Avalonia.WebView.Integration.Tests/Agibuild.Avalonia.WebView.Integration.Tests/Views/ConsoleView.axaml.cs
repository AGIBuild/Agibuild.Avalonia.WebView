using Avalonia.Controls;
using Avalonia.Input.Platform;
using Avalonia.Interactivity;
using Agibuild.Avalonia.WebView.Integration.Tests.ViewModels;

namespace Agibuild.Avalonia.WebView.Integration.Tests.Views
{
    public partial class ConsoleView : UserControl
    {
        public ConsoleView()
        {
            InitializeComponent();
        }

        private async void OnCopyClicked(object? sender, RoutedEventArgs e)
        {
            if (DataContext is not MainViewModel vm) return;
            if (string.IsNullOrEmpty(vm.SharedLog)) return;

            var topLevel = TopLevel.GetTopLevel(this);
            if (topLevel?.Clipboard is not { } clipboard) return;

            await clipboard.SetTextAsync(vm.SharedLog);
        }
    }
}
