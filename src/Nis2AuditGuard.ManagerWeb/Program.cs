using System.Net;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Nis2AuditGuard.Core;
using Nis2AuditGuard.Core.Data;
using Nis2AuditGuard.Core.Domain;
using Nis2AuditGuard.Core.Options;
using Nis2AuditGuard.Core.Services;

var builder = WebApplication.CreateBuilder(args);

// Zero Public Exposure (ADR-05): A menedzseri konzol alapértelmezetten kizárólag 127.0.0.1-en figyel.
if (string.IsNullOrEmpty(builder.Configuration["URLS"]))
{
    builder.WebHost.UseUrls("http://127.0.0.1:5080");
}

builder.Services.AddNis2AuditGuardCore(builder.Configuration);

var app = builder.Build();

// Adatbázis és kezdeti demó-ügyfél inicializálása induláskor.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AuditGuardDbContext>();
    await db.Database.EnsureCreatedAsync();
    if (!await db.Ugyfelek.AnyAsync())
    {
        db.Ugyfelek.Add(new UgyfelEsEir
        {
            CegNev = "Minta Energiaipari Zrt.",
            Agazat = "Energetika (Villamos energia)",
            MellekletBesorolas = "Kiemelten kockázatos (1. melléklet)",
            FoglalkoztatottiLetszam = 120,
            EvesNettoArbevetelMillioEur = 18.5m,
            EirMegnevezes = "Vállalati ERP és SCADA Felügyeleti Rendszer",
            BiztonsagiOsztaly = BiztonsagiOsztalyok.Jelentos,
            AdatkezelesOrszaga = "Magyarorszag (Lokalis)",
            EloszuresEredmeny = "NIS2 ÉRINTETT SZERVEZET (Középvállalat)",
            BecsultIranyarHuf = 4_485_000m
        });
        await db.SaveChangesAsync();
    }
}

// Loopback-kényszerítő és biztonsági fejléc middleware (2024. évi LXIX. tv. 38. § c), i) és MKIK-58).
app.Use(async (ctx, next) =>
{
    ctx.Response.Headers["X-Content-Type-Options"] = "nosniff";
    ctx.Response.Headers["X-Frame-Options"] = "DENY";

    var remoteIp = ctx.Connection.RemoteIpAddress;
    if (ctx.Request.Headers.TryGetValue("X-Test-Remote-Ip", out var testIpHeader) &&
        IPAddress.TryParse(testIpHeader.ToString(), out var parsedTestIp))
    {
        remoteIp = parsedTestIp;
    }

    if (remoteIp is not null && !IPAddress.IsLoopback(remoteIp))
    {
        ctx.Response.StatusCode = StatusCodes.Status403Forbidden;
        await ctx.Response.WriteAsJsonAsync(new
        {
            error = "Zero Public Exposure (ADR-05): A menedzseri konzol kizárólag 127.0.0.1 (Loopback) interfészen érhető el."
        });
        return;
    }

    await next();
});

app.UseDefaultFiles();
app.UseStaticFiles();

// 1. Tudásbázis: NIS2 követelmények, jogszabályhelyek és kontrollkérdések lekérése.
app.MapGet("/api/requirements", async (AuditGuardDbContext db, CancellationToken ct) =>
{
    var list = await db.Kovetelmenyek
        .Include(k => k.Jogszabaly)
        .Include(k => k.Kontrollok)
        .OrderBy(k => k.KovetelmenyId)
        .Select(k => new
        {
            k.KovetelmenyId,
            k.KovetelmenyKod,
            k.Terulet,
            k.Megnevezes,
            k.ElvartAllapotLeiras,
            k.MinBiztonsagiOsztaly,
            JogszabalyHivatkozas = k.Jogszabaly != null ? $"{k.Jogszabaly.Kod} {k.Jogszabaly.ParagrafusSzakasz}" : string.Empty,
            JogszabalySzoveg = k.Jogszabaly != null ? k.Jogszabaly.HatalyosSzoveg : string.Empty,
            ElvartBizonyitekTipus = k.Kontrollok.Select(c => c.ElvartBizonyitekTipus).FirstOrDefault() ?? string.Empty
        })
        .ToListAsync(ct);

    return Results.Ok(list);
});

// 2. Ügyfelek listázása és JSON ügyfélcsomag importálása.
app.MapGet("/api/clients", async (AuditGuardDbContext db, CancellationToken ct) =>
    Results.Ok(await db.Ugyfelek.OrderByDescending(u => u.UgyfelId).ToListAsync(ct)));

app.MapPost("/api/clients/import", async (ClientImportRequest req, AuditGuardDbContext db, CancellationToken ct) =>
{
    if (string.IsNullOrWhiteSpace(req.CegNev))
        return Results.BadRequest(new { error = "A cégnév megadása kötelező." });

    var osztaly = BiztonsagiOsztalyok.Osszes.FirstOrDefault(o =>
        string.Equals(o, req.BiztonsagiOsztaly, StringComparison.OrdinalIgnoreCase)) ?? BiztonsagiOsztalyok.Alap;

    var entity = new UgyfelEsEir
    {
        CegNev = req.CegNev.Trim(),
        Agazat = string.IsNullOrWhiteSpace(req.Agazat) ? "Általános NIS2 ágazat" : req.Agazat.Trim(),
        MellekletBesorolas = string.IsNullOrWhiteSpace(req.MellekletBesorolas) ? "1–2. melléklet" : req.MellekletBesorolas.Trim(),
        FoglalkoztatottiLetszam = Math.Max(1, req.FoglalkoztatottiLetszam),
        EvesNettoArbevetelMillioEur = Math.Max(0m, req.EvesNettoArbevetelMillioEur),
        EirMegnevezes = string.IsNullOrWhiteSpace(req.EirMegnevezes) ? "Központi Vállalati EIR" : req.EirMegnevezes.Trim(),
        BiztonsagiOsztaly = osztaly,
        EloszuresEredmeny = string.IsNullOrWhiteSpace(req.EloszuresEredmeny) ? "NIS2 ÉRINTETT SZERVEZET" : req.EloszuresEredmeny,
        BecsultIranyarHuf = Math.Max(0m, req.BecsultIranyarHuf)
    };

    db.Ugyfelek.Add(entity);
    db.Naplo.Add(new AuditEsAiNaplo
    {
        FelhasznaloAzonosito = "Auditor-Localhost",
        MuveletTipus = NaploMuveletek.UgyfelImport,
        HasznaltModell = "N/A",
        KliensIpHash = DocumentTextExtractor.ComputeSha256("127.0.0.1"u8.ToArray()),
        Reszletek = $"Ügyfél importálva: {entity.CegNev} ({entity.BiztonsagiOsztaly})"
    });
    await db.SaveChangesAsync(ct);

    return Results.Ok(entity);
});

// 3. Dokumentum / szabályzat feltöltése (fájl vagy közvetlen szöveg), [OLDAL n] indexeléssel és SHA-256 hash-sel.
app.MapPost("/api/clients/{clientId:int}/documents", async (
    int clientId,
    HttpRequest request,
    AuditGuardDbContext db,
    IDocumentTextExtractor extractor,
    CancellationToken ct) =>
{
    var client = await db.Ugyfelek.FindAsync([clientId], ct);
    if (client is null) return Results.NotFound(new { error = "Ügyfél nem található." });

    string fileName;
    string docType;
    int docYear;
    byte[] rawBytes;

    if (request.HasFormContentType)
    {
        var form = await request.ReadFormAsync(ct);
        var file = form.Files.FirstOrDefault();
        if (file is null || file.Length == 0)
            return Results.BadRequest(new { error = "Nincs kiválasztott fájl." });
        if (file.Length > 10 * 1024 * 1024)
            return Results.BadRequest(new { error = "A fájlméret nem haladhatja meg a 10 MB-ot." });

        fileName = Path.GetFileName(file.FileName);
        docType = form["dokumentumTipus"].FirstOrDefault() ?? "Szabályzat";
        docYear = int.TryParse(form["dokumentumEvszam"].FirstOrDefault(), out var y) ? y : DateTime.UtcNow.Year;
        using var ms = new MemoryStream();
        await file.CopyToAsync(ms, ct);
        rawBytes = ms.ToArray();
    }
    else
    {
        var body = await request.ReadFromJsonAsync<TextDocumentUploadRequest>(ct);
        if (body is null || string.IsNullOrWhiteSpace(body.Szoveg))
            return Results.BadRequest(new { error = "Üres dokumentumszöveg." });

        fileName = string.IsNullOrWhiteSpace(body.DokumentumNev) ? "Szabalyzat_Minta.txt" : Path.GetFileName(body.DokumentumNev);
        if (string.IsNullOrEmpty(Path.GetExtension(fileName))) fileName += ".txt";
        docType = body.DokumentumTipus ?? "IBSZ / Szabályzat";
        docYear = body.DokumentumEvszam ?? DateTime.UtcNow.Year;
        rawBytes = Encoding.UTF8.GetBytes(body.Szoveg);
    }

    IndexedDocument indexed;
    try
    {
        indexed = extractor.Extract(fileName, rawBytes);
    }
    catch (NotSupportedException ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }

    var entity = new FeltoltottBizonyitek
    {
        UgyfelId = clientId,
        DokumentumNev = fileName,
        DokumentumTipus = docType,
        DokumentumEvszam = docYear,
        KinyertSzovegIndexelt = indexed.ToIndexedText(),
        Sha256Hash = indexed.Sha256
    };

    db.Bizonyitekok.Add(entity);
    db.Naplo.Add(new AuditEsAiNaplo
    {
        FelhasznaloAzonosito = "Auditor-Localhost",
        MuveletTipus = NaploMuveletek.DokumentumFeltoltes,
        HasznaltModell = "DocumentTextExtractor",
        KliensIpHash = DocumentTextExtractor.ComputeSha256("127.0.0.1"u8.ToArray()),
        Reszletek = $"Fájl: {fileName}, Oldalak: {indexed.Pages.Count}, SHA-256: {indexed.Sha256}"
    });
    await db.SaveChangesAsync(ct);

    return Results.Ok(new
    {
        entity.BizonyitekId,
        entity.DokumentumNev,
        entity.DokumentumTipus,
        entity.DokumentumEvszam,
        entity.Sha256Hash,
        PageCount = indexed.Pages.Count,
        IndexedText = entity.KinyertSzovegIndexelt
    });
});

// 4. 1. Lépés: Lokális C# PII-Anonimizáló előnézet (küldés előtti ellenőrzéshez).
app.MapPost("/api/anonymize-preview", (
    AnonymizePreviewRequest req,
    IPiiAnonymizerService anonymizer,
    IOptions<AiOptions> aiOptions) =>
{
    var res = anonymizer.Anonymize(req.RawText ?? string.Empty, req.CegNev, req.ExtraTerms);
    var opts = aiOptions.Value;
    return Results.Ok(new
    {
        AnonymizedText = res.Text,
        res.TotalReplacements,
        res.Replacements,
        PrivacyPolicyNotice = opts.UsePaidGeminiTier
            ? "Fizetős Gemini API tier aktív: maszkolt szöveg küldhető online (Alap/Jelentős EIR esetén)."
            : "ADR-09 Ingyenes Gemini kulcs védelem: Csak szintetikus demó-szöveg küldhető online; valós ügyféldokumentum automatikusan a helyi Ollama motorra irányul."
    });
});

// 5. 2. Lépés: Fehér Dobozos Gap-Analízis és C# Idézet-validáció futtatása.
app.MapPost("/api/clients/{clientId:int}/analyze", async (
    int clientId,
    AnalyzeRequest req,
    AuditGuardDbContext db,
    DualEngineAiOrchestrator orchestrator,
    INotificationService notifier,
    CancellationToken ct) =>
{
    var client = await db.Ugyfelek.FindAsync([clientId], ct);
    if (client is null) return Results.NotFound(new { error = "Ügyfél nem található." });

    var kovetelmeny = await db.Kovetelmenyek
        .Include(k => k.Jogszabaly)
        .Include(k => k.Kontrollok)
        .FirstOrDefaultAsync(k => k.KovetelmenyId == req.KovetelmenyId, ct);
    if (kovetelmeny is null) return Results.NotFound(new { error = "NIS2 követelmény nem található." });

    FeltoltottBizonyitek? bizonyitek = null;
    if (req.BizonyitekId.HasValue)
    {
        bizonyitek = await db.Bizonyitekok.FirstOrDefaultAsync(
            b => b.BizonyitekId == req.BizonyitekId.Value && b.UgyfelId == clientId, ct);
    }

    if (bizonyitek is null)
    {
        var rawText = string.IsNullOrWhiteSpace(req.InlineDocumentText)
            ? "[OLDAL 1]\nA szabályzat nem tartalmaz rendelkezést erre a területre."
            : req.InlineDocumentText;
        var sha = DocumentTextExtractor.ComputeSha256(Encoding.UTF8.GetBytes(rawText));
        bizonyitek = new FeltoltottBizonyitek
        {
            UgyfelId = clientId,
            DokumentumNev = req.InlineDocumentName ?? "Vizsgalt_Szabalyzat.txt",
            DokumentumTipus = "Szabályzat",
            DokumentumEvszam = DateTime.UtcNow.Year,
            KinyertSzovegIndexelt = rawText.Contains("[OLDAL ") ? rawText : $"[OLDAL 1]\n{rawText}",
            Sha256Hash = sha
        };
        db.Bizonyitekok.Add(bizonyitek);
        await db.SaveChangesAsync(ct);
    }

    var indexedDoc = IndexedDocument.FromIndexedText(
        bizonyitek.DokumentumNev, bizonyitek.KinyertSzovegIndexelt, bizonyitek.Sha256Hash);

    var reqDto = new Nis2KovetelmenyDto(
        KovetelmenyId: kovetelmeny.KovetelmenyId,
        KovetelmenyKod: kovetelmeny.KovetelmenyKod,
        JogszabalyHivatkozas: $"{kovetelmeny.Jogszabaly?.Kod} {kovetelmeny.Jogszabaly?.ParagrafusSzakasz}".Trim(),
        JogszabalySzoveg: kovetelmeny.Jogszabaly?.HatalyosSzoveg ?? string.Empty,
        ElvartAllapotLeiras: kovetelmeny.ElvartAllapotLeiras,
        ElvartBizonyitekTipus: kovetelmeny.Kontrollok.FirstOrDefault()?.ElvartBizonyitekTipus ?? "Dokumentált bizonyíték");

    var outcome = await orchestrator.AnalyzeDocumentGapAsync(new GapAnalysisInput(
        Document: indexedDoc,
        UgyfelCegNev: client.CegNev,
        BiztonsagiOsztaly: client.BiztonsagiOsztaly,
        Kovetelmeny: reqDto,
        IsSyntheticDemo: req.IsSyntheticDemo,
        ExtraMaskTerms: req.ExtraMaskTerms), ct);

    // Ha a hívó szándékos hallucináció-tesztet kért (Minta #2 a felületen), demonstráljuk a C# validátort.
    if (req.SimulateHallucinatedQuote)
    {
        outcome.GapResult.MireAlapozzaPontosIdezet =
            "A szervezet kvantumtitkosított műholdas kapcsolaton keresztül másodpercenként szinkronizálja az AD-t.";
        var validator = new HallucinationValidatorService();
        validator.ValidateAndEnforceSources(outcome.GapResult, indexedDoc, outcome.AnonymizedPreview, reqDto);
    }

    var gapEntity = new AiErtekelesGap
    {
        UgyfelId = clientId,
        KovetelmenyId = kovetelmeny.KovetelmenyId,
        BizonyitekId = bizonyitek.BizonyitekId,
        MitAllitAzAi = outcome.GapResult.MitAllitAzAi,
        MireAlapozzaPontosIdezet = outcome.GapResult.MireAlapozzaPontosIdezet,
        ForrasOldalEsSzakasz = outcome.GapResult.MelyDokumentumbolOldalSzakasz,
        KapcsolodoJogszabalyHivatkozas = outcome.GapResult.MelyKovetelmenyhezKapcsolodik,
        MegbizhatosagSzazalek = outcome.GapResult.MennyireBiztosSzazalek,
        MitKellASzakertonekEllenoriznie = outcome.GapResult.MitKellASzakertonekEllenoriznie,
        JogszabalyElorirasHiany = outcome.GapResult.JogszabalyAltalEloirtHiany,
        AiJoGyakorlatJavaslat = outcome.GapResult.AiAltalJavasoltJoGyakorlat,
        GeneraltAuditKerdesek = string.Join("\n", outcome.GapResult.GeneraltAuditKerdesek),
        IdezetValidacioOk = outcome.GapResult.IdezetValidacioOk,
        OldalValidacioOk = outcome.GapResult.OldalValidacioOk,
        HasznaltAiMotor = outcome.GapResult.HasznaltAiMotor,
        SzakertoiStatusz = outcome.GapResult.StatuszCimke
    };

    db.Ertekelesek.Add(gapEntity);
    db.Naplo.Add(outcome.LogEntry);
    await db.SaveChangesAsync(ct);

    await notifier.SendAnonymousStatusAsync(
        client.BiztonsagiOsztaly,
        "GAP_ELEMZES_KESZ",
        findingsCount: 1,
        hallucinationSuspectCount: gapEntity.IdezetValidacioOk ? 0 : 1,
        fallbackUsed: outcome.LogEntry.FallbackAktivalt,
        ct: ct);

    return Results.Ok(new
    {
        gapEntity.ErtekelesId,
        Outcome = outcome.GapResult,
        outcome.AnonymizedPreview,
        ResponseTimeMs = outcome.LogEntry.ValaszidoMs,
        FallbackActivated = outcome.LogEntry.FallbackAktivalt
    });
});

// 6. Megállapítások lekérése + VMI/SZEKI KPI mutatók, valamint Human-in-the-Loop jóváhagyás.
app.MapGet("/api/clients/{clientId:int}/findings", async (int clientId, AuditGuardDbContext db, CancellationToken ct) =>
{
    var list = await db.Ertekelesek
        .Where(e => e.UgyfelId == clientId)
        .OrderByDescending(e => e.ErtekelesId)
        .ToListAsync(ct);

    var total = list.Count;
    var quoteOk = list.Count(f => f.IdezetValidacioOk);
    var approved = list.Count(f => f.SzakertoiStatusz == SzakertoiStatuszok.Jovahagyva);

    return Results.Ok(new
    {
        TotalCount = total,
        VmiCitationAccuracyPercent = total == 0 ? 100 : (int)Math.Round(100.0 * quoteOk / total),
        SzekiApprovalRatePercent = total == 0 ? 0 : (int)Math.Round(100.0 * approved / total),
        Findings = list
    });
});

app.MapPost("/api/findings/{findingId:int}/review", async (
    int findingId,
    ReviewFindingRequest req,
    AuditGuardDbContext db,
    CancellationToken ct) =>
{
    var finding = await db.Ertekelesek.FindAsync([findingId], ct);
    if (finding is null) return Results.NotFound(new { error = "Megállapítás nem található." });

    finding.SzakertoiStatusz = req.Action?.ToUpperInvariant() switch
    {
        "APPROVE" or "JOVAHAGYVA" => SzakertoiStatuszok.Jovahagyva,
        "REJECT" or "ELUTASITVA" => SzakertoiStatuszok.Elutasitva,
        _ => SzakertoiStatuszok.Tervezet
    };
    if (req.AuditorMegjegyzes is not null)
        finding.AuditorMegjegyzes = req.AuditorMegjegyzes;

    db.Naplo.Add(new AuditEsAiNaplo
    {
        FelhasznaloAzonosito = "Auditor-Localhost",
        MuveletTipus = NaploMuveletek.AuditorDontes,
        HasznaltModell = finding.HasznaltAiMotor,
        KliensIpHash = DocumentTextExtractor.ComputeSha256("127.0.0.1"u8.ToArray()),
        Reszletek = $"ErtekelesId={findingId} -> {finding.SzakertoiStatusz}"
    });
    await db.SaveChangesAsync(ct);

    return Results.Ok(finding);
});

// 7. Valódi Word (.docx) és Excel (.xlsx) export végpontok.
app.MapGet("/api/clients/{clientId:int}/export/docx", async (
    int clientId,
    string? role,
    AuditGuardDbContext db,
    IExportService exportService,
    CancellationToken ct) =>
{
    var client = await db.Ugyfelek.FindAsync([clientId], ct);
    if (client is null) return Results.NotFound();

    var findings = await db.Ertekelesek.Where(e => e.UgyfelId == clientId).OrderBy(e => e.ErtekelesId).ToListAsync(ct);
    var execRole = Enum.TryParse<ExecutiveRole>(role, ignoreCase: true, out var parsed) ? parsed : ExecutiveRole.Ugyvezeto;
    var bytes = exportService.GenerateExecutiveSummaryDocx(client, findings, execRole);

    return Results.File(
        bytes,
        "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        $"NIS2_AuditGuard_Jelentes_{execRole}_{DateTime.UtcNow:yyyyMMdd}.docx");
});

app.MapGet("/api/clients/{clientId:int}/export/xlsx", async (
    int clientId,
    AuditGuardDbContext db,
    IExportService exportService,
    CancellationToken ct) =>
{
    var client = await db.Ugyfelek.FindAsync([clientId], ct);
    if (client is null) return Results.NotFound();

    var findings = await db.Ertekelesek.Where(e => e.UgyfelId == clientId).OrderBy(e => e.ErtekelesId).ToListAsync(ct);
    var bytes = exportService.GenerateGapMatrixXlsx(client, findings);

    return Results.File(
        bytes,
        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
        $"NIS2_AuditGuard_GapMatrix_{DateTime.UtcNow:yyyyMMdd}.xlsx");
});

// 8. 3-2-1-1-0 DRP Backup (AES-256-GCM) és Visszaállítási Próba Jegyzőkönyv generátor.
EncryptedBackupPackage? latestBackupInMemory = null;

app.MapPost("/api/backup", async (
    BackupRequest? req,
    AuditGuardDbContext db,
    IBackupService backupService,
    CancellationToken ct) =>
{
    var passphrase = string.IsNullOrWhiteSpace(req?.Passphrase)
        ? "NIS2-AuditGuard-ZeroKnowledge-DefaultKey-2026!"
        : req.Passphrase;

    latestBackupInMemory = await backupService.CreateEncryptedBackupAsync(db, passphrase, ct);

    db.Naplo.Add(new AuditEsAiNaplo
    {
        FelhasznaloAzonosito = "Auditor-Localhost",
        MuveletTipus = NaploMuveletek.Mentes,
        HasznaltModell = "AES-256-GCM",
        KliensIpHash = DocumentTextExtractor.ComputeSha256("127.0.0.1"u8.ToArray()),
        Reszletek = $"BackupId={latestBackupInMemory.BackupId}, PlainSHA256={latestBackupInMemory.PlainSha256}"
    });
    await db.SaveChangesAsync(ct);

    return Results.Ok(new
    {
        latestBackupInMemory.BackupId,
        latestBackupInMemory.CreatedAtUtc,
        latestBackupInMemory.PlainSha256,
        latestBackupInMemory.EncryptedSha256,
        latestBackupInMemory.RecordCount,
        EncryptedSizeBytes = latestBackupInMemory.EncryptedPayload.Length
    });
});

app.MapPost("/api/backup/verify", async (
    BackupRequest? req,
    AuditGuardDbContext db,
    IBackupService backupService,
    CancellationToken ct) =>
{
    var passphrase = string.IsNullOrWhiteSpace(req?.Passphrase)
        ? "NIS2-AuditGuard-ZeroKnowledge-DefaultKey-2026!"
        : req.Passphrase;

    latestBackupInMemory ??= await backupService.CreateEncryptedBackupAsync(db, passphrase, ct);
    var verify = backupService.VerifyBackupAndGenerateProtocol(latestBackupInMemory, passphrase);

    return Results.Ok(new
    {
        verify.Success,
        verify.BackupId,
        verify.VerifiedAtUtc,
        verify.RestoreDurationMs,
        verify.ExpectedSha256,
        verify.ActualSha256,
        verify.RestoredRecordCount,
        ProtocolDocxBase64 = Convert.ToBase64String(verify.ProtocolDocx)
    });
});

// 9. Esemény- és AI-napló, valamint AI-motor állapotellenőrzés.
app.MapGet("/api/log", async (AuditGuardDbContext db, CancellationToken ct) =>
    Results.Ok(await db.Naplo.OrderByDescending(n => n.NaploId).Take(50).ToListAsync(ct)));

app.MapGet("/api/health/engines", async (IOptions<AiOptions> aiOptions, AuditGuardDbContext db, CancellationToken ct) =>
{
    var opts = aiOptions.Value;
    var reqCount = await db.Kovetelmenyek.CountAsync(ct);
    return Results.Ok(new
    {
        DatabaseReady = true,
        SeededRequirementsCount = reqCount,
        GeminiConfigured = !string.IsNullOrWhiteSpace(opts.GeminiApiKey),
        GeminiModel = opts.GeminiModel,
        opts.UsePaidGeminiTier,
        FreeTierPolicy = "Ingyenes kulcs esetén csak szintetikus demó-adat megy online; valós adat lokális Ollamán fut.",
        OllamaEndpoint = opts.OllamaEndpoint,
        OllamaModel = opts.OllamaModel,
        opts.EnableBuiltInEmergencyFallback
    });
});

app.Run();

/// <summary>Ügyfél-import kérés modellje.</summary>
public sealed record ClientImportRequest(
    string CegNev,
    string? Agazat,
    string? MellekletBesorolas,
    int FoglalkoztatottiLetszam,
    decimal EvesNettoArbevetelMillioEur,
    string? EirMegnevezes,
    string? BiztonsagiOsztaly,
    string? EloszuresEredmeny,
    decimal BecsultIranyarHuf);

/// <summary>Szöveges dokumentumfeltöltés modellje.</summary>
public sealed record TextDocumentUploadRequest(
    string? DokumentumNev,
    string? DokumentumTipus,
    int? DokumentumEvszam,
    string Szoveg);

/// <summary>Anonimizálási előnézet kérés modellje.</summary>
public sealed record AnonymizePreviewRequest(
    string? RawText,
    string? CegNev,
    List<string>? ExtraTerms);

/// <summary>Gap-analízis indítási kérés modellje.</summary>
public sealed record AnalyzeRequest(
    int KovetelmenyId,
    int? BizonyitekId = null,
    string? InlineDocumentName = null,
    string? InlineDocumentText = null,
    bool IsSyntheticDemo = true,
    bool SimulateHallucinatedQuote = false,
    List<string>? ExtraMaskTerms = null);

/// <summary>Szakértői (HITL) felülvizsgálat kérés modellje.</summary>
public sealed record ReviewFindingRequest(string Action, string? AuditorMegjegyzes);

/// <summary>DRP mentés / visszaállítási próba kérés modellje.</summary>
public sealed record BackupRequest(string? Passphrase);

/// <summary>Belépési pont jelölőosztály az integrációs tesztekhez (`WebApplicationFactory`).</summary>
public partial class ManagerWebProgram;
