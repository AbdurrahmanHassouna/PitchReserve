using System.Security.Claims;
using System.Text.Json;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace PitchReserve.Api.Extensions;

public static class RateLimitingExtensions
{
    public static IServiceCollection AddRateLimiting(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // These configuration values keep the agreed defaults when the section is absent.
        var windowSeconds = configuration.GetValue("RateLimiting:WindowSeconds", 60);
        var loginLimit = configuration.GetValue("RateLimiting:LoginLimitPerIp", 10);
        var registrationLimit = configuration.GetValue("RateLimiting:RegistrationLimitPerIp", 5);
        var readLimit = configuration.GetValue("RateLimiting:ReadLimitPerUserOrIp", 120);
        var writeLimit = configuration.GetValue("RateLimiting:WriteLimitPerUserOrIp", 30);

        if (windowSeconds < 1 || loginLimit < 1 || registrationLimit < 1 || readLimit < 1 || writeLimit < 1)
        {
            throw new InvalidOperationException("Rate-limiting values must be positive.");
        }

        var window = TimeSpan.FromSeconds(windowSeconds);

        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
                GetPartition(context, window, loginLimit, registrationLimit, readLimit, writeLimit));

            options.OnRejected = async (rejection, cancellationToken) =>
            {
                rejection.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;

                var retryAfterSeconds = 60;
                if (rejection.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                {
                    retryAfterSeconds = Math.Max(1, (int)Math.Ceiling(retryAfter.TotalSeconds));
                }

                rejection.HttpContext.Response.Headers["Retry-After"] =
                    retryAfterSeconds.ToString(System.Globalization.CultureInfo.InvariantCulture);

                var problemDetails = new ProblemDetails
                {
                    Status = StatusCodes.Status429TooManyRequests,
                    Title = "Too many requests",
                    Detail = "The request limit for this API policy has been reached."
                };
                problemDetails.Extensions["traceId"] = rejection.HttpContext.TraceIdentifier;

                await rejection.HttpContext.Response.WriteAsJsonAsync(
                    problemDetails,
                    options: null,
                    contentType: "application/problem+json",
                    cancellationToken: cancellationToken);
            };
        });

        return services;
    }

    private static RateLimitPartition<string> GetPartition(
        HttpContext context,
        TimeSpan window,
        int loginLimit,
        int registrationLimit,
        int readLimit,
        int writeLimit)
    {
        if (HttpMethods.IsOptions(context.Request.Method))
        {
            return RateLimitPartition.GetNoLimiter("no-limit");
        }

        var action = context.GetEndpoint()?.Metadata.GetMetadata<ControllerActionDescriptor>();
        if (action is null)
        {
            // Swagger, static assets, and endpoints outside MVC controllers do not consume limits.
            return RateLimitPartition.GetNoLimiter("no-controller");
        }

        var method = context.Request.Method;
        string policyName;
        int permitLimit;
        string subject;

        if (string.Equals(action.ControllerName, "Auth", StringComparison.OrdinalIgnoreCase) &&
            HttpMethods.IsPost(method) &&
            string.Equals(action.ActionName, "Login", StringComparison.OrdinalIgnoreCase))
        {
            policyName = "login";
            permitLimit = loginLimit;
            subject = GetClientIp(context);
        }
        else if (string.Equals(action.ControllerName, "Auth", StringComparison.OrdinalIgnoreCase) &&
                 HttpMethods.IsPost(method) &&
                 string.Equals(action.ActionName, "Register", StringComparison.OrdinalIgnoreCase))
        {
            policyName = "registration";
            permitLimit = registrationLimit;
            subject = GetClientIp(context);
        }
        else if (HttpMethods.IsGet(method) || HttpMethods.IsHead(method))
        {
            policyName = "read";
            permitLimit = readLimit;
            subject = GetUserOrClientIp(context);
        }
        else
        {
            policyName = "write";
            permitLimit = writeLimit;
            subject = GetUserOrClientIp(context);
        }

        // Including the policy name gives reads, writes, login, and registration separate windows.
        var partitionKey = $"{policyName}:{subject}";
        return RateLimitPartition.GetFixedWindowLimiter(partitionKey, _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = permitLimit,
            Window = window,
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
            QueueLimit = 0,
            AutoReplenishment = true
        });
    }

    private static string GetUserOrClientIp(HttpContext context)
    {
        if (context.User.Identity?.IsAuthenticated == true)
        {
            var userIdValue = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value ??
                              context.User.FindFirst("sub")?.Value;
            if (Guid.TryParse(userIdValue, out var userId))
            {
                return $"user:{userId:D}";
            }
        }

        return GetClientIp(context);
    }

    private static string GetClientIp(HttpContext context)
    {
        // RemoteIpAddress is set by the server; caller-supplied forwarding headers are ignored.
        return $"ip:{context.Connection.RemoteIpAddress?.MapToIPv6().ToString() ?? "unknown"}";
    }
}
