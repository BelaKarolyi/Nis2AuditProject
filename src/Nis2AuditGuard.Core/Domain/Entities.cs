namespace Nis2AuditGuard.Core.Domain;

// ==========================================================================================
// A 7 táblás relációs adatmodell entitásai (lásd: database/04_SQL_Adatbazis_Modell_es_Seed_*.sql).
// Tudásbázis-lánc: Jogszabály → Követelmény → Kontroll / Bizonyíték / Kérdés.
// ==========================================================================================

/// <summary>Biztonsági osztályok a 2024. évi LXIX. törvény és a 7/2024. (VI. 24.) MK rendelet szerint.</summary>
public static class BiztonsagiOsztalyok
{
    /// <summary>"Alap" biztonsági osztály.</summary>
    public const string Alap = "Alap";

    /// <summary>"Jelentős" biztonsági osztály (ékezet nélkül tárolva).</summary>
    public const string Jelentos = "Jelentos";

    /// <summary>"Magas" biztonsági osztály – ilyenkor tilos a felhős AI használata.</summary>
    public const string Magas = "Magas";

    /// <summary>Az összes megengedett érték (CHECK constraint és validáció).</summary>
    public static readonly string[] Osszes = [Alap, Jelentos, Magas];
}

/// <summary>Szakértői (Human-in-the-Loop) státuszok az AI-megállapításokhoz.</summary>
public static class SzakertoiStatuszok
{
    /// <summary>Alapértelmezett státusz: minden AI-kimenet csak tervezet.</summary>
    public const string Tervezet = "TERVEZET – SZAKÉRTŐI FELÜLVIZSGÁLAT SZÜKSÉGES";

    /// <summary>A szakértő jóváhagyta a megállapítást.</summary>
    public const string Jovahagyva = "JÓVÁHAGYVA – SZAKÉRTŐ ÁLTAL ELLENŐRIZVE";

    /// <summary>A szakértő hamis pozitívként elutasította a megállapítást.</summary>
    public const string Elutasitva = "ELUTASÍTVA – HAMIS POZITÍV";
}

/// <summary>1. tábla: Jogszabályhelyek hatályos, szó szerinti szövege (a tudásbázis legfelső szintje).</summary>
public class Jogszabaly
{
    /// <summary>Elsődleges kulcs.</summary>
    public int JogszabalyId { get; set; }

    /// <summary>Jogszabály rövid kódja, pl. "2024. évi LXIX. tv.".</summary>
    public string Kod { get; set; } = string.Empty;

    /// <summary>A jogszabály hivatalos címe.</summary>
    public string Cim { get; set; } = string.Empty;

    /// <summary>Paragrafus / szakasz megjelölése, pl. "6. § (3) bek.".</summary>
    public string ParagrafusSzakasz { get; set; } = string.Empty;

    /// <summary>A pontos jogszabályi szöveg – az AI nem idézhet memóriából, csak innen.</summary>
    public string HatalyosSzoveg { get; set; } = string.Empty;

    /// <summary>Hatálybalépés dátuma.</summary>
    public DateOnly HatalybaLepes { get; set; } = new(2025, 1, 1);

    /// <summary>A jogszabályhelyhez tartozó követelmények.</summary>
    public List<Kovetelmeny> Kovetelmenyek { get; set; } = [];
}

/// <summary>2. tábla: NIS2 követelmények (Jogszabály → Követelmény).</summary>
public class Kovetelmeny
{
    /// <summary>Elsődleges kulcs.</summary>
    public int KovetelmenyId { get; set; }

    /// <summary>Idegen kulcs a jogszabályhelyre.</summary>
    public int JogszabalyId { get; set; }

    /// <summary>Egyedi követelménykód, pl. "NIS2-AC-02".</summary>
    public string KovetelmenyKod { get; set; } = string.Empty;

    /// <summary>Szakterület, pl. "Hozzáférés-kezelés".</summary>
    public string Terulet { get; set; } = string.Empty;

    /// <summary>Rövid megnevezés.</summary>
    public string Megnevezes { get; set; } = string.Empty;

    /// <summary>Az elvárt (megfelelő) állapot leírása.</summary>
    public string ElvartAllapotLeiras { get; set; } = string.Empty;

    /// <summary>Az a legalacsonyabb biztonsági osztály, amelytől a követelmény kötelező.</summary>
    public string MinBiztonsagiOsztaly { get; set; } = BiztonsagiOsztalyok.Alap;

    /// <summary>Navigációs tulajdonság a jogszabályhelyre.</summary>
    public Jogszabaly? Jogszabaly { get; set; }

    /// <summary>A követelményhez tartozó kontrollok és auditkérdések.</summary>
    public List<KontrollEsKerdes> Kontrollok { get; set; } = [];
}

/// <summary>3. tábla: Kontrollok, elvárt bizonyítékok és auditkérdések.</summary>
public class KontrollEsKerdes
{
    /// <summary>Elsődleges kulcs.</summary>
    public int KontrollId { get; set; }

    /// <summary>Idegen kulcs a követelményre.</summary>
    public int KovetelmenyId { get; set; }

    /// <summary>Az elvárt kontroll leírása.</summary>
    public string ElvartKontrollLeiras { get; set; } = string.Empty;

    /// <summary>Az elvárt auditbizonyíték típusa(i).</summary>
    public string ElvartBizonyitekTipus { get; set; } = string.Empty;

    /// <summary>Felelős szerepkör(ök).</summary>
    public string FelelosSzerepkor { get; set; } = string.Empty;

    /// <summary>Alap auditkérdés.</summary>
    public string AlapAuditKerdes { get; set; } = string.Empty;

    /// <summary>Mélyítő kérdés.</summary>
    public string MelyitoAuditKerdes { get; set; } = string.Empty;

    /// <summary>Bizonyítékbekérő kérdés.</summary>
    public string BizonyitekBekeroKerdes { get; set; } = string.Empty;

    /// <summary>Ellenőrző kérdés.</summary>
    public string EllenorzoAuditKerdes { get; set; } = string.Empty;

    /// <summary>Navigációs tulajdonság a követelményre.</summary>
    public Kovetelmeny? Kovetelmeny { get; set; }
}

/// <summary>4. tábla: Ügyfelek és az általuk üzemeltetett elektronikus információs rendszer (EIR).</summary>
public class UgyfelEsEir
{
    /// <summary>Elsődleges kulcs.</summary>
    public int UgyfelId { get; set; }

    /// <summary>Az ügyfél cégneve (soha nem kerül maszkolatlanul külső AI-hoz).</summary>
    public string CegNev { get; set; } = string.Empty;

    /// <summary>Ágazat a törvény 1. vagy 2. melléklete szerint.</summary>
    public string Agazat { get; set; } = string.Empty;

    /// <summary>Melléklet szerinti besorolás.</summary>
    public string MellekletBesorolas { get; set; } = string.Empty;

    /// <summary>Foglalkoztatotti létszám.</summary>
    public int FoglalkoztatottiLetszam { get; set; }

    /// <summary>Éves nettó árbevétel millió euróban.</summary>
    public decimal EvesNettoArbevetelMillioEur { get; set; }

    /// <summary>Az EIR megnevezése.</summary>
    public string EirMegnevezes { get; set; } = string.Empty;

    /// <summary>Az EIR biztonsági osztálya.</summary>
    public string BiztonsagiOsztaly { get; set; } = BiztonsagiOsztalyok.Alap;

    /// <summary>Az adatkezelés országa (2024. évi LXIX. tv. nyilvántartási kötelezettség).</summary>
    public string AdatkezelesOrszaga { get; set; } = "Magyarorszag (Lokalis)";

    /// <summary>Az előszűrés eredménye.</summary>
    public string EloszuresEredmeny { get; set; } = string.Empty;

    /// <summary>Becsült felkészítési irányár forintban.</summary>
    public decimal BecsultIranyarHuf { get; set; }

    /// <summary>Létrehozás időpontja (UTC).</summary>
    public DateTime LetrehozasIdopontja { get; set; } = DateTime.UtcNow;

    /// <summary>Feltöltött dokumentumok.</summary>
    public List<FeltoltottBizonyitek> Bizonyitekok { get; set; } = [];
}

/// <summary>5. tábla: Feltöltött ügyféldokumentumok és auditbizonyítékok.</summary>
public class FeltoltottBizonyitek
{
    /// <summary>Elsődleges kulcs.</summary>
    public int BizonyitekId { get; set; }

    /// <summary>Idegen kulcs az ügyfélre.</summary>
    public int UgyfelId { get; set; }

    /// <summary>Eredeti fájlnév.</summary>
    public string DokumentumNev { get; set; } = string.Empty;

    /// <summary>Dokumentumtípus, pl. "IBSZ", "Mentési szabályzat".</summary>
    public string DokumentumTipus { get; set; } = string.Empty;

    /// <summary>Kinyert szöveg [OLDAL n] címkékkel.</summary>
    public string KinyertSzovegIndexelt { get; set; } = string.Empty;

    /// <summary>A dokumentum évszáma (elavult bizonyítékok kiszűréséhez).</summary>
    public int DokumentumEvszam { get; set; }

    /// <summary>Az eredeti fájl SHA-256 lenyomata (sértetlenség igazolása).</summary>
    public string Sha256Hash { get; set; } = string.Empty;

    /// <summary>Feltöltés időpontja (UTC).</summary>
    public DateTime FeltoltesIdopontja { get; set; } = DateTime.UtcNow;

    /// <summary>Navigációs tulajdonság az ügyfélre.</summary>
    public UgyfelEsEir? Ugyfel { get; set; }
}

/// <summary>6. tábla: Fehér dobozos AI-értékelések (Gap-analízis megállapítások).</summary>
public class AiErtekelesGap
{
    /// <summary>Elsődleges kulcs.</summary>
    public int ErtekelesId { get; set; }

    /// <summary>Idegen kulcs az ügyfélre.</summary>
    public int UgyfelId { get; set; }

    /// <summary>Idegen kulcs a követelményre.</summary>
    public int KovetelmenyId { get; set; }

    /// <summary>Idegen kulcs a vizsgált dokumentumra.</summary>
    public int BizonyitekId { get; set; }

    /// <summary>1. Mit állít az AI?</summary>
    public string MitAllitAzAi { get; set; } = string.Empty;

    /// <summary>2. Mire alapozza? (szó szerinti idézet)</summary>
    public string MireAlapozzaPontosIdezet { get; set; } = string.Empty;

    /// <summary>3. Mely dokumentumból / oldalról / szakaszból?</summary>
    public string ForrasOldalEsSzakasz { get; set; } = string.Empty;

    /// <summary>4. Mely követelményhez kapcsolódik? (SQL-ből kikényszerítve)</summary>
    public string KapcsolodoJogszabalyHivatkozas { get; set; } = string.Empty;

    /// <summary>5. Mennyire biztos? (0–100%)</summary>
    public int MegbizhatosagSzazalek { get; set; }

    /// <summary>6. Mit kell a szakértőnek ellenőriznie?</summary>
    public string MitKellASzakertonekEllenoriznie { get; set; } = string.Empty;

    /// <summary>Jogszabály által előírt, de hiányzó elem.</summary>
    public string JogszabalyElorirasHiany { get; set; } = string.Empty;

    /// <summary>AI által javasolt jó gyakorlat (nem kötelező előírás).</summary>
    public string AiJoGyakorlatJavaslat { get; set; } = string.Empty;

    /// <summary>Generált auditkérdések (sortöréssel elválasztva).</summary>
    public string GeneraltAuditKerdesek { get; set; } = string.Empty;

    /// <summary>A C# validátor szó szerint megtalálta-e az idézetet.</summary>
    public bool IdezetValidacioOk { get; set; }

    /// <summary>Az idézet a megjelölt oldalon található-e.</summary>
    public bool OldalValidacioOk { get; set; }

    /// <summary>Az eredményt előállító AI-motor.</summary>
    public string HasznaltAiMotor { get; set; } = string.Empty;

    /// <summary>Human-in-the-Loop státusz.</summary>
    public string SzakertoiStatusz { get; set; } = SzakertoiStatuszok.Tervezet;

    /// <summary>Auditori megjegyzés a felülvizsgálathoz.</summary>
    public string? AuditorMegjegyzes { get; set; }

    /// <summary>Értékelés időpontja (UTC).</summary>
    public DateTime ErtekelesIdopontja { get; set; } = DateTime.UtcNow;

    /// <summary>Navigáció: követelmény.</summary>
    public Kovetelmeny? Kovetelmeny { get; set; }

    /// <summary>Navigáció: dokumentum.</summary>
    public FeltoltottBizonyitek? Bizonyitek { get; set; }
}

/// <summary>7. tábla: Esemény- és AI-napló (minden AI-hívás és szakértői döntés visszakereshető).</summary>
public class AuditEsAiNaplo
{
    /// <summary>Elsődleges kulcs.</summary>
    public long NaploId { get; set; }

    /// <summary>Esemény időpontja (UTC).</summary>
    public DateTime IdopontUtc { get; set; } = DateTime.UtcNow;

    /// <summary>A műveletet végző felhasználó / rendszerkomponens.</summary>
    public string FelhasznaloAzonosito { get; set; } = string.Empty;

    /// <summary>Művelettípus, lásd <see cref="NaploMuveletek"/>.</summary>
    public string MuveletTipus { get; set; } = string.Empty;

    /// <summary>Az AI-nak ténylegesen elküldött (anonimizált) prompt.</summary>
    public string? KuldesreKerultAnonimPrompt { get; set; }

    /// <summary>A nyers AI-válasz JSON.</summary>
    public string? NyersAiValaszJson { get; set; }

    /// <summary>Használt modell neve.</summary>
    public string HasznaltModell { get; set; } = string.Empty;

    /// <summary>Aktiválódott-e a helyi fallback.</summary>
    public bool FallbackAktivalt { get; set; }

    /// <summary>Válaszidő ezredmásodpercben.</summary>
    public int ValaszidoMs { get; set; }

    /// <summary>A kliens IP-címének SHA-256 lenyomata (GDPR: nyers IP nem tárolódik).</summary>
    public string KliensIpHash { get; set; } = string.Empty;

    /// <summary>Szabad szöveges részletek (pl. hibaüzenet, szakértői döntés).</summary>
    public string? Reszletek { get; set; }
}

/// <summary>A naplóban használt művelettípusok.</summary>
public static class NaploMuveletek
{
    /// <summary>Publikus előszűrés.</summary>
    public const string Eloszures = "ELOSZURES";

    /// <summary>Ügyfél-adatcsomag importja.</summary>
    public const string UgyfelImport = "UGYFEL_IMPORT";

    /// <summary>Dokumentum feltöltése.</summary>
    public const string DokumentumFeltoltes = "DOKUMENTUM_FELTOLTES";

    /// <summary>AI dokumentumelemzés.</summary>
    public const string AiElemzes = "DOKUMENTUM_AI_ELEMZES";

    /// <summary>Szakértői döntés.</summary>
    public const string AuditorDontes = "AUDITOR_DONTES";

    /// <summary>Exportálás.</summary>
    public const string Export = "EXPORT";

    /// <summary>Biztonsági mentés / visszaállítási próba.</summary>
    public const string Mentes = "MENTES";
}

