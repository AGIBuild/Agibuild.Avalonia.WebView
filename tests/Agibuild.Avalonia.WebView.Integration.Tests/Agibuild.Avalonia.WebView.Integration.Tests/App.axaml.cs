using Agibuild.Avalonia.WebView.Integration.Tests.ViewModels;
using Agibuild.Avalonia.WebView.Integration.Tests.Views;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using System;
using System.Linq;

namespace Agibuild.Avalonia.WebView.Integration.Tests
{
    public partial class App : Application
    {
        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                // Avoid duplicate validations from both Avalonia and the CommunityToolkit. 
                // More info: https://docs.avaloniaui.net/docs/guides/development-guides/data-validation#manage-validationplugins
                DisableAvaloniaDataAnnotationValidation();

                var args = desktop.Args ?? Array.Empty<string>();
                var mainVm = new MainViewModel();

                if (args.Contains("--wk-smoke"))
                {
                    mainVm.SelectedTabIndex = 1;
                    mainVm.WkWebViewSmoke.AutoRun = true;
                    mainVm.WkWebViewSmoke.AutoRunCompleted += exitCode =>
                        Dispatcher.UIThread.Post(() => desktop.Shutdown(exitCode));
                }
                else if (args.Contains("--consumer-e2e"))
                {
                    mainVm.SelectedTabIndex = 2;
                    mainVm.ConsumerE2E.AutoRun = true;
                    mainVm.ConsumerE2E.AutoRunCompleted += exitCode =>
                        Dispatcher.UIThread.Post(() => desktop.Shutdown(exitCode));
                }
                else if (args.Contains("--advanced-e2e"))
                {
                    mainVm.SelectedTabIndex = 3;
                    mainVm.AdvancedE2E.AutoRun = true;
                    mainVm.AdvancedE2E.AutoRunCompleted += exitCode =>
                        Dispatcher.UIThread.Post(() => desktop.Shutdown(exitCode));
                }

                desktop.MainWindow = new MainWindow { DataContext = mainVm };
            }
            else if (ApplicationLifetime is ISingleViewApplicationLifetime singleViewPlatform)
            {
                singleViewPlatform.MainView = new MainView
                {
                    DataContext = new MainViewModel()
                };
            }

            base.OnFrameworkInitializationCompleted();
        }

        private void DisableAvaloniaDataAnnotationValidation()
        {
            // Get an array of plugins to remove
            var dataValidationPluginsToRemove =
                BindingPlugins.DataValidators.OfType<DataAnnotationsValidationPlugin>().ToArray();

            // remove each entry found
            foreach (var plugin in dataValidationPluginsToRemove)
            {
                BindingPlugins.DataValidators.Remove(plugin);
            }
        }
    }
}