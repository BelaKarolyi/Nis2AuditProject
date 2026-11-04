using System.ComponentModel.DataAnnotations;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.RateLimiting;
using Nis2AuditGuard.Core.Domain;
using Nis2AuditGuard.Core.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddAntiforgery(o => o.HeaderName = "X-CSRF-TOKEN");
builder.Services.AddSingleton<IPreScreeningService, PreScreeningService>();

// DDoS és űrlap-túlterhelés elleni védelem: IP-alapú Fixed Window Rate Limiter (max. 5 kérés / perc).
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("PublicFormLimit", context =>
    {
        var ip = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        return RateLimitPartition.GetFixedWindowLimiter(ip, _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 5,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0,
            AutoReplenishment = true
        });
    });
});

var app = builder.Build();

// Biztonsági HTTP fejlécek (OWASP Hardening, MKIK-58).
app.Use(async (ctx, next) =>
{
    ctx.Response.Headers["X-Content-Type-Options"] = "nosniff";
    ctx.Response.Headers["X-Frame-Options"] = "DENY";
    ctx.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    await next();
});

app.UseDefaultFiles();
app.UseStaticFiles();
app.UseRateLimiter();

// Anti-CSRF token lekérése a publikus űrlaphoz.
app.MapGet("/api/antiforgery/token", (IAntiforgery antiforgery, HttpContext ctx) =>
{
    var tokens = antiforgery.GetAndStoreTokens(ctx);
    return Results.Ok(new { requestToken = tokens.RequestToken });
});

// Publikus NIS2 érintettségi előszűrő és irányár-kalkulátor végpont (RateLimit + Honeypot + Time-Trap).
app.MapPost("/api/prescreen", (PreScreeningRequest req, IPreScreeningService service) =>
{
    var nowUnix = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
    if (!service.ValidateAntiBot(req, nowUnix, out var botError))
    {
        return Results.BadRequest(new { error = botError });
    }

    var validationResults = new List<ValidationResult>();
    if (!Validator.TryValidateObject(req, new ValidationContext(req), validationResults, validateAllProperties: true))
    {
        return Results.BadRequest(new { errors = validationResults.Select(v => v.ErrorMessage) });
    }

    var result = service.Evaluate(req);
    return Results.Ok(result);
}).RequireRateLimiting("PublicFormLimit");

app.Run();

/// <summary>Belépési pont jelölőosztály az integrációs tesztekhez (`WebApplicationFactory`).</summary>
public partial class PublicWebProgram;
