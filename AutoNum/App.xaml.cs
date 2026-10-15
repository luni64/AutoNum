using AutoNumber.Infrastructure;
using AutoNumber.ViewModels;
using System.Windows;
using System.Windows.Threading;

namespace AutoNumber
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        DialogService dialogService = new();

        private void startup(object sender, StartupEventArgs e)
        {
            AppLog.Initialize();

            // Last line of defence: nothing in the app should throw past a command handler, but if it
            // does, log it and tell the user instead of silently dying.
            DispatcherUnhandledException += onDispatcherUnhandledException;
            AppDomain.CurrentDomain.UnhandledException += onDomainUnhandledException;
            TaskScheduler.UnobservedTaskException += onUnobservedTaskException;

            try
            {
                global::QuestPDF.Settings.License = global::QuestPDF.Infrastructure.LicenseType.Community;

                var mainVM = new MainVM(dialogService);
                var mainWin = new MainWindow(mainVM);
                mainWin.Show();
            }
            catch (Exception ex)
            {
                reportUnhandled(ex, "Programmstart");
                Shutdown(1);
            }
        }

        /// <summary>
        /// Exceptions escaping the UI thread (command handlers, async void continuations, bindings).
        /// The app stays alive so the user can still save their work.
        /// </summary>
        private void onDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            reportUnhandled(e.Exception, "Bedienoberfläche");
            e.Handled = true;
        }

        /// <summary>
        /// Exceptions on non-UI threads. The runtime terminates the process after this handler;
        /// all we can do is log and tell the user why.
        /// </summary>
        private void onDomainUnhandledException(object? sender, UnhandledExceptionEventArgs e)
        {
            var ex = e.ExceptionObject as Exception ?? new Exception(e.ExceptionObject?.ToString() ?? "unknown");
            reportUnhandled(ex, "Hintergrundthread");
        }

        /// <summary>
        /// Faulted tasks nobody awaited. Logged only — they don't crash the process on .NET Core+.
        /// </summary>
        private void onUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
        {
            AppLog.Error("Unhandled exception (nicht beobachteter Task)", e.Exception);
            e.SetObserved();
        }

        private static void reportUnhandled(Exception ex, string origin)
        {
            AppLog.Error($"Unhandled exception ({origin})", ex);

            var message = $"Ein unerwarteter Fehler ist aufgetreten ({origin}):\n\n{ex.GetType().Name}: {ex.Message}";
            if (AppLog.IsFileLoggingActive)
            {
                message += $"\n\nDetails stehen in der Protokolldatei:\n{AppLog.LogPath}\n\nBitte diese Datei einer Fehlermeldung beifügen.";
            }

            try
            {
                MessageBox.Show(message, "AutoNumber – Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception dlgEx)
            {
                AppLog.Error("Could not show the unhandled-exception dialog", dlgEx);
            }
        }
    }
}
