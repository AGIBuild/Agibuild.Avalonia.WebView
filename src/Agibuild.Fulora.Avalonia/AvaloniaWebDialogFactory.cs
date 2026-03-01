namespace Agibuild.Fulora;

/// <summary>
/// Production implementation of <see cref="IWebDialogFactory"/> that creates
/// <see cref="AvaloniaWebDialog"/> instances backed by Avalonia <c>Window</c> + <c>WebView</c>.
/// <para>
/// Register via DI or instantiate directly:
/// <code>
/// var factory = new AvaloniaWebDialogFactory();
/// using var dialog = factory.Create();
/// dialog.Show();
/// await dialog.NavigateAsync(new Uri("https://example.com"));
/// </code>
/// </para>
/// </summary>
public sealed class AvaloniaWebDialogFactory : IWebDialogFactory
{
    /// <inheritdoc />
    public IWebDialog Create(IWebViewEnvironmentOptions? options = null)
        => new AvaloniaWebDialog(options);
}
