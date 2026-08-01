/*#:property LangVersion=14.0
#:package Serilog@4.3.0
#:package Serilog.Sinks.Console@6.0.0
#:package Serilog.Sinks.File@6.0.0
#:package Serilog.Enrichers.CallerInfo@1.0.6

using Serilog;
using System;
using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;
using Serilog.Core;
using Serilog.Events;
using Serilog.Filters;

namespace AKidsDream.Common.testLogging;

class ThreadIdEnricher : ILogEventEnricher
{
    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty(
            "ThreadId", Environment.CurrentManagedThreadId));
    }
}

class CustomDateFormatter : IFormatProvider
{
    readonly IFormatProvider basedOn;
    readonly string shortDatePattern;
    public CustomDateFormatter(string shortDatePattern, IFormatProvider basedOn)
    {
        this.shortDatePattern = shortDatePattern;
        this.basedOn = basedOn;
    }
    public object GetFormat(Type formatType)
    {
        if (formatType == typeof(DateTimeFormatInfo))
        {
            var basedOnFormatInfo = (DateTimeFormatInfo)basedOn.GetFormat(formatType);
            var dateFormatInfo = (DateTimeFormatInfo)basedOnFormatInfo.Clone();
            dateFormatInfo.ShortDatePattern = this.shortDatePattern;
            return dateFormatInfo;
        }
        return this.basedOn.GetFormat(formatType);
    }
}


public class testLogger
{
    private static readonly ILogger _log = Serilog.Log.ForContext<testLogger>();
    
    public static void Main()
    {
        System.Console.WriteLine("CallingAssembly: " + Assembly.GetCallingAssembly().GetName().Name); 
        System.Console.WriteLine("EntryAssembly: " + (Assembly.GetEntryAssembly()?.GetName().Name ?? "<null>"));
        
        var dateFormatter = new CustomDateFormatter("yyyy-MM-dd", CultureInfo.InvariantCulture);
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            // OR MinimumLevel.ControlledBy(new LoggingLevelSwitch(LogEventLevel.Warning))
            // => levelSwitch.MinimumLevel = LogEventLevel.Verbose; Thus we can change the level dynamically
            
            .Destructure.ByTransforming<Exception>(ex => ex.ToString())
            
            .Enrich.WithProperty("Version", "1.0.0")
            .Enrich.With(new ThreadIdEnricher())
            
            .Filter.ByExcluding(Matching.WithProperty<int>("Count", p => p < 10))
            
            .WriteTo.Console(
                // restrictedToMinimumLevel: LogEventLevel.Information,
                formatProvider: dateFormatter,
                outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {SourceContext}.{Method}:{Line} {Message:lj}{NewLine}{Exception}"
                )
            .WriteTo.File(
                new Serilog.Formatting.Json.JsonFormatter(),
                "logs/myapp.jsonl",
                rollingInterval: RollingInterval.Day
            )
            
            .CreateLogger();

        _log.Info("Hello, world!");
        _log.Warning("Something is not right");
        int a = 10, b = 0;
        try
        {
            _log.Debug("Dividing {A} by {B}", a, b);
            System.Console.WriteLine(a / b);
        }
        catch (Exception ex)
        {
            _log.Error(ex, "Something went wrong");
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }
}*/