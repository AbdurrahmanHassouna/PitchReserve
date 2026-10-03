namespace PitchReserve.Api.Diagnostics;

using Serilog;
using Serilog.Core;
using Serilog.Events;
using Serilog.Filters;

public class PrivacyEnricher : ILogEventEnricher
{
    private static readonly HashSet<string> SensitiveProperties = new(StringComparer.OrdinalIgnoreCase)
    {
        "RequestPath",
        "Path",
        "QueryString",
        "ActionName",
        "ActionId",
        "Authorization",
        "AuthorizationHeader",
        "Headers",
        "RequestHeaders",
        "ResponseHeaders",
        "RequestBody",
        "ResponseBody",
        "ClientIp",
        "Token",
        "AccessToken",
        "RefreshToken",
        "Password",
        "Secret",
        "Cookie",
        "Cookies",
        "Set-Cookie"
    };

    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        Sanitize(logEvent);
    }

    private static void Sanitize(LogEvent logEvent)
    {
        var keysToRemove = logEvent.Properties.Keys
            .Where(k => SensitiveProperties.Contains(k))
            .ToList();

        foreach (var key in keysToRemove)
        {
            logEvent.RemovePropertyIfPresent(key);
        }
    }
}

public static class PrivacyLoggingExtensions
{
    public static LoggerConfiguration ApplyPitchReservePrivacyPolicies(this LoggerConfiguration configuration)
    {
        return configuration
            .Enrich.With<PrivacyEnricher>()
            .Filter.ByExcluding(Matching.FromSource("Microsoft.AspNetCore.Hosting.Diagnostics"))
            .Filter.ByExcluding(Matching.FromSource("Microsoft.AspNetCore.Diagnostics.ExceptionHandlerMiddleware"));
    }
}
