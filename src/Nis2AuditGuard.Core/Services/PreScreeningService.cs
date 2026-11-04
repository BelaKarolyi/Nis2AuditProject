using Nis2AuditGuard.Core.Domain;

namespace Nis2AuditGuard.Core.Services;

/// <summary>A publikus NIS2 érintettségi előszűrő és irányár-kalkulátor interfésze.</summary>
public interface IPreScreeningService
{
    /// <summary>Ellenőrzi, hogy a beküldés nem automatizált spam-botból érkezett-e (Honeypot + Time-Trap).</summary>
    /// <param name="request">A beküldött űrlapmodell.</param>
    /// <param name="nowUnixSec">Az ellenőrzés aktuális Unix-időbélyege (tesztelhetőséghez paraméterezhető).</param>
    /// <param name="errorMessage">Hibaüzenet blokkolás esetén.</param>
    bool ValidateAntiBot(PreScreeningRequest request, long nowUnixSec, out string? errorMessage);

    /// <summary>Kiszámítja a determinisztikus NIS2 érintettséget és a becsült felkészítési irányárat.</summary>
    /// <param name="request">A beküldött űrlapmodell.</param>
    PreScreeningResult Evaluate(PreScreeningRequest request);
}

/// <summary>
/// Determinisztikus NIS2 érintettségi előszűrő és irányár-kalkulátor szolgáltatás
/// a 2024. évi LXIX. törvény méret- és ágazati szabályai, valamint az 1/2025. SZTFH rendelet szorzói alapján.
/// Az AI SOHA nem számol hatósági küszöbértéket vagy árat – azt ez a C# szolgáltatás végzi.
/// </summary>
public sealed class PreScreeningService : IPreScreeningService
{
    /// <summary>A minimális emberi kitöltési idő másodpercben (Time-Trap botvédelem).</summary>
    public const int MinFillTimeSeconds = 3;

    /// <inheritdoc />
    public bool ValidateAntiBot(PreScreeningRequest request, long nowUnixSec, out string? errorMessage)
    {
        if (!string.IsNullOrEmpty(request.WebsiteUrlHoneypot))
        {
            errorMessage = "Automatizált spam-beküldés észlelve (Honeypot csapdamező kitöltve).";
            return false;
        }

        if (request.UrlapMegnyitasUnixSec <= 0 || (nowUnixSec - request.UrlapMegnyitasUnixSec) < MinFillTimeSeconds)
        {
            errorMessage = "Automatizált spam-beküldés észlelve (Time-Trap: túl gyors űrlapbeküldés).";
            return false;
        }

        errorMessage = null;
        return true;
    }

    /// <inheritdoc />
    public PreScreeningResult Evaluate(PreScreeningRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var nagyvallalat = request.Letszam >= 250 ||
                           (request.EvesArbevetelMillioEur > 50m && request.MerlegfoosszegMillioEur > 43m);

        var kozepvallalat = !nagyvallalat &&
                            (request.Letszam >= 50 ||
                             request.EvesArbevetelMillioEur >= 10m ||
                             request.MerlegfoosszegMillioEur >= 10m);

        var agazatErintett = !string.Equals(request.Melleklet, "nincs", StringComparison.OrdinalIgnoreCase);
        var erintett = agazatErintett && (nagyvallalat || kozepvallalat);

        var kategoria = nagyvallalat
            ? "Nagyvállalat (Kiemelten fontos / Fontos szervezet)"
            : kozepvallalat
                ? "Középvállalat (Fontos / Érintett szervezet)"
                : "Mikro- vagy kisvállalkozás (Méretküszöb alatti)";

        var erintettseg = erintett
            ? $"NIS2 ÉRINTETT SZERVEZET – {kategoria} (2024. évi LXIX. tv. hatálya alá tartozik)"
            : agazatErintett
                ? "Méretküszöb alatti szervezet (Kivételes szabályok és beszállítói lánc – 17. § – ellenőrzése javasolt!)"
                : "Nem NIS2 melléklet szerinti ágazat (Közvetett beszállítói érintettség vizsgálata javasolt)";

        // Irányár-becslés: alapdíj + EIR darabszám felár, szorozva a biztonsági osztály (1/2025. SZTFH r.) faktorával.
        var alapAr = 3_000_000m;
        var eirFelar = Math.Max(0, request.EirDarabszam - 1) * 450_000m;
        var osztalySzorzo = request.BiztonsagiOsztaly switch
        {
            BiztonsagiOsztalyok.Magas => 1.6m,
            BiztonsagiOsztalyok.Jelentos => 1.3m,
            _ => 1.0m
        };
        var becsultIranyar = Math.Round((alapAr + eirFelar) * osztalySzorzo, 0);

        return new PreScreeningResult(
            Erintett: erintett,
            Erintettseg: erintettseg,
            Kategoria: kategoria,
            JogszabalyiAlap: "2024. évi LXIX. törvény 1–2. §, 1–2. melléklet és 1/2025. SZTFH rendelet",
            BecsultFelkeszitesiIranyarHuf: becsultIranyar,
            Statusz: "ELŐZETES TERVEZET – TANÁCSADÓI EGYEZTETÉS SZÜKSÉGES");
    }
}
