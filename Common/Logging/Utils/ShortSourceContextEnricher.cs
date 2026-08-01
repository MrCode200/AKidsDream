using System;
using Serilog.Core;
using Serilog.Events;

namespace AKidsDream.Common.Logging;

public class ShortSourceContextEnricher : ILogEventEnricher
{
    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        if (logEvent.Properties.TryGetValue("SourceContext", out var value)
            && value is ScalarValue scalar
            && scalar.Value is string fullName)
        {
            string shortName;
            try
            {
                shortName = fullName.Substring(fullName.LastIndexOf('.') + 1);
            }
            catch (ArgumentOutOfRangeException exception)
            {
                shortName = fullName;
            }

            logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("ShortSourceContext", shortName));
        }
    }
}