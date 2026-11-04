using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Nis2AuditGuard.Core.Services;

// NIS2-AuditGuard – Eval és dokumentáció-szinkronizáló segédeszköz.
// Parancsok:
//   extract <bemeneti fájl> <kimeneti .txt>   – szövegkinyerés [OLDAL n] címkékkel
//   sync-docs <docs mappa>                    – a .docx dokumentumok szinkronizálása az ADR-08..13 döntésekkel
if (args.Length >= 3 && args[0] == "extract")
{
    var doc = new DocumentTextExtractor().Extract(args[1], File.ReadAllBytes(args[1]));
    File.WriteAllText(args[2], doc.ToIndexedText());
    Console.WriteLine($"{doc.Pages.Count} oldal, SHA-256: {doc.Sha256}");
    return 0;
}

if (args.Length >= 2 && args[0] == "sync-docs")
{
    SyncDocxFiles(args[1]);
    return 0;
}

Console.WriteLine("Használat: extract <fájl> <kimenet.txt> | sync-docs <docs-mappa>");
return 1;

static void SyncDocxFiles(string docsDir)
{
    // Egységes szövegcserék minden beadandó .docx dokumentumban (a formázás megtartásával).
    (string Old, string New)[] replacements =
    [
        // 1. Keretrendszer (.NET 8 -> .NET 10 LTS, ADR-08)
        ("ASP.NET Core 8.0", "ASP.NET Core 10.0 (.NET 10 LTS)"),
        ("ASP.NET Core 8 ", "ASP.NET Core 10 "),
        ("C# .NET 8", "C# .NET 10 LTS"),
        (".NET 8.0 / ASP.NET Core MVC", ".NET 10.0 LTS / ASP.NET Core Minimal API"),
        ("(.NET 8.0", "(.NET 10.0 LTS"),
        ("(.NET 8)", "(.NET 10 LTS)"),
        (".NET 8", ".NET 10 LTS"),

        // 2. Gemini modell (2.5/1.5 Pro -> gemini-3.8-flash, ADR-09)
        ("Gemini 2.5 Pro/Flash API", "Gemini API (gemini-3.8-flash)"),
        ("Gemini 2.5 Pro / Flash API", "Gemini API (gemini-3.8-flash)"),
        ("Gemini 2.5 Pro", "Gemini (gemini-3.8-flash)"),
        ("Gemini 1.5 Pro API", "Gemini API (gemini-3.8-flash)"),
        ("Gemini 1.5 Pro", "Gemini (gemini-3.8-flash)"),

        // 3. HLD korábbi belső ellentmondásainak javítása (ADR-02, ADR-03, ADR-04, ADR-05, ADR-10, ADR-12)
        ("monolitikus (MVP fázisban) ASP.NET Core webalkalmazás", "kétzónás (PublicWeb + ManagerWeb) ASP.NET Core (.NET 10 LTS) webalkalmazás"),
        ("HTML5, CSS3, Bootstrap 5, Razor Pages (Reszponzív UI)", "HTML5, CSS3, Bootstrap 5 statikus UI (wwwroot) + Minimal API végpontok"),
        ("ASP.NET Core Razor/MVC + Bootstrap 5 + WAF", "ASP.NET Core 10 Minimal API + Bootstrap 5 + RateLimiter"),
        ("a Razor nézetek alapértelmezetten HTML-kódolnak minden kimenetet (XSS ellen)", "a frontend JSON API-n kommunikál és biztonsági fejléceket (CSP) használ (XSS ellen)"),
        ("Temperature = 0.0", "Temperature = 0.1"),
        ("Láthatatlan reCAPTCHA v3 vagy lokális HoneyPot mező (Spam botok ellen).", "Lokális Honeypot csapdamező és 3 másodperces Time-Trap időzítés-ellenőrzés (Spam botok ellen, külső Google reCAPTCHA függés nélkül)."),
        ("Honeypot, időzítés-ellenőrzés, CAPTCHA", "Honeypot és 3 mp Time-Trap időzítés-ellenőrzés"),
        ("SanitizerService", "PiiAnonymizerService"),
        ("(`[COMPANY_NAME]`, `[IP_ADDRESS]`)", "(`[ÜGYFÉL_SZERVEZET]`, `[BELSŐ_IP_CÍM_1]`, `[ANONIM_EMAIL_1]`)"),
        ("Ha a távolság (Levenshtein) túl nagy, a kártya ⚠️ Hallucináció gyanú címkét kap.", "Normalizált szó szerinti kereséssel (`IndexedDocument.FindExact`) és oldalszám-ellenőrzéssel validál; eltéréskor a kártya `IdezetValidacioOk = false` és ⚠️ HALLUCINÁCIÓ-GYANÚ jelzést kap."),
        ("Mentés \"Hallucináció gyanú\" státusszal, logolás", "Mentés \"TERVEZET\" státusszal és IdezetValidacioOk=false (Hallucináció-gyanú) jelzéssel, logolás"),
        ("Auditor Belépés (🔒)", "Zárt Menedzseri Zóna: csak 127.0.0.1 (🔒)"),
        ("(Hitelesített belépés után)", "(Kizárólag 127.0.0.1 Loopback / VPN zónában)"),

        // 4. OpenClaw / Ollama szerepének pontosítása ("B" architektúra, ADR-10)
        ("lokális OpenClaw / Ollama modell", "lokális Ollama modell + opcionális OpenClaw értesítő"),
        ("OpenClaw AI-Gateway + Ollama", "lokális Ollama (és opcionális OpenClaw Discord-értesítő)"),
        ("OpenClaw Gateway + Lokális Ollama", "Lokális Ollama + opcionális OpenClaw értesítő"),
        ("Változat: 1.2 (Véglegesített alapverzió)", "Változat: 1.3 (Szinkronizálva az ADR-08..13 döntésekkel – 2026.11.04.)")
    ];

    foreach (var file in Directory.GetFiles(docsDir, "*.docx", SearchOption.TopDirectoryOnly))
    {
        var changedParagraphs = 0;
        using var wordDoc = WordprocessingDocument.Open(file, true);
        var body = wordDoc.MainDocumentPart?.Document?.Body;
        if (body is null) continue;

        foreach (var para in body.Descendants<Paragraph>())
        {
            var fullText = para.InnerText;
            if (string.IsNullOrEmpty(fullText)) continue;

            var updated = fullText;
            foreach (var (oldVal, newVal) in replacements)
            {
                if (updated.Contains(oldVal, StringComparison.Ordinal))
                    updated = updated.Replace(oldVal, newVal, StringComparison.Ordinal);
            }

            if (!ReferenceEquals(updated, fullText) && updated != fullText)
            {
                RewriteParagraphPreservingFirstRunStyle(para, updated);
                changedParagraphs++;
            }
        }

        // A fő PAD dokumentum verziótörténeti táblázatának kiegészítése az 1.3-as sorral (ha még nincs benne).
        if (Path.GetFileName(file).StartsWith("01_PAD_", StringComparison.OrdinalIgnoreCase))
        {
            if (AppendPadVersionRowIfMissing(body)) changedParagraphs++;
        }

        wordDoc.MainDocumentPart!.Document!.Save();
        Console.WriteLine($"{Path.GetFileName(file)}: {changedParagraphs} bekezdés/táblázatsor frissítve.");
    }
}

static void RewriteParagraphPreservingFirstRunStyle(Paragraph para, string newText)
{
    var runs = para.Elements<Run>().ToList();
    var firstRunProps = runs.FirstOrDefault()?.RunProperties?.CloneNode(true) as RunProperties;
    foreach (var r in runs) r.Remove();

    var newRun = new Run();
    if (firstRunProps is not null) newRun.Append(firstRunProps);
    newRun.Append(new Text(newText) { Space = DocumentFormat.OpenXml.SpaceProcessingModeValues.Preserve });
    para.Append(newRun);
}

static bool AppendPadVersionRowIfMissing(Body body)
{
    var table = body.Descendants<Table>().FirstOrDefault(t => t.InnerText.Contains("Tevékenység") && t.InnerText.Contains("1.2"));
    if (table is null || table.InnerText.Contains("2026.11.04.")) return false;

    var templateRow = table.Elements<TableRow>().LastOrDefault();
    if (templateRow is null) return false;

    var newRow = (TableRow)templateRow.CloneNode(true);
    var cells = newRow.Elements<TableCell>().ToList();
    string[] values =
    [
        "1.3",
        "2026.11.04.",
        "Károlyi Béla (FULABV)",
        "ADR-08..13 átvezetése: .NET 10 LTS solution, gemini-3.8-flash (ingyenes kulcsnál csak szintetikus adat), C# Dual-Engine + lokális Ollama + opcionális OpenClaw értesítő, kétzónás projekt-szétválasztás."
    ];
    for (var i = 0; i < Math.Min(cells.Count, values.Length); i++)
    {
        var p = cells[i].Elements<Paragraph>().FirstOrDefault() ?? cells[i].AppendChild(new Paragraph());
        RewriteParagraphPreservingFirstRunStyle(p, values[i]);
    }
    table.Append(newRow);
    return true;
}

// Golden-Set AI-eredményességmérés (`dotnet run --project tests/Nis2AuditGuard.Eval -- eval`)
// A felső szintű parancskezelő bővítéséhez külön segédfüggvényként is meghívható.
