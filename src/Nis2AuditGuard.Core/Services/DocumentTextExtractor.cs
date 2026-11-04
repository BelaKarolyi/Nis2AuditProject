using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using UglyToad.PdfPig;
using UglyToad.PdfPig.DocumentLayoutAnalysis.TextExtractor;

namespace Nis2AuditGuard.Core.Services;

/// <summary>Egy oldal (vagy DOCX esetén szakasz) kinyert szövege.</summary>
/// <param name="Page">1-től számozott oldal / szakasz sorszáma.</param>
/// <param name="Text">Az oldal szövege.</param>
public sealed record DocumentPage(int Page, string Text);

/// <summary>Idézet-találat a dokumentumban.</summary>
/// <param name="Page">Az oldal, amelyen az idézet szerepel.</param>
public sealed record QuoteHit(int Page);

/// <summary>
/// Oldalakra bontott, indexelt dokumentum. Az oldalcímkék ([OLDAL n]) teszik lehetővé,
/// hogy az AI pontos forráshelyet adjon meg, és hogy a validátor ezt ellenőrizze.
/// </summary>
public sealed partial class IndexedDocument
{
    private readonly List<(int Page, string Normalized)> _normalizedPages;

    /// <summary>Létrehoz egy indexelt dokumentumot az oldalak listájából.</summary>
    /// <param name="fileName">Eredeti fájlnév.</param>
    /// <param name="pages">Az oldalak.</param>
    /// <param name="sha256">Az eredeti bájtsorozat SHA-256 lenyomata.</param>
    public IndexedDocument(string fileName, IReadOnlyList<DocumentPage> pages, string sha256)
    {
        FileName = fileName;
        Pages = pages;
        Sha256 = sha256;
        _normalizedPages = pages.Select(p => (p.Page, Normalize(p.Text))).ToList();
    }

    /// <summary>Eredeti fájlnév.</summary>
    public string FileName { get; }

    /// <summary>Az oldalak.</summary>
    public IReadOnlyList<DocumentPage> Pages { get; }

    /// <summary>SHA-256 lenyomat (hexadecimális, kisbetűs).</summary>
    public string Sha256 { get; }

    /// <summary>A teljes szöveg [OLDAL n] címkékkel – ez kerül adatbázisba és az AI-promptba.</summary>
    public string ToIndexedText()
    {
        var sb = new StringBuilder();
        foreach (var p in Pages)
        {
            sb.Append("[OLDAL ").Append(p.Page).AppendLine("]");
            sb.AppendLine(p.Text.Trim());
        }
        return sb.ToString();
    }

    /// <summary>
    /// Szó szerinti (whitespace- és kis/nagybetű-normalizált) idézetkeresés.
    /// Oldalhatáron átnyúló idézetet is megtalál (ilyenkor a kezdőoldalt adja vissza).
    /// </summary>
    /// <param name="quote">Az AI által visszaadott idézet.</param>
    /// <returns>A találat oldala, vagy null, ha az idézet nem szerepel a dokumentumban.</returns>
    public QuoteHit? FindExact(string? quote)
    {
        var q = Normalize(quote);
        if (q.Length == 0) return null;

        foreach (var (page, text) in _normalizedPages)
        {
            if (text.Contains(q, StringComparison.OrdinalIgnoreCase)) return new QuoteHit(page);
        }

        // Oldalhatáron átnyúló idézet: két szomszédos oldal összefűzve.
        for (var i = 0; i < _normalizedPages.Count - 1; i++)
        {
            var joined = _normalizedPages[i].Normalized + " " + _normalizedPages[i + 1].Normalized;
            if (joined.Contains(q, StringComparison.OrdinalIgnoreCase)) return new QuoteHit(_normalizedPages[i].Page);
        }
        return null;
    }

    /// <summary>Visszaállít egy indexelt dokumentumot az adatbázisban tárolt [OLDAL n] címkés szövegből.</summary>
    /// <param name="fileName">Fájlnév.</param>
    /// <param name="indexedText">Címkézett szöveg.</param>
    /// <param name="sha256">Tárolt lenyomat.</param>
    public static IndexedDocument FromIndexedText(string fileName, string indexedText, string sha256)
    {
        var pages = new List<DocumentPage>();
        var matches = PageTagRegex().Matches(indexedText);
        if (matches.Count == 0)
        {
            pages.Add(new DocumentPage(1, indexedText));
        }
        else
        {
            for (var i = 0; i < matches.Count; i++)
            {
                var start = matches[i].Index + matches[i].Length;
                var end = i + 1 < matches.Count ? matches[i + 1].Index : indexedText.Length;
                pages.Add(new DocumentPage(int.Parse(matches[i].Groups[1].Value), indexedText[start..end].Trim()));
            }
        }
        return new IndexedDocument(fileName, pages, sha256);
    }

    /// <summary>Whitespace-egységesítés és tipográfiai idézőjelek/kötőjelek normalizálása.</summary>
    internal static string Normalize(string? input)
    {
        if (string.IsNullOrWhiteSpace(input)) return string.Empty;
        var s = input
            .Replace('„', '"').Replace('”', '"').Replace('“', '"')
            .Replace('–', '-').Replace('—', '-')
            .Replace('\u00A0', ' ');
        return WhitespaceRegex().Replace(s, " ").Trim().Trim('"', '\'', '.', ' ');
    }

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespaceRegex();

    [GeneratedRegex(@"\[OLDAL (\d+)\]")]
    private static partial Regex PageTagRegex();
}

/// <summary>Dokumentum-szövegkinyerő szolgáltatás interfésze.</summary>
public interface IDocumentTextExtractor
{
    /// <summary>Kinyeri a szöveget oldalanként, és kiszámítja a SHA-256 lenyomatot.</summary>
    /// <param name="fileName">Fájlnév (a kiterjesztés határozza meg a formátumot).</param>
    /// <param name="content">A fájl tartalma.</param>
    IndexedDocument Extract(string fileName, byte[] content);
}

/// <summary>
/// PDF (PdfPig, oldalanként), DOCX (OpenXml, címsorok / oldaltörések szerint) és TXT/MD
/// formátumú dokumentumok szövegkinyerője. Teljesen lokálisan fut – nincs külső hívás.
/// </summary>
public sealed class DocumentTextExtractor : IDocumentTextExtractor
{
    /// <summary>A támogatott kiterjesztések.</summary>
    public static readonly string[] SupportedExtensions = [".pdf", ".docx", ".txt", ".md"];

    /// <inheritdoc />
    public IndexedDocument Extract(string fileName, byte[] content)
    {
        ArgumentNullException.ThrowIfNull(content);
        var ext = Path.GetExtension(fileName).ToLowerInvariant();
        var pages = ext switch
        {
            ".pdf" => ExtractPdf(content),
            ".docx" => ExtractDocx(content),
            ".txt" or ".md" => ExtractPlainText(content),
            _ => throw new NotSupportedException($"Nem támogatott fájlformátum: {ext}. Támogatott: {string.Join(", ", SupportedExtensions)}")
        };
        return new IndexedDocument(fileName, pages, ComputeSha256(content));
    }

    /// <summary>SHA-256 lenyomat számítása (kisbetűs hex).</summary>
    /// <param name="content">Bájtsorozat.</param>
    public static string ComputeSha256(byte[] content) =>
        Convert.ToHexString(SHA256.HashData(content)).ToLowerInvariant();

    private static List<DocumentPage> ExtractPdf(byte[] content)
    {
        using var pdf = PdfDocument.Open(content);
        return pdf.GetPages()
            .Select(p => new DocumentPage(p.Number, ContentOrderTextExtractor.GetText(p)))
            .ToList();
    }

    /// <summary>
    /// A DOCX-nek nincs fix oldala; "oldalnak" a kézi oldaltörések közötti részt tekintjük.
    /// Ha nincs oldaltörés, kb. 3000 karakteres szakaszokra bontunk.
    /// </summary>
    private static List<DocumentPage> ExtractDocx(byte[] content)
    {
        using var ms = new MemoryStream(content);
        using var doc = WordprocessingDocument.Open(ms, false);
        var body = doc.MainDocumentPart?.Document?.Body;
        if (body is null) return [new DocumentPage(1, string.Empty)];

        var pages = new List<DocumentPage>();
        var current = new StringBuilder();
        foreach (var para in body.Descendants<Paragraph>())
        {
            var hasPageBreak = para.Descendants<Break>().Any(b => b.Type?.Value == BreakValues.Page);
            current.AppendLine(para.InnerText);
            if (hasPageBreak || current.Length > 3000)
            {
                pages.Add(new DocumentPage(pages.Count + 1, current.ToString()));
                current.Clear();
            }
        }
        if (current.Length > 0 || pages.Count == 0) pages.Add(new DocumentPage(pages.Count + 1, current.ToString()));
        return pages;
    }

    /// <summary>Szöveges fájl: a form feed (\f) karakter oldaltörésnek számít; egyébként 3000 karakteres szakaszok.</summary>
    private static List<DocumentPage> ExtractPlainText(byte[] content)
    {
        var text = Encoding.UTF8.GetString(content).TrimStart('\uFEFF');
        var chunks = text.Contains('\f') ? text.Split('\f') : ChunkByParagraph(text, 3000);
        return chunks.Select((t, i) => new DocumentPage(i + 1, t)).ToList();
    }

    private static string[] ChunkByParagraph(string text, int maxChars)
    {
        var result = new List<string>();
        var sb = new StringBuilder();
        foreach (var line in text.Split('\n'))
        {
            if (sb.Length + line.Length > maxChars && sb.Length > 0)
            {
                result.Add(sb.ToString());
                sb.Clear();
            }
            sb.Append(line).Append('\n');
        }
        if (sb.Length > 0 || result.Count == 0) result.Add(sb.ToString());
        return [.. result];
    }
}

