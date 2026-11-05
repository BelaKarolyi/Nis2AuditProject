using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Nis2AuditGuard.Core.Data;
using Nis2AuditGuard.Core.Domain;
using Nis2AuditGuard.Core.Options;
using Nis2AuditGuard.Core.Services;

namespace Nis2AuditGuard.Tests;

/// <summary>
/// Egység- és integrációs tesztek a NIS2-AuditGuard kulcskomponenseihez (ADR-11).
/// Lefedi: PII-anonimizáló, Hallucináció-őr, Dual-Engine Failover, ADR-09 Free Tier védelem,
/// Előszűrő határértékek, DOCX/XLSX export, AES-256-GCM DRP mentés, PublicWeb és ManagerWeb védelem.
/// </summary>
public sealed class AuditGuardTests
{
    private static readonly Nis2KovetelmenyDto SampleRequirement = new(
        KovetelmenyId: 1,
        KovetelmenyKod: "NIS2-AC-02",
        JogszabalyHivatkozas: "7/2024. (VI. 24.) MK r. Hozzáférés-kezelés (AC)",
        JogszabalySzoveg: "A szervezet köteles dokumentált hozzáférés-kezelési eljárást működtetni és évente felülvizsgálni.",
        ElvartAllapotLeiras: "Éves dokumentált jogosultság-felülvizsgálat.",
        ElvartBizonyitekTipus: "Hozzáférés-kezelési szabályzat + AD jegyzőkönyv");

    [Fact]
    public void PiiAnonymizer_MasksCompanyEmailIpPhoneTax_AndRestoresTokens()
    {
        var sut = new PiiAnonymizerService();
        const string input =
            "Az Alfa Energia Kft. rendszergazdája (admin@alfa.hu, +36 30 555 1234, adószám: 12345678-2-41) " +
            "a 10.20.30.40 címről éri el a dc01.corp.local szervert. Az Alfa Energia jogosultságai érvényesek.";

        var res = sut.Anonymize(input, "Alfa Energia Kft.");

        Assert.DoesNotContain("Alfa Energia", res.Text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("admin@alfa.hu", res.Text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("10.20.30.40", res.Text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("555 1234", res.Text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("12345678-2-41", res.Text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("dc01.corp.local", res.Text, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(PiiAnonymizerService.CompanyToken, res.Text);
        Assert.True(res.TotalReplacements >= 6);

        var restored = res.Restore($"Szervezet: {PiiAnonymizerService.CompanyToken}");
        Assert.Contains("Alfa Energia", restored);
    }

    [Fact]
    public void HallucinationValidator_ValidQuoteAndPage_PassesAndEnforcesSqlLaw()
    {
        var sut = new HallucinationValidatorService();
        var doc = new IndexedDocument(
            "IBSZ.pdf",
            [
                new DocumentPage(1, "Bevezetés és hatály."),
                new DocumentPage(2, "4.2. A felhasználói jogosultságokat évente felül kell vizsgálni.")
            ],
            "abc");

        var ai = new WhiteBoxAiGapResult
        {
            MitAllitAzAi = "Az éves felülvizsgálat elő van írva.",
            MireAlapozzaPontosIdezet = "A felhasználói jogosultságokat évente felül kell vizsgálni.",
            MelyDokumentumbolOldalSzakasz = "IBSZ.pdf (2. oldal, 4.2 szakasz)",
            MelyKovetelmenyhezKapcsolodik = "Kitalált § 999.",
            MennyireBiztosSzazalek = 95
        };

        var validated = sut.ValidateAndEnforceSources(ai, doc, doc.ToIndexedText(), SampleRequirement);

        Assert.True(validated.IdezetValidacioOk);
        Assert.True(validated.OldalValidacioOk);
        Assert.Equal("NIS2-AC-02 – 7/2024. (VI. 24.) MK r. Hozzáférés-kezelés (AC)", validated.MelyKovetelmenyhezKapcsolodik);
        Assert.Equal(SzakertoiStatuszok.Tervezet, validated.StatuszCimke);
    }

    [Fact]
    public void HallucinationValidator_FabricatedQuote_FlagsHallucinationSuspect()
    {
        var sut = new HallucinationValidatorService();
        var doc = new IndexedDocument("IBSZ.pdf", [new DocumentPage(1, "A jelszó hossza legalább 12 karakter.")], "abc");

        var ai = new WhiteBoxAiGapResult
        {
            MireAlapozzaPontosIdezet = "A jogosultságokat blokklánc alapú okosszerződés ellenőrzi.",
            MelyDokumentumbolOldalSzakasz = "1. oldal",
            MitKellASzakertonekEllenoriznie = "Kérje be a naplót."
        };

        var validated = sut.ValidateAndEnforceSources(ai, doc, doc.ToIndexedText(), SampleRequirement);

        Assert.False(validated.IdezetValidacioOk);
        Assert.StartsWith("⚠️ HALLUCINÁCIÓ-GYANÚ:", validated.MitKellASzakertonekEllenoriznie);
    }

    [Fact]
    public void HallucinationValidator_WrongPageNumber_FlagsPageMismatch()
    {
        var sut = new HallucinationValidatorService();
        var doc = new IndexedDocument(
            "IBSZ.pdf",
            [new DocumentPage(1, "Üres."), new DocumentPage(2, "Pontos idézet a második oldalon.")],
            "abc");

        var ai = new WhiteBoxAiGapResult
        {
            MireAlapozzaPontosIdezet = "Pontos idézet a második oldalon.",
            MelyDokumentumbolOldalSzakasz = "IBSZ.pdf, 1. oldal"
        };

        var validated = sut.ValidateAndEnforceSources(ai, doc, doc.ToIndexedText(), SampleRequirement);

        Assert.True(validated.IdezetValidacioOk);
        Assert.False(validated.OldalValidacioOk);
        Assert.StartsWith("⚠️ OLDALSZÁM-ELTÉRÉS:", validated.MitKellASzakertonekEllenoriznie);
    }

    [Fact]
    public async Task DualEngineOrchestrator_PrimaryFailure_AutomaticallyFailsOverToOllama()
    {
        var gemini = new FakeAiEngine("Gemini", shouldThrow: true);
        var ollama = new FakeAiEngine("Ollama", shouldThrow: false, quote: "A jogosultságok felülvizsgálata évente történik.");
        var opts = Options.Create(new AiOptions { GeminiApiKey = "test-key", UsePaidGeminiTier = true });
        var sut = new DualEngineAiOrchestrator(
            [gemini, ollama],
            new PiiAnonymizerService(),
            new HallucinationValidatorService(),
            opts,
            NullLogger<DualEngineAiOrchestrator>.Instance);

        var doc = new IndexedDocument("IBSZ.pdf", [new DocumentPage(1, "A jogosultságok felülvizsgálata évente történik.")], "h");
        var outcome = await sut.AnalyzeDocumentGapAsync(new GapAnalysisInput(
            doc, "Teszt Kft.", BiztonsagiOsztalyok.Alap, SampleRequirement, IsSyntheticDemo: true));

        Assert.Equal(1, gemini.CallCount);
        Assert.Equal(1, ollama.CallCount);
        Assert.True(outcome.LogEntry.FallbackAktivalt);
        Assert.Contains("Ollama Helyi Fallback", outcome.GapResult.HasznaltAiMotor);
        Assert.True(outcome.GapResult.IdezetValidacioOk);
    }

    [Fact]
    public async Task DualEngineOrchestrator_HighSecurityOrFreeTierRealDoc_NeverCallsGemini()
    {
        var gemini = new FakeAiEngine("Gemini", shouldThrow: false);
        var ollama = new FakeAiEngine("Ollama", shouldThrow: false, quote: "Helyi feldolgozású szöveg.");
        var opts = Options.Create(new AiOptions { GeminiApiKey = "test-key", UsePaidGeminiTier = false });
        var sut = new DualEngineAiOrchestrator(
            [gemini, ollama],
            new PiiAnonymizerService(),
            new HallucinationValidatorService(),
            opts,
            NullLogger<DualEngineAiOrchestrator>.Instance);

        var doc = new IndexedDocument("IBSZ.pdf", [new DocumentPage(1, "Helyi feldolgozású szöveg.")], "h");

        // 1. eset: "Magas" biztonsági osztály -> Gemini tilos!
        var highSec = await sut.AnalyzeDocumentGapAsync(new GapAnalysisInput(
            doc, "Teszt Kft.", BiztonsagiOsztalyok.Magas, SampleRequirement, IsSyntheticDemo: true));
        Assert.Equal(0, gemini.CallCount);
        Assert.Contains("Magas biztonsági osztály", highSec.GapResult.HasznaltAiMotor);

        // 2. eset: ADR-09 Ingyenes Gemini kulcs + Valós (nem szintetikus) dokumentum -> Gemini tilos!
        var realDoc = await sut.AnalyzeDocumentGapAsync(new GapAnalysisInput(
            doc, "Teszt Kft.", BiztonsagiOsztalyok.Alap, SampleRequirement, IsSyntheticDemo: false));
        Assert.Equal(0, gemini.CallCount);
        Assert.Contains("Ingyenes Gemini kulcs védelme", realDoc.GapResult.HasznaltAiMotor);
    }

    [Theory]
    [InlineData(49, 9.5, false, "Alap", 3_000_000)]
    [InlineData(50, 5.0, true, "Jelentos", 3_900_000)]
    [InlineData(20, 12.0, true, "Magas", 4_800_000)]
    public void PreScreeningService_BoundariesAndMultipliers_AreCalculatedDeterministically(
        int headcount,
        double revenueMeur,
        bool expectedInScope,
        string securityClass,
        decimal expectedPrice)
    {
        var sut = new PreScreeningService();
        var res = sut.Evaluate(new PreScreeningRequest
        {
            Agazat = "Energetika",
            Melleklet = "1",
            Letszam = headcount,
            EvesArbevetelMillioEur = (decimal)revenueMeur,
            EirDarabszam = 1,
            BiztonsagiOsztaly = securityClass
        });

        Assert.Equal(expectedInScope, res.Erintett);
        Assert.Equal(expectedPrice, res.BecsultFelkeszitesiIranyarHuf);
    }

    [Fact]
    public async Task ExportAndBackupServices_GenerateValidPackagesAndVerifyRestore()
    {
        var options = new DbContextOptionsBuilder<AuditGuardDbContext>()
            .UseSqlite("DataSource=:memory:")
            .Options;

        await using var db = new AuditGuardDbContext(options);
        await db.Database.OpenConnectionAsync();
        await db.Database.EnsureCreatedAsync();

        var client = new UgyfelEsEir
        {
            CegNev = "Teszt Üzem Kft.",
            Agazat = "Feldolgozóipar",
            MellekletBesorolas = "2. melléklet",
            FoglalkoztatottiLetszam = 80,
            EvesNettoArbevetelMillioEur = 15m,
            EirMegnevezes = "MES Rendszer",
            BiztonsagiOsztaly = BiztonsagiOsztalyok.Jelentos,
            EloszuresEredmeny = "NIS2 ÉRINTETT",
            BecsultIranyarHuf = 3_900_000m
        };
        db.Ugyfelek.Add(client);
        await db.SaveChangesAsync();

        var export = new ExportService();
        var docx = export.GenerateExecutiveSummaryDocx(client, [], ExecutiveRole.Ugyvezeto);
        var xlsx = export.GenerateGapMatrixXlsx(client, []);
        Assert.True(docx.Length > 1000 && docx[0] == 'P' && docx[1] == 'K');
        Assert.True(xlsx.Length > 1000 && xlsx[0] == 'P' && xlsx[1] == 'K');

        var backup = new BackupService();
        var pkg = await backup.CreateEncryptedBackupAsync(db, "SzigoruJelszo2026!");
        var okVerify = backup.VerifyBackupAndGenerateProtocol(pkg, "SzigoruJelszo2026!");
        var badVerify = backup.VerifyBackupAndGenerateProtocol(pkg, "HibasJelszo");

        Assert.True(okVerify.Success);
        Assert.Equal(pkg.PlainSha256, okVerify.ActualSha256);
        Assert.True(okVerify.ProtocolDocx.Length > 1000);
        Assert.False(badVerify.Success);
    }

    [Fact]
    public async Task PublicWeb_EnforcesHoneypotTimeTrap_AndZeroPublicExposure()
    {
        await using var factory = new WebApplicationFactory<PublicWebProgram>();
        using var client = factory.CreateClient();

        // 1. Zero Public Exposure: nincs /admin vagy /login végpont.
        var adminResp = await client.GetAsync("/admin");
        Assert.Equal(HttpStatusCode.NotFound, adminResp.StatusCode);

        // 2. Honeypot csapdamező kitöltve -> 400 Bad Request.
        var hpResp = await client.PostAsJsonAsync("/api/prescreen", new PreScreeningRequest
        {
            Agazat = "IT",
            Letszam = 60,
            WebsiteUrlHoneypot = "http://spambot.example",
            UrlapMegnyitasUnixSec = DateTimeOffset.UtcNow.ToUnixTimeSeconds() - 10
        });
        Assert.Equal(HttpStatusCode.BadRequest, hpResp.StatusCode);

        // 3. Time-Trap (< 3 mp) -> 400 Bad Request.
        var fastResp = await client.PostAsJsonAsync("/api/prescreen", new PreScreeningRequest
        {
            Agazat = "IT",
            Letszam = 60,
            UrlapMegnyitasUnixSec = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
        });
        Assert.Equal(HttpStatusCode.BadRequest, fastResp.StatusCode);

        // 4. Érvényes emberi beküldés -> 200 OK.
        var validResp = await client.PostAsJsonAsync("/api/prescreen", new PreScreeningRequest
        {
            Agazat = "IT",
            Letszam = 60,
            EirDarabszam = 1,
            BiztonsagiOsztaly = BiztonsagiOsztalyok.Alap,
            UrlapMegnyitasUnixSec = DateTimeOffset.UtcNow.ToUnixTimeSeconds() - 10
        });
        Assert.Equal(HttpStatusCode.OK, validResp.StatusCode);
    }

    [Fact]
    public async Task ManagerWeb_BlocksExternalIp_AndServesSeededRequirementsOnLoopback()
    {
        await using var factory = new WebApplicationFactory<ManagerWebProgram>();
        using var client = factory.CreateClient();

        // 1. Külső IP szimulálása -> 403 Forbidden (Zero Public Exposure).
        using var extReq = new HttpRequestMessage(HttpMethod.Get, "/api/requirements");
        extReq.Headers.Add("X-Test-Remote-Ip", "203.0.113.42");
        var extResp = await client.SendAsync(extReq);
        Assert.Equal(HttpStatusCode.Forbidden, extResp.StatusCode);

        // 2. Loopback (127.0.0.1) kérés -> 200 OK és mind a 11 NIS2 követelmény elérhető.
        using var localReq = new HttpRequestMessage(HttpMethod.Get, "/api/requirements");
        localReq.Headers.Add("X-Test-Remote-Ip", "127.0.0.1");
        var localResp = await client.SendAsync(localReq);
        Assert.Equal(HttpStatusCode.OK, localResp.StatusCode);
        var body = await localResp.Content.ReadAsStringAsync();
        Assert.Contains("NIS2-AC-02", body);
        Assert.Contains("NIS2-CP-01", body);
        Assert.Contains("NIS2-SC-01", body);
    }

    private sealed class FakeAiEngine(string name, bool shouldThrow, string quote = "NEM TALÁLHATÓ") : IAiEngine
    {
        public string EngineName => name;
        public int CallCount { get; private set; }

        public Task<AiEngineResponse> GenerateGapAnalysisAsync(string systemInstruction, string userPrompt, CancellationToken ct = default)
        {
            CallCount++;
            if (shouldThrow) throw new HttpRequestException("Simulated network timeout.");
            var res = new WhiteBoxAiGapResult
            {
                MitAllitAzAi = "Teszt megállapítás.",
                MireAlapozzaPontosIdezet = quote,
                MelyDokumentumbolOldalSzakasz = "1. oldal",
                MennyireBiztosSzazalek = 90,
                MitKellASzakertonekEllenoriznie = "Ellenőrizendő.",
                JogszabalyAltalEloirtHiany = "Kötelező elem.",
                AiAltalJavasoltJoGyakorlat = "Jó gyakorlat."
            };
            return Task.FromResult(new AiEngineResponse(res, "{}", name));
        }
    }
}

