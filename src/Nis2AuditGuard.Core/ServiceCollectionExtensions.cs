using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Nis2AuditGuard.Core.Data;
using Nis2AuditGuard.Core.Options;
using Nis2AuditGuard.Core.Services;

namespace Nis2AuditGuard.Core;

/// <summary>Dependency Injection regisztrációs segédmetódusok a NIS2-AuditGuard Core szolgáltatásaihoz.</summary>
public static class ServiceCollectionExtensions
{
    /// <summary>Regisztrálja az adatbázis-kontextust, az anonimizálót, a validátort, az AI-motorokat, az exportot és a mentést.</summary>
    /// <param name="services">A DI szolgáltatásgyűjtemény.</param>
    /// <param name="configuration">Az alkalmazás konfigurációja.</param>
    public static IServiceCollection AddNis2AuditGuardCore(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<AiOptions>(configuration.GetSection(AiOptions.SectionName));

        var provider = configuration["Database:Provider"] ?? "Sqlite";
        var connectionString = configuration.GetConnectionString("DefaultConnection")
                               ?? "Data Source=nis2auditguard.db";

        services.AddDbContext<AuditGuardDbContext>(opts =>
        {
            if (string.Equals(provider, "SqlServer", StringComparison.OrdinalIgnoreCase))
            {
                opts.UseSqlServer(connectionString);
            }
            else
            {
                opts.UseSqlite(connectionString);
            }
        });

        services.AddSingleton<IPreScreeningService, PreScreeningService>();
        services.AddSingleton<IPiiAnonymizerService, PiiAnonymizerService>();
        services.AddSingleton<IDocumentTextExtractor, DocumentTextExtractor>();
        services.AddSingleton<IHallucinationValidatorService, HallucinationValidatorService>();
        services.AddSingleton<IExportService, ExportService>();
        services.AddSingleton<IBackupService, BackupService>();

        services.AddHttpClient<GeminiAiEngine>(c => c.Timeout = TimeSpan.FromSeconds(15));
        services.AddHttpClient<OllamaAiEngine>(c => c.Timeout = TimeSpan.FromSeconds(200));
        services.AddTransient<IAiEngine>(sp => sp.GetRequiredService<GeminiAiEngine>());
        services.AddTransient<IAiEngine>(sp => sp.GetRequiredService<OllamaAiEngine>());
        services.AddScoped<DualEngineAiOrchestrator>();

        services.AddHttpClient<INotificationService, OpenClawNotificationService>();

        return services;
    }
}

