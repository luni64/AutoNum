using AutoNumber.Model;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;

namespace AutoNumber.Infrastructure;

/// <summary>
/// The application log: one append-only text file (by default %AppData%\AutoNum\autonum.log)
/// that users can attach to a bug report. A thin wrapper over <see cref="Trace"/>, so everything
/// also reaches the debugger's output window through the default listener.
/// <para>
/// What belongs here is what helps to diagnose a user's problem after the fact:
/// the session facts (version, OS), every file open/save with the data that decided the code
/// path (metadata version and source, restore mode, counts, sizes), warnings for degraded
/// behaviour the user may not notice (font fallback, settings reset, skipped EXIF tag), and
/// errors with the full exception. Step-by-step debugging chatter does not belong here —
/// use the debugger for that.
/// </para>
/// <para>
/// Lifecycle: <see cref="Initialize"/> runs first thing at startup and only buffers; the file is
/// opened by <see cref="Configure"/> once the settings (enabled flag, path) are known, which
/// also flushes the buffered lines. Configure is called again whenever settings are saved.
/// </para>
/// </summary>
public static class AppLog
{
    private const long MaxLogBytes = 1_000_000;
    private static readonly object _sync = new();
    private static FileTraceListener? _listener;
    private static List<string>? _early = [];
    private static bool _configured;

    /// <summary>Where the log goes when no custom path is configured.</summary>
    public static string DefaultLogPath => Path.Combine(AppSettingsStore.SettingsDirectory, "autonum.log");

    /// <summary>The effective log file (configured path or default). The previous file is kept as <c>&lt;name&gt;.1</c> after rotation.</summary>
    public static string LogPath { get; private set; } = DefaultLogPath;

    /// <summary>True while a log file is open; false if logging is disabled or the file could not be opened.</summary>
    public static bool IsFileLoggingActive => _listener is not null;

    /// <summary>Maps the settings value to the effective path: blank means the default.</summary>
    public static string ResolvePath(string? configuredPath) =>
        string.IsNullOrWhiteSpace(configuredPath) ? DefaultLogPath : configuredPath.Trim();

    /// <summary>
    /// Writes the session header. Call once, first thing at startup; nothing is written to disk
    /// until <see cref="Configure"/> has been called with the settings.
    /// </summary>
    public static void Initialize()
    {
        var version = Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "?";
        Info($"==== AutoNumber {version} started | {RuntimeInformation.OSDescription} | {RuntimeInformation.FrameworkDescription} | culture {CultureInfo.CurrentCulture.Name}");
    }

    /// <summary>
    /// Applies the logging settings: opens (or switches, or closes) the log file. Idempotent.
    /// Returns null on success, otherwise a user-facing German message saying why the file
    /// could not be opened (logging then stays off until the settings are changed).
    /// </summary>
    public static string? Configure(bool enabled, string? configuredPath)
    {
        lock (_sync)
        {
            var newPath = ResolvePath(configuredPath);
            var unchanged = _configured && enabled == IsFileLoggingActive
                && (!enabled || string.Equals(newPath, LogPath, StringComparison.OrdinalIgnoreCase));
            if (unchanged)
            {
                return null;
            }

            if (_listener is not null)
            {
                Info(enabled ? $"Logging continues in '{newPath}'" : "Logging disabled in the settings");
                Trace.Listeners.Remove(_listener);
                _listener.Dispose();
                _listener = null;
            }

            LogPath = newPath;
            string? error = null;
            if (enabled)
            {
                try
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(newPath) ?? AppSettingsStore.SettingsDirectory);
                    rotateIfLarge(newPath);
                    var listener = new FileTraceListener(newPath);
                    foreach (var line in _early ?? [])
                    {
                        listener.WriteRaw(line);
                    }
                    Trace.Listeners.Add(listener);
                    Trace.AutoFlush = true;
                    _listener = listener;
                    if (_configured)
                    {
                        Info($"Logging enabled, writing to '{newPath}'");
                    }
                }
                catch (Exception ex)
                {
                    Trace.WriteLine($"AppLog: cannot open '{newPath}': {ex}");
                    error = $"Die Protokolldatei konnte nicht geöffnet werden:\n{newPath}\n\n{ex.Message}\n\nEs wird kein Protokoll geschrieben, bis die Einstellung korrigiert ist.";
                }
            }

            _early = null;
            _configured = true;
            return error;
        }
    }

    /// <summary>
    /// For the unhandled-exception path: if the app crashed before the settings were loaded,
    /// fall back to the default log so the failure is still recorded somewhere.
    /// </summary>
    public static void EnsureConfigured()
    {
        lock (_sync)
        {
            if (!_configured)
            {
                Configure(enabled: true, configuredPath: null);
            }
        }
    }

    public static void Info(string message) => write(TraceEventType.Information, message);

    public static void Warn(string message, Exception? ex = null) => write(TraceEventType.Warning, withException(message, ex));

    public static void Error(string message, Exception? ex = null) => write(TraceEventType.Error, withException(message, ex));

    private static void write(TraceEventType type, string message)
    {
        lock (_sync)
        {
            _early?.Add(formatLine(levelOf(type), message));
        }

        switch (type)
        {
            case TraceEventType.Error: Trace.TraceError(message); break;
            case TraceEventType.Warning: Trace.TraceWarning(message); break;
            default: Trace.TraceInformation(message); break;
        }
    }

    private static string withException(string message, Exception? ex) =>
        ex is null ? message : $"{message}{Environment.NewLine}{ex}";

    private static string formatLine(string level, string? message) =>
        $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} [{level,-5}] {message}";

    private static string levelOf(TraceEventType type) => type switch
    {
        TraceEventType.Critical or TraceEventType.Error => "ERROR",
        TraceEventType.Warning => "WARN",
        TraceEventType.Information => "INFO",
        _ => "TRACE",
    };

    private static void rotateIfLarge(string path)
    {
        var file = new FileInfo(path);
        if (file.Exists && file.Length > MaxLogBytes)
        {
            File.Move(path, path + ".1", overwrite: true);
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

        /// <summary>Writes an already formatted line (used to flush the start-up buffer).</summary>
        public void WriteRaw(string line) => _writer.WriteLine(line);

        public override void Write(string? message) => _writer.Write(message);

        public override void WriteLine(string? message) => _writer.WriteLine(formatLine("TRACE", message));

        public override void TraceEvent(TraceEventCache? eventCache, string source, TraceEventType eventType, int id, string? message)
            => _writer.WriteLine(formatLine(levelOf(eventType), message));

        public override void TraceEvent(TraceEventCache? eventCache, string source, TraceEventType eventType, int id, string? format, params object?[]? args)
            => TraceEvent(eventCache, source, eventType, id, format is null ? null : args is { Length: > 0 } ? string.Format(CultureInfo.InvariantCulture, format, args) : format);

        protected override void Dispose(bool disposing)
        {
            if (disposing) _writer.Dispose();
            base.Dispose(disposing);
        }
    }
}
