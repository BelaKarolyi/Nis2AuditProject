using System.Text.RegularExpressions;

namespace Nis2AuditGuard.Core.Services;

/// <summary>Az anonimizálás eredménye.</summary>
/// <param name="Text">A maszkolt, külső AI-nak továbbítható szöveg.</param>
/// <param name="Replacements">Kategóriánkénti cserék száma (naplózáshoz és az előnézethez).</param>
/// <param name="TokenMap">Token → eredeti érték térkép (CSAK lokálisan, a visszaállításhoz; soha nem hagyja el a szervert).</param>
public sealed record AnonymizationResult(
    string Text,
    IReadOnlyDictionary<string, int> Replacements,
    IReadOnlyDictionary<string, string> TokenMap)
{
    /// <summary>Az összes csere száma.</summary>
    public int TotalReplacements => Replacements.Values.Sum();

    /// <summary>Visszaállítja az eredeti értékeket egy (pl. AI által generált) szövegben.</summary>
    /// <param name="text">Tokeneket tartalmazó szöveg.</param>
    public string Restore(string text)
    {
        if (string.IsNullOrEmpty(text)) return text;
        foreach (var (token, original) in TokenMap.OrderByDescending(kv => kv.Key.Length))
        {
            text = text.Replace(token, original, StringComparison.Ordinal);
        }
        return text;
    }
}

/// <summary>PII-anonimizáló szolgáltatás interfésze.</summary>
public interface IPiiAnonymizerService
{
    /// <summary>Maszkolja a személyes és szervezeti azonosítókat.</summary>
    /// <param name="rawText">Eredeti szöveg.</param>
    /// <param name="ugyfelCegNev">Az ügyfél cégneve.</param>
    /// <param name="extraTerms">További maszkolandó kifejezések (pl. ismert személynevek, hostnevek).</param>
    AnonymizationResult Anonymize(string rawText, string? ugyfelCegNev, IEnumerable<string>? extraTerms = null);
}

/// <summary>
/// Lokális adatvédelmi pajzs (Privacy-by-Design, 2024. évi LXIX. tv. 9. §; ADR-03).
/// Minden külső AI-hívás ELŐTT lefut, és sorszámozott tokenekre cseréli a cégnevet, ismert neveket,
/// e-mail címeket, IP-címeket, telefonszámokat, adószámot, cégjegyzékszámot, IBAN-t és belső hostneveket.
/// A sorszámozás (pl. [ANONIM_EMAIL_2]) megőrzi a szöveg logikai szerkezetét az AI számára.
/// </summary>
public sealed partial class PiiAnonymizerService : IPiiAnonymizerService
{
    /// <summary>A cégnév helyére kerülő token.</summary>
    public const string CompanyToken = "[ÜGYFÉL_SZERVEZET]";

    /// <inheritdoc />
    public AnonymizationResult Anonymize(string rawText, string? ugyfelCegNev, IEnumerable<string>? extraTerms = null)
    {
        var counts = new Dictionary<string, int>();
        var map = new Dictionary<string, string>();
        if (string.IsNullOrWhiteSpace(rawText)) return new AnonymizationResult(string.Empty, counts, map);

        var text = rawText;

        // 1. Cégnév (és a jogi forma nélküli változata, pl. "Minta Kft." → "Minta").
        if (!string.IsNullOrWhiteSpace(ugyfelCegNev))
        {
            foreach (var variant in CompanyVariants(ugyfelCegNev))
            {
                text = ReplaceLiteral(text, variant, _ => CompanyToken, "CEGNEV", counts, map);
            }
        }

        // 2. Kifejezett listán megadott kifejezések (személynevek, rendszernevek).
        var termIndex = 0;
        foreach (var term in (extraTerms ?? []).Where(t => t.Trim().Length >= 3).Distinct())
        {
            var token = $"[ANONIM_SZEMELY_{++termIndex}]";
            text = ReplaceLiteral(text, term.Trim(), _ => token, "SZEMELY", counts, map);
        }

        // 3. Mintaalapú azonosítók – a sorrend számít (az IBAN/adószám előbb, mint a telefon).
        text = ReplacePattern(text, EmailRegex(), "ANONIM_EMAIL", "EMAIL", counts, map);
        text = ReplacePattern(text, IbanRegex(), "ANONIM_BANKSZAMLA", "BANKSZAMLA", counts, map);
        text = ReplacePattern(text, HuAccountRegex(), "ANONIM_BANKSZAMLA", "BANKSZAMLA", counts, map);
        text = ReplacePattern(text, TaxNumberRegex(), "ANONIM_ADOSZAM", "ADOSZAM", counts, map);
        text = ReplacePattern(text, CompanyRegNumberRegex(), "ANONIM_CEGJEGYZEKSZAM", "CEGJEGYZEKSZAM", counts, map);
        text = ReplacePattern(text, IPv4Regex(), "BELSŐ_IP_CÍM", "IP", counts, map);
        text = ReplacePattern(text, IPv6Regex(), "BELSŐ_IP_CÍM", "IP", counts, map);
        text = ReplacePattern(text, HuPhoneRegex(), "ANONIM_TELEFON", "TELEFON", counts, map);
        text = ReplacePattern(text, InternalHostRegex(), "BELSŐ_HOSZTNÉV", "HOSZTNEV", counts, map);

        return new AnonymizationResult(text, counts, map);
    }

    private static IEnumerable<string> CompanyVariants(string name)
    {
        var trimmed = name.Trim();
        yield return trimmed;
        var withoutForm = LegalFormRegex().Replace(trimmed, string.Empty).Trim().TrimEnd(',', '.');
        if (withoutForm.Length >= 3 && !withoutForm.Equals(trimmed, StringComparison.OrdinalIgnoreCase))
            yield return withoutForm;
    }

    private static string ReplaceLiteral(string text, string literal, Func<int, string> tokenFactory, string category,
        Dictionary<string, int> counts, Dictionary<string, string> map)
    {
        var regex = new Regex(Regex.Escape(literal), RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, TimeSpan.FromSeconds(1));
        return regex.Replace(text, m =>
        {
            Increment(counts, category);
            var token = tokenFactory(counts[category]);
            map.TryAdd(token, m.Value);
            return token;
        });
    }

    private static string ReplacePattern(string text, Regex regex, string tokenPrefix, string category,
        Dictionary<string, int> counts, Dictionary<string, string> map)
    {
        // Ugyanaz az érték mindig ugyanazt a tokent kapja (konzisztens hivatkozás az AI számára).
        var seen = map.Where(kv => kv.Key.StartsWith("[" + tokenPrefix, StringComparison.Ordinal))
                      .ToDictionary(kv => kv.Value, kv => kv.Key, StringComparer.OrdinalIgnoreCase);
        return regex.Replace(text, m =>
        {
            Increment(counts, category);
            if (!seen.TryGetValue(m.Value, out var token))
            {
                token = $"[{tokenPrefix}_{seen.Count + 1}]";
                seen[m.Value] = token;
                map[token] = m.Value;
            }
            return token;
        });
    }

    private static void Increment(Dictionary<string, int> counts, string key) =>
        counts[key] = counts.TryGetValue(key, out var c) ? c + 1 : 1;

    [GeneratedRegex(@"\s*(Korlátolt Felelősségű Társaság|Zártkörűen Működő Részvénytársaság|Kft\.?|Zrt\.?|Nyrt\.?|Bt\.?|Kkt\.?)$", RegexOptions.IgnoreCase)]
    private static partial Regex LegalFormRegex();

    [GeneratedRegex(@"[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}")]
    private static partial Regex EmailRegex();

    [GeneratedRegex(@"\bHU\d{2}(?:[ ]?\d{4}){6}\b", RegexOptions.IgnoreCase)]
    private static partial Regex IbanRegex();

    [GeneratedRegex(@"\b\d{8}-\d{8}(?:-\d{8})?\b")]
    private static partial Regex HuAccountRegex();

    [GeneratedRegex(@"\b\d{8}-\d-\d{2}\b")]
    private static partial Regex TaxNumberRegex();

    [GeneratedRegex(@"\b\d{2}-\d{2}-\d{6}\b")]
    private static partial Regex CompanyRegNumberRegex();

    [GeneratedRegex(@"\b(?:(?:25[0-5]|2[0-4]\d|1?\d?\d)\.){3}(?:25[0-5]|2[0-4]\d|1?\d?\d)(?:/\d{1,2})?\b")]
    private static partial Regex IPv4Regex();

    [GeneratedRegex(@"\b(?:[0-9a-fA-F]{1,4}:){3,7}[0-9a-fA-F]{1,4}\b")]
    private static partial Regex IPv6Regex();

    [GeneratedRegex(@"(?:\+36|\b06)[\s-]?\(?\d{1,2}\)?[\s-]?\d{3}[\s-]?\d{3,4}\b")]
    private static partial Regex HuPhoneRegex();

    [GeneratedRegex(@"\b[a-zA-Z0-9-]+\.(?:local|lan|corp|internal|intra)\b", RegexOptions.IgnoreCase)]
    private static partial Regex InternalHostRegex();
}

