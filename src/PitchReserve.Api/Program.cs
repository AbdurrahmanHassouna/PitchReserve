using System.ComponentModel.DataAnnotations;
using System.Reflection;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using PitchReserve.Api.Diagnostics;
using PitchReserve.Api.Middleware;
using PitchReserve.Api.Options;
using PitchReserve.Api.Services;
using PitchReserve.Application;
using PitchReserve.Application.Common.Interfaces;
using PitchReserve.Application.Common.Options;
using PitchReserve.Infrastructure;
using PitchReserve.Infrastructure.Authentication;
using Serilog;
using Serilog.Events;

// Bootstrap logger captures startup/container initialization failures
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.Hosting.Lifetime", LogEventLevel.Information)
    .MinimumLevel.Override("System", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .ApplyPitchReservePrivacyPolicies()
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
    .CreateBootstrapLogger();

SafeSelfLog.Enable();

try
{
    var builder = WebApplication.CreateBuilder(args);

    var logDropMonitor = new LogDropMonitor();
    builder.Services.AddSingleton<ILogDropMonitor>(logDropMonitor);
    builder.Services.AddHostedService<LogDropNotifierService>();

    builder.Services.AddOptions<RequestLoggingOptions>()
        .Bind(builder.Configuration.GetSection(RequestLoggingOptions.SectionName))
        .ValidateDataAnnotations()
        .ValidateOnStart();

    builder.Services.AddOptions<ApplicationLoggingOptions>()
        .Bind(builder.Configuration.GetSection(ApplicationLoggingOptions.SectionName))
        .ValidateDataAnnotations()
        .ValidateOnStart();

    builder.Host.UseSerilog((context, services, configuration) =>
    {
        configuration.ConfigurePitchReserveLogging(context.Configuration, logDropMonitor);
    });

    builder.Services.AddHttpContextAccessor();
    builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();

    builder.Services.AddApplicationServices();
    builder.Services.AddInfrastructureServices(builder.Configuration);

    builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
    builder.Services.AddProblemDetails();

    builder.Services.AddControllers();

    var jwt = builder.Configuration.GetSection("JWTSettings").Get<JWTSettings>()
              ?? throw new InvalidOperationException("JWT settings are not configured.");
    builder.Services.AddAuthentication()
        .AddJwtBearer(options =>
        {
            options.MapInboundClaims = false;
            options.TokenValidationParameters = new TokenValidationParameters()
            {
                RoleClaimType = ClaimTypes.Role,
                NameClaimType = JwtRegisteredClaimNames.Sub,
                ValidateIssuer = true,
                ValidIssuer = jwt.Issuer,
                ValidateAudience = true,
                ValidAudience = jwt.Audience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.FromSeconds(5),
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Secret))
            };
        });
    builder.Services.AddAuthorization();

    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(options =>
    {
        options.SwaggerDoc("v1", new OpenApiInfo
        {
            Title = "PitchReserve API",
            Version = "v1",
            Description = "PitchReserve REST API for pitch reservations, match matchmaking, and payment processing."
        });

        options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
        {
            Name = "Authorization",
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            In = ParameterLocation.Header,
            Description = "Enter JWT Bearer token: Bearer {token}"
        });

        options.AddSecurityRequirement(new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference
                    {
                        Type = ReferenceType.SecurityScheme,
                        Id = "Bearer"
                    }
                },
                Array.Empty<string>()
            }
        });

        var xmlFilename = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
        var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFilename);
        if (File.Exists(xmlPath))
        {
            options.IncludeXmlComments(xmlPath);
        }
    });

    var app = builder.Build();

    app.UseMiddleware<CorrelationContextMiddleware>();
    app.UseMiddleware<RequestCompletionMiddleware>();
    app.UseExceptionHandler();

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI(c =>
        {
            c.SwaggerEndpoint("/swagger/v1/swagger.json", "PitchReserve API v1");
        });
    }

    app.UseHttpsRedirection();
    app.UseRouting();

    app.UseAuthentication();
    app.UseMiddleware<UserContextLoggingMiddleware>();
    app.UseAuthorization();

    app.MapControllers();

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal("Host terminated unexpectedly with {ExceptionType}: {StackTrace}", ex.GetType().FullName, ex.StackTrace);
    Environment.ExitCode = 1;
}
finally
{
    Log.CloseAndFlush();
}

public partial class Program { }
