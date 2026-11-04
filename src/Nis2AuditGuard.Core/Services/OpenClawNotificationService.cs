using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nis2AuditGuard.Core.Domain;
using Nis2AuditGuard.Core.Options;

namespace Nis2AuditGuard.Core.Services;

/// <summary>
/// Opcionális értesítő szolgáltatás interfésze (OpenClaw Gateway / Discord csatorna, ADR-10).
/// KIZÁRÓLAG anonim metaadatot (darabszám, státusz) továbbítható!
/// </summary>
public interface INotificationService
{
    /// <summary>Anonim metaadat-értesítést küld egy elemzés vagy mentés befejezéséről.</summary>
    /// <param name="biztonsagiOsztaly">Az EIR biztonsági osztálya ("Magas" esetén az értesítés le van tiltva!).</param>
    /// <param name="eventType">Esemény típusa (pl. "GAP_ELEMZES_KESZ", "DRP_VERIFY_OK").</param>
    /// <param name="findingsCount">Megállapítások darabszáma.</param>
    /// <param name="hallucinationSuspectCount">Hallucináció-gyanús elemek száma.</param>
    /// <param name="fallbackUsed">Aktiválódott-e az offline fallback.</param>
    /// <param name="ct">Megszakítási token.</param>
    Task SendAnonymousStatusAsync(
        string biztonsagiOsztaly,
        string eventType,
        int findingsCount,
        int hallucinationSuspectCount,
        bool fallbackUsed,
        CancellationToken ct = default);
}

/// <summary>
/// OpenClaw Gateway / Discord metaadat-értesítő (ADR-10).
/// Biztonsági korlátok (2024. évi LXIX. tv. 9. § és ADR-03):
/// 1. Soha nem fogad és nem továbbít cégnevet, dokumentumszöveget vagy idézetet.
/// 2. „Magas” biztonsági osztályú EIR esetén teljesen néma marad (0 hálózati csomag).
/// </summary>
public sealed class OpenClawNotificationService(
    HttpClient httpClient,
    IOptions<AiOptions> options,
    ILogger<OpenClawNotificationService> logger) : INotificationService
{
    private readonly AiOptions _options = options.Value;

    /// <inheritdoc />
    public async Task SendAnonymousStatusAsync(
        string biztonsagiOsztaly,
        string eventType,
        int findingsCount,
        int hallucinationSuspectCount,
        bool fallbackUsed,
        CancellationToken ct = default)
    {
        if (string.Equals(biztonsagiOsztaly, BiztonsagiOsztalyok.Magas, StringComparison.OrdinalIgnoreCase))
            return;

        if (string.IsNullOrWhiteSpace(_options.OpenClawNotificationEndpoint))
            return;

        var message = $"[NIS2-AuditGuard] Esemény: {eventType} | Osztály: {biztonsagiOsztaly} | " +
                      $"Megállapítások: {findingsCount} db | Idézet-figyelmeztetés: {hallucinationSuspectCount} db | " +
                      $"Offline Fallback: {(fallbackUsed ? "IGEN" : "NEM")}";

        try
        {
            var payload = new { content = message };
            using var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
            using var resp = await httpClient.PostAsync(_options.OpenClawNotificationEndpoint, content, ct);
            resp.EnsureSuccessStatusCode();
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Az opcionális OpenClaw/Discord metaadat-értesítés nem sikerült (az alapfolyamatot nem érinti).");
        }
    }
}
