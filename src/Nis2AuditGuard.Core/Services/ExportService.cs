using ClosedXML.Excel;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Nis2AuditGuard.Core.Domain;

namespace Nis2AuditGuard.Core.Services;

/// <summary>Az exportálandó vezetői összefoglaló célközönsége (3 szintű jelentés).</summary>
public enum ExecutiveRole
{
    /// <summary>Ügyvezető / Felsővezetés (üzleti kockázat, bírság-kitettség, erőforrásigény).</summary>
    Ugyvezeto,

    /// <summary>IT Vezető / IBF (konkrét technikai kontrollok, architektúra, határidők).</summary>
    ItVezeto,

    /// <summary>Kiberbiztonsági Auditor (6-mezős bizonyítékok, szó szerinti idézetek, §-ok).</summary>
    Auditor
}

/// <summary>Word (.docx) és Excel (.xlsx) exportáló szolgáltatás interfésze.</summary>
public interface IExportService
{
    /// <summary>Előállítja a 3-szintű vezetői összefoglalót és részletes Gap-jelentést valódi .docx formátumban.</summary>
    /// <param name="ugyfel">Az érintett ügyfél és EIR adatai.</param>
    /// <param name="findings">A rögzített fehér dobozos AI-értékelések.</param>
    /// <param name="role">A kiválasztott vezetői szerepkör.</param>
    byte[] GenerateExecutiveSummaryDocx(UgyfelEsEir ugyfel, IReadOnlyList<AiErtekelesGap> findings, ExecutiveRole role = ExecutiveRole.Ugyvezeto);

    /// <summary>Előállítja a teljes, 6 átláthatósági mezőt tartalmazó Gap-analízis mátrixot valódi .xlsx formátumban.</summary>
    /// <param name="ugyfel">Az érintett ügyfél és EIR adatai.</param>
    /// <param name="findings">A rögzített fehér dobozos AI-értékelések.</param>
    byte[] GenerateGapMatrixXlsx(UgyfelEsEir ugyfel, IReadOnlyList<AiErtekelesGap> findings);
}

/// <summary>
/// Valódi OpenXML (`.docx`) és ClosedXML (`.xlsx`) exportáló szolgáltatás.
/// Minden exportált dokumentum tartalmazza a kötelező "TERVEZET – SZAKÉRTŐI FELÜLVIZSGÁLAT SZÜKSÉGES"
/// vagy a szakértő által jóváhagyott státuszt, a 6 átláthatósági elemet és a kötelező vs. jó gyakorlat elkülönítését.
/// </summary>
public sealed class ExportService : IExportService
{
    /// <inheritdoc />
    public byte[] GenerateExecutiveSummaryDocx(
        UgyfelEsEir ugyfel,
        IReadOnlyList<AiErtekelesGap> findings,
        ExecutiveRole role = ExecutiveRole.Ugyvezeto)
    {
        ArgumentNullException.ThrowIfNull(ugyfel);
        ArgumentNullException.ThrowIfNull(findings);

        using var ms = new MemoryStream();
        using (var doc = WordprocessingDocument.Create(ms, WordprocessingDocumentType.Document, true))
        {
            var main = doc.AddMainDocumentPart();
            var body = new Body();
            main.Document = new Document(body);

            AddHeading(body, "NIS2-AuditGuard – Előaudit és Gap-Analízis Jelentés", "28", true);
            AddParagraph(body, $"Státusz: { SzakertoiStatuszok.Tervezet }", bold: true, colorHex: "B45309");
            AddParagraph(body, $"Szervezet: {ugyfel.CegNev} | Ágazat: {ugyfel.Agazat} ({ugyfel.MellekletBesorolas})");
            AddParagraph(body, $"Vizsgált EIR: {ugyfel.EirMegnevezes} | Biztonsági osztály: {ugyfel.BiztonsagiOsztaly} | Adatkezelés helye: {ugyfel.AdatkezelesOrszaga}");
            AddParagraph(body, $"Generálás időpontja (UTC): {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}");

            var total = findings.Count;
            var approved = findings.Count(f => f.SzakertoiStatusz == SzakertoiStatuszok.Jovahagyva);
            var quoteOk = findings.Count(f => f.IdezetValidacioOk);
            var vmi = total == 0 ? 100 : (int)Math.Round(100.0 * quoteOk / total);
            var szeki = total == 0 ? 0 : (int)Math.Round(100.0 * approved / total);

            AddHeading(body, $"1. Vezetői Összefoglaló ({FormatRoleTitle(role)})", "24", true);
            AddParagraph(body, BuildRoleSpecificSummary(ugyfel, findings, role, vmi, szeki));

            AddHeading(body, "2. Részletes Fehér Dobozos Megállapítások (6 Átláthatósági Elem)", "24", true);
            var idx = 1;
            foreach (var f in findings)
            {
                AddHeading(body, $"{idx++}. [{f.KapcsolodoJogszabalyHivatkozas}] – Státusz: {f.SzakertoiStatusz}", "20", true);
                AddParagraph(body, $"1) Mit állít az AI? {f.MitAllitAzAi}");
                AddParagraph(body, $"2) Mire alapozza (szó szerinti idézet)? \"{f.MireAlapozzaPontosIdezet}\"");
                AddParagraph(body, $"3) Forrás dokumentum / oldal / szakasz: {f.ForrasOldalEsSzakasz} | C# Idézet-validáció: {(f.IdezetValidacioOk ? "OK (Szó szerint megtalálva)" : "⚠️ HALLUCINÁCIÓ-GYANÚ")}");
                AddParagraph(body, $"4) Kapcsolódó NIS2 követelmény: {f.KapcsolodoJogszabalyHivatkozas}");
                AddParagraph(body, $"5) Megbízhatóság: {f.MegbizhatosagSzazalek}% | AI-motor: {f.HasznaltAiMotor}");
                AddParagraph(body, $"6) Mit kell a szakértőnek ellenőriznie? {f.MitKellASzakertonekEllenoriznie}");
                AddParagraph(body, $"• Kötelező jogszabályi előírás / hiány: {f.JogszabalyElorirasHiany}", bold: true);
                AddParagraph(body, $"• AI által javasolt jó gyakorlat (nem kötelező): {f.AiJoGyakorlatJavaslat}");
                if (!string.IsNullOrWhiteSpace(f.AuditorMegjegyzes))
                    AddParagraph(body, $"• Auditori megjegyzés: {f.AuditorMegjegyzes}", bold: true);
            }

            main.Document.Save();
        }
        return ms.ToArray();
    }

    /// <inheritdoc />
    public byte[] GenerateGapMatrixXlsx(UgyfelEsEir ugyfel, IReadOnlyList<AiErtekelesGap> findings)
    {
        ArgumentNullException.ThrowIfNull(ugyfel);
        ArgumentNullException.ThrowIfNull(findings);

        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("NIS2_Gap_Matrix");

        string[] headers =
        [
            "ID",
            "Ügyfél / EIR",
            "Biztonsági osztály",
            "4. Követelmény és Jogszabály (§)",
            "1. Mit állít az AI?",
            "2. Mire alapozza (Pontos idézet)",
            "3. Forrás (Dokumentum / Oldal)",
            "5. Bizonyosság (%)",
            "6. Mit ellenőrizzen a szakértő?",
            "Kötelező jogszabályi hiány",
            "AI jó gyakorlat (Best practice)",
            "C# Idézet-validáció OK?",
            "Használt AI-motor",
            "Szakértői (HITL) Státusz",
            "Auditori megjegyzés"
        ];

        for (var c = 0; c < headers.Length; c++)
        {
            var cell = ws.Cell(1, c + 1);
            cell.Value = headers[c];
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#1E293B");
            cell.Style.Font.FontColor = XLColor.White;
        }

        for (var r = 0; r < findings.Count; r++)
        {
            var f = findings[r];
            var row = r + 2;
            ws.Cell(row, 1).Value = f.ErtekelesId;
            ws.Cell(row, 2).Value = $"{ugyfel.CegNev} – {ugyfel.EirMegnevezes}";
            ws.Cell(row, 3).Value = ugyfel.BiztonsagiOsztaly;
            ws.Cell(row, 4).Value = f.KapcsolodoJogszabalyHivatkozas;
            ws.Cell(row, 5).Value = f.MitAllitAzAi;
            ws.Cell(row, 6).Value = f.MireAlapozzaPontosIdezet;
            ws.Cell(row, 7).Value = f.ForrasOldalEsSzakasz;
            ws.Cell(row, 8).Value = f.MegbizhatosagSzazalek;
            ws.Cell(row, 9).Value = f.MitKellASzakertonekEllenoriznie;
            ws.Cell(row, 10).Value = f.JogszabalyElorirasHiany;
            ws.Cell(row, 11).Value = f.AiJoGyakorlatJavaslat;
            ws.Cell(row, 12).Value = f.IdezetValidacioOk ? "IGEN (Hitelesített)" : "NEM (Hallucináció-gyanú)";
            ws.Cell(row, 13).Value = f.HasznaltAiMotor;
            ws.Cell(row, 14).Value = f.SzakertoiStatusz;
            ws.Cell(row, 15).Value = f.AuditorMegjegyzes ?? string.Empty;
        }

        ws.Columns().AdjustToContents(1, 60);
        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return ms.ToArray();
    }

    private static string FormatRoleTitle(ExecutiveRole role) => role switch
    {
        ExecutiveRole.Ugyvezeto => "Ügyvezetői / Igazgatósági Nézet",
        ExecutiveRole.ItVezeto => "IT Vezetői / IBF Technikai Nézet",
        _ => "Kiberbiztonsági Auditori Bizonyíték-Nézet"
    };

    private static string BuildRoleSpecificSummary(
        UgyfelEsEir ugyfel,
        IReadOnlyList<AiErtekelesGap> findings,
        ExecutiveRole role,
        int vmi,
        int szeki) => role switch
    {
        ExecutiveRole.Ugyvezeto =>
            $"A(z) {ugyfel.CegNev} ({ugyfel.EirMegnevezes}, '{ugyfel.BiztonsagiOsztaly}' osztály) előzetes NIS2 gap-elemzése során {findings.Count} db kontrollpont került kiértékelésre. " +
            $"A 2024. évi LXIX. törvény alapján a felsővezetés személyesen felel a kockázatmenedzsment jóváhagyásáért és az éves kiberbiztonsági képzésért. " +
            $"Forrás-hitelességi mutató (VMI): {vmi}%, Szakértői jóváhagyási arány (SZEKI): {szeki}%. Becsült felkészítési költségkeret: {ugyfel.BecsultIranyarHuf:N0} Ft.",
        ExecutiveRole.ItVezeto =>
            $"Technikai összefoglaló a(z) {ugyfel.EirMegnevezes} rendszerhez ({findings.Count} vizsgált kontroll). " +
            $"Kiemelt műszaki fókuszpontok: 7/2024. MK rendelet szerinti MFA kikényszerítés privilegizált fiókokra, 3-2-1-1-0 air-gap mentés és dokumentált visszaállítási próba, " +
            $"valamint az adminisztratív felületek publikus internetről történő leválasztása (Zero Public Exposure).",
        _ =>
            $"Auditori munkapéldány (NIST SP 800-53A Rev 5 – Examine / Interview / Test módszertan). " +
            $"Összes megállapítás: {findings.Count} db, ebből C# idézet-validátor által szó szerint igazolt: {findings.Count(f => f.IdezetValidacioOk)} db ({vmi}%). " +
            $"Jóváhagyott: {findings.Count(f => f.SzakertoiStatusz == SzakertoiStatuszok.Jovahagyva)} db. Minden 'TERVEZET' státuszú tétel manuális bizonyíték-ellenőrzést igényel."
    };

    private static void AddHeading(Body body, string text, string fontSizeHalfPt, bool bold)
    {
        var run = new Run(new RunProperties(new Bold { Val = bold }, new FontSize { Val = fontSizeHalfPt }),
            new Text(text) { Space = SpaceProcessingModeValues.Preserve });
        body.Append(new Paragraph(run));
    }

    private static void AddParagraph(Body body, string text, bool bold = false, string? colorHex = null)
    {
        var props = new RunProperties();
        if (bold) props.Append(new Bold());
        if (colorHex is not null) props.Append(new Color { Val = colorHex });
        body.Append(new Paragraph(new Run(props, new Text(text) { Space = SpaceProcessingModeValues.Preserve })));
    }
}

