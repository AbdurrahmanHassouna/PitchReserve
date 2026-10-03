namespace PitchReserve.Api.Middleware;

using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.IdentityModel.JsonWebTokens;
using Serilog.Context;

public class UserContextLoggingMiddleware
{
    private readonly RequestDelegate _next;

    public UserContextLoggingMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        IDisposable? userScope = null;

        var user = context.User;
        if (user?.Identity?.IsAuthenticated == true)
        {
            var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? user.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
                ?? user.FindFirst("sub")?.Value;

            if (!string.IsNullOrWhiteSpace(userId))
            {
                userScope = LogContext.PushProperty("UserId", userId);
            }
        }

        try
        {
            await _next(context);
        }
        finally
        {
            userScope?.Dispose();
        }
    }
}
