using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Agibuild.Avalonia.WebView.Integration.Tests.ViewModels;
using Agibuild.Avalonia.WebView.Integration.Tests.Views;
using Agibuild.Avalonia.WebView.Testing;
using Xunit;

namespace Agibuild.Avalonia.WebView.Integration.Tests.Automation;

public sealed class WebViewFunctionalTests
{
    [AvaloniaFact]
    public async Task Navigate_command_updates_last_navigation()
    {
        var adapter = new MockWebViewAdapter();
        var viewModel = new WebViewTestViewModel(adapter);
        var view = new WebViewTestView { DataContext = viewModel };
        var window = new Window { Content = view };

        window.Show();
        view.FindControl<TextBox>("AddressTextBox")!.Text = "https://example.test";

        await viewModel.NavigateCommand.ExecuteAsync(null);

        var expectedUri = new Uri("https://example.test");
        Assert.Equal(expectedUri.ToString(), viewModel.LastNavigation);
        Assert.Equal(expectedUri, adapter.LastNavigationUri);
        window.Close();
    }

    [AvaloniaFact]
    public async Task Navigate_html_updates_last_navigation()
    {
        var adapter = new MockWebViewAdapter();
        var viewModel = new WebViewTestViewModel(adapter);
        var view = new WebViewTestView { DataContext = viewModel };
        var window = new Window { Content = view };

        window.Show();
        view.FindControl<TextBox>("HtmlTextBox")!.Text = "<html><body>ok</body></html>";

        await viewModel.NavigateHtmlCommand.ExecuteAsync(null);

        Assert.Equal("html", viewModel.LastNavigation);
        Assert.Null(adapter.LastNavigationUri);
        window.Close();
    }

    [AvaloniaFact]
    public async Task Invoke_script_updates_result()
    {
        var adapter = new MockWebViewAdapter { ScriptResult = "ok" };
        var viewModel = new WebViewTestViewModel(adapter);
        var view = new WebViewTestView { DataContext = viewModel };
        var window = new Window { Content = view };

        window.Show();
        view.FindControl<TextBox>("ScriptTextBox")!.Text = "return 'ok';";

        await viewModel.InvokeScriptCommand.ExecuteAsync(null);

        Assert.Equal("ok", viewModel.ScriptResult);
        window.Close();
    }

    [AvaloniaFact]
    public void Navigation_completed_event_updates_state()
    {
        var adapter = new MockWebViewAdapter();
        var viewModel = new WebViewTestViewModel(adapter);
        var view = new WebViewTestView { DataContext = viewModel };
        var window = new Window { Content = view };

        window.Show();
        var uri = new Uri("https://example.test");
        var navId = Guid.NewGuid();
        adapter.NavigateAsync(navId, uri).GetAwaiter().GetResult();
        adapter.RaiseNavigationCompleted(navId, uri, NavigationCompletedStatus.Success);

        Assert.Equal("NavigationCompleted", viewModel.LastEvent);
        window.Close();
    }
}
