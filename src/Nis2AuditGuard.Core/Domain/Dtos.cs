using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Nis2AuditGuard.Core.Domain;

/// <summary>
/// A tudásbázisból (SQL) betöltött követelmény az AI-prompt és a validátor számára.
/// Garantálja, hogy a jogszabályi hivatkozás ne az AI "memóriájából" származzon.
/// </summary>
/// <param name="KovetelmenyId">Adatbázis-azonosító.</param>
/// <param name="KovetelmenyKod">Követelménykód, pl. NIS2-AC-02.</param>
/// <param name="JogszabalyHivatkozas">Pontos jogszabályhely.</param>
/// <param name="JogszabalySzoveg">A jogszabályhely szó szerinti szövege.</param>
/// <param name="ElvartAllapotLeiras">Elvárt állapot.</param>
/// <param name="ElvartBizonyitekTipus">Elvárt bizonyíték típusa.</param>
public sealed record Nis2KovetelmenyDto(
    int KovetelmenyId,
    string KovetelmenyKod,
    string JogszabalyHivatkozas,
    string JogszabalySzoveg,
    string ElvartAllapotLeiras,
    string ElvartBizonyitekTipus);

/// <summary>
/// A "fehér dobozos" AI-értékelés kényszerített JSON-sémája.
/// Lefedi a feladatkiírás 6 kötelező átláthatósági kérdését, és szétválasztja
/// a jogszabályi előírást az AI által javasolt jó gyakorlattól.
/// </summary>
public sealed class WhiteBoxAiGapResult
{
    /// <summary>1. Mit állít az AI a vizsgált dokumentumról?</summary>
    [JsonPropertyName("mit_allit_az_ai")]
    public string MitAllitAzAi { get; set; } = string.Empty;

    /// <summary>2. Mire alapozza? Szó szerinti idézet a dokumentumból, vagy "NEM TALÁLHATÓ".</summary>
    [JsonPropertyName("mire_alapozza_pontos_idezet")]
    public string MireAlapozzaPontosIdezet { get; set; } = string.Empty;

    /// <summary>3. Mely dokumentumból, oldalról, szakaszból származik?</summary>
    [JsonPropertyName("mely_dokumentumbol_oldal_szakasz")]
    public string MelyDokumentumbolOldalSzakasz { get; set; } = string.Empty;

    /// <summary>4. Mely követelményhez kapcsolódik? (a validátor az SQL-ből felülírja)</summary>
    [JsonPropertyName("mely_kovetelmenyhez_kapcsolodik")]
    public string MelyKovetelmenyhezKapcsolodik { get; set; } = string.Empty;

    /// <summary>5. Mennyire biztos az eredmény? (0–100)</summary>
    [JsonPropertyName("mennyire_biztos_szazalek")]
    public int MennyireBiztosSzazalek { get; set; }

    /// <summary>6. Mit kell a humán szakértőnek ellenőriznie?</summary>
    [JsonPropertyName("mit_kell_a_szakertonek_ellenoriznie")]
    public string MitKellASzakertonekEllenoriznie { get; set; } = string.Empty;

    /// <summary>A jogszabály által előírt, de hiányzó elem (üres, ha megfelel).</summary>
    [JsonPropertyName("jogszabaly_altal_eloirt_hiany")]
    public string JogszabalyAltalEloirtHiany { get; set; } = string.Empty;

    /// <summary>Kiegészítő jó gyakorlat (nem kötelező előírás).</summary>
    [JsonPropertyName("ai_altal_javasolt_jo_gyakorlat")]
    public string AiAltalJavasoltJoGyakorlat { get; set; } = string.Empty;

    /// <summary>Generált auditinterjú-kérdések.</summary>
    [JsonPropertyName("generalt_audit_kerdesek")]
    public List<string> GeneraltAuditKerdesek { get; set; } = [];

    /// <summary>A C# validátor szó szerint megtalálta-e az idézetet.</summary>
    [JsonPropertyName("idezet_validacio_ok")]
    public bool IdezetValidacioOk { get; set; }

    /// <summary>Az idézet a megjelölt oldalon szerepel-e.</summary>
    [JsonPropertyName("oldal_validacio_ok")]
    public bool OldalValidacioOk { get; set; }

    /// <summary>Az eredményt előállító AI-motor.</summary>
    [JsonPropertyName("hasznalt_ai_motor")]
    public string HasznaltAiMotor { get; set; } = string.Empty;

    /// <summary>Human-in-the-Loop státuszcímke.</summary>
    [JsonPropertyName("statusz_cimke")]
    public string StatuszCimke { get; set; } = SzakertoiStatuszok.Tervezet;

    /// <summary>Igaz, ha a megállapítás negatív (a dokumentum nem tartalmazza az elvárt elemet).</summary>
    [JsonIgnore]
    public bool IsNegativeFinding =>
        string.IsNullOrWhiteSpace(MireAlapozzaPontosIdezet) ||
        MireAlapozzaPontosIdezet.Contains("NEM TALÁLHATÓ", StringComparison.OrdinalIgnoreCase);
}

/// <summary>A publikus előszűrő űrlap bemenete beépített honeypot és time-trap mezőkkel.</summary>
public sealed class PreScreeningRequest
{
    /// <summary>Ágazat a törvény mellékletei szerint.</summary>
    [Required(ErrorMessage = "Az ágazat kiválasztása kötelező.")]
    [StringLength(150)]
    public string Agazat { get; set; } = string.Empty;

    /// <summary>Melléklet: "1" (kiemelten kockázatos) vagy "2" (kockázatos), vagy "nincs".</summary>
    [StringLength(10)]
    public string Melleklet { get; set; } = "1";

    /// <summary>Foglalkoztatotti létszám.</summary>
    [Range(1, 1_000_000, ErrorMessage = "Érvényes létszámot adjon meg.")]
    public int Letszam { get; set; }

    /// <summary>Éves nettó árbevétel millió euróban.</summary>
    [Range(0, 1_000_000)]
    public decimal EvesArbevetelMillioEur { get; set; }

    /// <summary>Éves mérlegfőösszeg millió euróban.</summary>
    [Range(0, 1_000_000)]
    public decimal MerlegfoosszegMillioEur { get; set; }

    /// <summary>Üzemeltetett EIR-ek száma.</summary>
    [Range(1, 500)]
    public int EirDarabszam { get; set; } = 1;

    /// <summary>Legmagasabb biztonsági osztály.</summary>
    [AllowedValues(BiztonsagiOsztalyok.Alap, BiztonsagiOsztalyok.Jelentos, BiztonsagiOsztalyok.Magas)]
    public string BiztonsagiOsztaly { get; set; } = BiztonsagiOsztalyok.Alap;

    /// <summary>Rejtett honeypot mező: ember nem tölti ki, bot igen.</summary>
    public string? WebsiteUrlHoneypot { get; set; }

    /// <summary>Az űrlap megnyitásának Unix-időbélyege (time-trap).</summary>
    public long UrlapMegnyitasUnixSec { get; set; }
}

/// <summary>Az előszűrés determinisztikus eredménye.</summary>
/// <param name="Erintett">Érintett-e a szervezet méret és ágazat alapján.</param>
/// <param name="Erintettseg">Szöveges értékelés.</param>
/// <param name="Kategoria">Méretkategória (Mikro/Kis, Közép, Nagy).</param>
/// <param name="JogszabalyiAlap">Az értékelés jogszabályi alapja.</param>
/// <param name="BecsultFelkeszitesiIranyarHuf">Becsült irányár forintban.</param>
/// <param name="Statusz">Kötelező tervezet-címke.</param>
public sealed record PreScreeningResult(
    bool Erintett,
    string Erintettseg,
    string Kategoria,
    string JogszabalyiAlap,
    decimal BecsultFelkeszitesiIranyarHuf,
    string Statusz);

