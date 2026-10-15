using AutoNumber.Infrastructure;
using AutoNumber.Model;
using AutoNumber.ViewModels;
using System.Diagnostics;
using System.IO;
using System.Reflection;
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

        /// <summary>
        /// Append-only log of unhandled exceptions, next to settings.json. Users can attach it to a bug report.
        /// </summary>
        public static string ErrorLogPath => Path.Combine(AppSettingsStore.SettingsDirectory, "error.log");

        private void startup(object sender, StartupEventArgs e)
        {
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
            tryLog(e.Exception, "Nicht beobachteter Task");
            e.SetObserved();
        }

        private static void reportUnhandled(Exception ex, string origin)
        {
            var logged = tryLog(ex, origin);

            var message = $"Ein unerwarteter Fehler ist aufgetreten ({origin}):\n\n{ex.GetType().Name}: {ex.Message}";
            if (logged)
            {
                message += $"\n\nDetails wurden gespeichert in:\n{ErrorLogPath}\n\nBitte diese Datei einer Fehlermeldung beifügen.";
            }

            try
            {
                MessageBox.Show(message, "AutoNumber – Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception dlgEx)
            {
                Trace.WriteLine($"Error showing unhandled-exception dialog: {dlgEx}");
            }
        }

        private static bool tryLog(Exception ex, string origin)
        {
            Trace.WriteLine($"Unhandled exception ({origin}): {ex}");
            try
            {
                Directory.CreateDirectory(AppSettingsStore.SettingsDirectory);
                var version = Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "?";
                var entry = $"""
                    ==== {DateTime.Now:yyyy-MM-dd HH:mm:ss} | AutoNumber {version} | {origin} ====
                    {ex}

                    """;
                File.AppendAllText(ErrorLogPath, entry);
                return true;
            }
            catch (Exception logEx)
            {
                Trace.WriteLine($"Error writing {ErrorLogPath}: {logEx}");
                return false;
            }
        }
    }
}
