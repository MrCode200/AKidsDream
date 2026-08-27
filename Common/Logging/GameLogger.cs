using System;
using System.IO;
using System.Runtime.CompilerServices;
using Godot;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.GodotConsole;
using Serilog.Sinks.SystemConsole.Themes;

namespace AKidsDream.Common.Logging;

//[2026-07-31 12:34:56,789] [WARNING | fake_file.py | lineno(123) | fake_function] ...

//.Enrich.WithCallerInfo( //Try to figure out later
//    includeFileInfo: false,
//    "AKidsDream."
//    ) // Includes Method, namespace (including class name)

// https://github.com/serilog/serilog-sinks-debug Writes to IDE (System.Diagnostics.Debug.WriteLine())

public static class LoggerExtensions
{
    public static CallerLogger Here(this ILogger log,
        [CallerMemberName] string member = "",
        [CallerLineNumber] int line = 0)
        => new CallerLogger(log, member, line);
}

public readonly struct CallerLogger
{
    private readonly ILogger _log;
    private readonly string _member;
    private readonly int _line;

    internal CallerLogger(ILogger log, string member, int line)
    {
        _log = log; _member = member; _line = line;
    }

    public void Verbose(string template, params object[] args) => Write(LogEventLevel.Verbose, null, template, args);
    public void Debug(string template, params object[] args) => Write(LogEventLevel.Debug, null, template, args);
    public void Info(string template, params object[] args)  => Write(LogEventLevel.Information, null, template, args);
    public void Warn(string template, params object[] args)  => Write(LogEventLevel.Warning, null, template, args);
    public void Err(string template, params object[] args) => Write(LogEventLevel.Error, null, template, args);
    public void Err(Exception ex, string template, params object[] args) => Write(LogEventLevel.Error, ex, template, args);
    public void Fatal(string template, params object[] args) => Write(LogEventLevel.Fatal, null, template, args);
    public void Fatal(Exception ex, string template, params object[] args) => Write(LogEventLevel.Fatal, ex, template, args);

    private void Write(LogEventLevel level, Exception ex, string template, object[] args) =>
        _log.ForContext("Method", _member)
            .ForContext("Line", _line)
            .Write(level, ex, template, args);
}

public static class GameLogger
{
    private static bool _isSetup;
    private static readonly object _lock = new();

        // Shared "intent" palette. The Godot sink turns these into BBCode hex colors;
    // the plain console sink can't use arbitrary hex (it relies on Serilog's ANSI
    // theme categories instead), so this is mainly here to keep the two outputs
    // conceptually consistent and easy to retune in one place.
    private static class LogColors
    {
        public const string Timestamp     = "#6c7a89"; // muted gray-blue
        public const string SourceContext = "#5dade2"; // blue
        public const string MethodLine    = "#f4d03f"; // yellow
        public const string UnitTag       = "#48c9b0"; // teal
        public const string Verbose       = "#7f8c8d";
        public const string Debug         = "#95a5a6";
        public const string Information   = "#2ecc71";
        public const string Warning       = "#f39c12";
        public const string Error         = "#e74c3c";
        public const string Fatal         = "#ff005f";
    }

    // Builds one Godot BBCode template per level. GD.PrintRich() (used internally by
    // Serilog.Sinks.GodotConsole) is what actually renders [color]/[b] tags — a single
    // static outputTemplate can't branch on level, so this has to be selected per-event
    // via templateSelector rather than baked into one shared string.
    private static string BuildGodotTemplate(string levelColor, bool boldLevel = false)
    {
        var levelOpen  = boldLevel ? "[b]" : "";
        var levelClose = boldLevel ? "[/b]" : "";

        return
            "[color=" + LogColors.Timestamp + "][{Timestamp:HH:mm:ss}][/color] " +
            "[[color=" + LogColors.SourceContext + "]{ShortSourceContext}[/color] | " +
            "[color=" + LogColors.MethodLine + "]{Method}:{Line}[/color]] " +
            "[color=" + levelColor + "]" + levelOpen + "[{Level:u3}]" + levelClose + "[/color] => " +
            "[color=" + LogColors.UnitTag + "][{UnitName}:{UnitId}][/color]: " +
            "[color=" + levelColor + "]{Message:lj}[/color]" +
            "{NewLine}" +
            "[color=" + LogColors.Error + "]{Exception}[/color]";
    }
    
    public static void Setup(bool debug = true)
    {
        if (_isSetup) return;

        lock (_lock)
        {
            if (_isSetup) return;
            _isSetup = true;

            var logDir = Path.Combine(OS.GetUserDataDir(), "logs");

            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Is(debug ? LogEventLevel.Debug : LogEventLevel.Information)
                .Enrich.FromLogContext() // enables LogContext.PushProperty scopes later
                .Enrich.With<ShortSourceContextEnricher>()
                .WriteTo.GodotConsole(
                    templateSelector: logEvent => logEvent.Level switch
                    {
                        LogEventLevel.Verbose     => BuildGodotTemplate(LogColors.Verbose),
                        LogEventLevel.Debug       => BuildGodotTemplate(LogColors.Debug),
                        LogEventLevel.Information => BuildGodotTemplate(LogColors.Information),
                        LogEventLevel.Warning     => BuildGodotTemplate(LogColors.Warning),
                        LogEventLevel.Error       => BuildGodotTemplate(LogColors.Error),
                        LogEventLevel.Fatal       => BuildGodotTemplate(LogColors.Fatal, boldLevel: true),
                        _                         => BuildGodotTemplate(LogColors.Debug)
                    },
                    pushErrorsToDebugger: false
                ) // CONFIG: which logging to show in console
                /*
                .WriteTo.Console(outputTemplate:
                    "[{Timestamp:HH:mm:ss}] " +
                    "[{ShortSourceContext} | {Method} | {Line}] " +
                    "[{Level:u3}] => " +
                    "[{UnitName}:{UnitId}]: " +
                    "{Message:lj}{NewLine}{Exception}",
                    theme: AnsiConsoleTheme.Code
                )*/
                
                /* Could Complicate things, not really needed to filter out 
                 .WriteTo.Logger(lc => lc
                    .MinimumLevel.Is(LogEventLevel.Debug)
                    .Enrich.With<RemoveShortSourceContextEnricher>()
                    .WriteTo.File(
                        new Serilog.Formatting.Json.JsonFormatter(),
                        Path.Combine(logDir, "AKidsDreamLogs-.jsonl"),
                        rollingInterval: RollingInterval.Day)
                )
                */
                .WriteTo.File(
                    new Serilog.Formatting.Json.JsonFormatter(),
                    Path.Combine(logDir, "AKidsDreamLogs-.jsonl"),
                    rollingInterval: RollingInterval.Day)
                .CreateLogger();
        }
    }

    private static void EnsureSetup()
    {
        if (!_isSetup) Setup();
    }

    public static ILogger For<T>()
    {
        EnsureSetup();
        return Log.ForContext<T>();
    }

    public static ILogger For(Type type)
    {
        EnsureSetup();
        return Log.ForContext(type);
    }
}