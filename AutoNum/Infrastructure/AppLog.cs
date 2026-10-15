using AutoNumber.Model;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;

namespace AutoNumber.Infrastructure;

/// <summary>
/// The application log: one append-only text file in %AppData%\AutoNum that users can attach
/// to a bug report. A thin wrapper over <see cref="Trace"/>, so everything also reaches the
/// debugger's output window through the default listener.
/// <para>
/// What belongs here is what helps to diagnose a user's problem after the fact:
/// the session facts (version, OS), every file open/save with the data that decided the code
/// path (metadata version and source, restore mode, counts, sizes), warnings for degraded
/// behaviour the user may not notice (font fallback, settings reset, skipped EXIF tag), and
/// errors with the full exception. Step-by-step debugging chatter does not belong here —
/// use the debugger for that.
/// </para>
/// </summary>
public static class AppLog
{
    private const long MaxLogBytes = 1_000_000;

    /// <summary>Current log file. The previous one is kept as <c>autonum.log.1</c> after rotation.</summary>
    public static string LogPath => Path.Combine(AppSettingsStore.SettingsDirectory, "autonum.log");

    /// <summary>False if the log file could not be opened; messages then only reach the debugger.</summary>
    public static bool IsFileLoggingActive { get; private set; }

    /// <summary>
    /// Attaches the file listener and writes the session header. Call once, first thing at startup.
    /// Never throws — logging must not be able to prevent the app from starting.
    /// </summary>
    public static void Initialize()
    {
        try
        {
            Directory.CreateDirectory(AppSettingsStore.SettingsDirectory);
            rotateIfLarge();
            Trace.Listeners.Add(new FileTraceListener(LogPath));
            Trace.AutoFlush = true;
            IsFileLoggingActive = true;
        }
        catch (Exception ex)
        {
            Trace.WriteLine($"AppLog: file logging unavailable: {ex}");
        }

        var version = Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "?";
        Info($"==== AutoNumber {version} started | {RuntimeInformation.OSDescription} | {RuntimeInformation.FrameworkDescription} | culture {CultureInfo.CurrentCulture.Name}");
    }

    public static void Info(string message) => Trace.TraceInformation(message);

    public static void Warn(string message, Exception? ex = null) => Trace.TraceWarning(withException(message, ex));

    public static void Error(string message, Exception? ex = null) => Trace.TraceError(withException(message, ex));

    private static string withException(string message, Exception? ex) =>
        ex is null ? message : $"{message}{Environment.NewLine}{ex}";

    private static void rotateIfLarge()
    {
        var file = new FileInfo(LogPath);
        if (file.Exists && file.Length > MaxLogBytes)
        {
            File.Move(LogPath, LogPath + ".1", overwrite: true);
        }
    }

    /// <summary>
    /// Writes one time-stamped line per message: <c>2026-10-15 09:12:33.123 [INFO ] message</c>.
    /// Opened with shared read access so the user can look at the file while the app runs.
    /// </summary>
    private sealed class FileTraceListener : TraceListener
    {
        private readonly StreamWriter _writer;

        public FileTraceListener(string path)
        {
            var stream = new FileStream(path, FileMode.Append, FileAccess.Write, FileShare.ReadWrite);
            _writer = new StreamWriter(stream) { AutoFlush = true };
        }

        public override void Write(string? message) => _writer.Write(message);

        public override void WriteLine(string? message) => writeEntry("TRACE", message);

        public override void TraceEvent(TraceEventCache? eventCache, string source, TraceEventType eventType, int id, string? message)
            => writeEntry(levelOf(eventType), message);

        public override void TraceEvent(TraceEventCache? eventCache, string source, TraceEventType eventType, int id, string? format, params object?[]? args)
            => writeEntry(levelOf(eventType), format is null ? null : args is { Length: > 0 } ? string.Format(CultureInfo.InvariantCulture, format, args) : format);

        private void writeEntry(string level, string? message)
            => _writer.WriteLine($"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} [{level,-5}] {message}");

        private static string levelOf(TraceEventType type) => type switch
        {
            TraceEventType.Critical or TraceEventType.Error => "ERROR",
            TraceEventType.Warning => "WARN",
            TraceEventType.Information => "INFO",
            _ => "TRACE",
        };

        protected override void Dispose(bool disposing)
        {
            if (disposing) _writer.Dispose();
            base.Dispose(disposing);
        }
    }
}
