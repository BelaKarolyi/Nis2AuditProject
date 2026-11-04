using System.Text.RegularExpressions;
using Nis2AuditGuard.Core.Domain;

namespace Nis2AuditGuard.Core.Services;

/// <summary>A hallucináció-őr és forrás-validátor szolgáltatás interfésze.</summary>
public interface IHallucinationValidatorService
{
    /// <summary>
    /// Ellenőrzi az AI-eredményt a forrásdokumentum (eredeti és anonimizált változata)
    /// és az SQL-ből betöltött hiteles NIS2 követelmény alapján.
    /// </summary>
    /// <param name="aiResult">Az AI által visszaadott eredményobjektum.</param>
    /// <param name="originalDoc">Az eredeti, oldalanként indexelt dokumentum.</param>
    /// <param name="anonymizedText">Az anonimizált dokumentumszöveg (ha az idézet tokeneket tartalmazna).</param>
    /// <param name="sqlRequirement">Az SQL tudásbázisból betöltött hiteles követelmény.</param>
    WhiteBoxAiGapResult ValidateAndEnforceSources(
        WhiteBoxAiGapResult aiResult,
        IndexedDocument originalDoc,
        string anonymizedText,
        Nis2KovetelmenyDto sqlRequirement);
}

/// <summary>
/// Determinisztikus „Fehér Dobozos” ellenőrző szolgáltatás az AI hallucinációinak kiszűrésére (ADR-04).
/// 1. A jogszabályi hivatkozást felülírja az SQL tudásbázisból.
/// 2. Kikényszeríti a "TERVEZET – SZAKÉRTŐI FELÜLVIZSGÁLAT SZÜKSÉGES" címkét (Human-in-the-Loop).
/// 3. Normalizált szó szerinti kereséssel (`IndexedDocument.FindExact`) ellenőrzi az idézetet és az oldalszámot.
/// </summary>
public sealed partial class HallucinationValidatorService : IHallucinationValidatorService
{
    /// <summary>A hallucináció-gyanú figyelmeztető előtagja.</summary>
    public const string HallucinationPrefix =
        "⚠️ HALLUCINÁCIÓ-GYANÚ: A megjelölt idézet nem található szó szerint a dokumentumban! Kérem, ellenőrizze manuálisan a megadott oldalszámot! ";

    /// <summary>Az oldalszám-eltérés figyelmeztető előtagja.</summary>
    public const string PageMismatchPrefix =
        "⚠️ OLDALSZÁM-ELTÉRÉS: Az idézet szó szerint megtalálható a dokumentumban, de más oldalon szerepel, mint amit az AI megjelölt! ";

    /// <inheritdoc />
    public WhiteBoxAiGapResult ValidateAndEnforceSources(
        WhiteBoxAiGapResult aiResult,
        IndexedDocument originalDoc,
        string anonymizedText,
        Nis2KovetelmenyDto sqlRequirement)
    {
        ArgumentNullException.ThrowIfNull(aiResult);
        ArgumentNullException.ThrowIfNull(originalDoc);
        ArgumentNullException.ThrowIfNull(sqlRequirement);

        // 1. Determinisztikus jogszabályhely-kényszerítés az SQL tudásbázisból (0% §-hallucináció).
        aiResult.MelyKovetelmenyhezKapcsolodik =
            $"{sqlRequirement.KovetelmenyKod} – {sqlRequirement.JogszabalyHivatkozas}";

        // 2. Kötelező Human-in-the-Loop tervezet státusz.
        aiResult.StatuszCimke = SzakertoiStatuszok.Tervezet;

        // Megbízhatóság tartományba szorítása (0..100).
        aiResult.MennyireBiztosSzazalek = Math.Clamp(aiResult.MennyireBiztosSzazalek, 0, 100);

        // 3. Negatív megállapítás (amikor a szabályzatból teljesen hiányzik az előírás): nincs idézet.
        if (aiResult.IsNegativeFinding ||
            aiResult.MireAlapozzaPontosIdezet.Contains("nem található", StringComparison.OrdinalIgnoreCase))
        {
            aiResult.IdezetValidacioOk = true;
            aiResult.OldalValidacioOk = true;
            return aiResult;
        }

        // 4. Szó szerinti idézet keresése előbb az eredeti, majd az anonimizált dokumentumban.
        var hit = originalDoc.FindExact(aiResult.MireAlapozzaPontosIdezet);
        if (hit is null && !string.IsNullOrWhiteSpace(anonymizedText))
        {
            var anonDoc = IndexedDocument.FromIndexedText(originalDoc.FileName, anonymizedText, originalDoc.Sha256);
            hit = anonDoc.FindExact(aiResult.MireAlapozzaPontosIdezet);
        }

        if (hit is null)
        {
            aiResult.IdezetValidacioOk = false;
            aiResult.OldalValidacioOk = false;
            if (!aiResult.MitKellASzakertonekEllenoriznie.StartsWith("⚠️", StringComparison.Ordinal))
            {
                aiResult.MitKellASzakertonekEllenoriznie =
                    HallucinationPrefix + aiResult.MitKellASzakertonekEllenoriznie;
            }
            return aiResult;
        }

        aiResult.IdezetValidacioOk = true;

        // 5. Oldalszám-validáció: ha az AI oldalszámot írt a forrásmezőbe, egyezik-e a találat oldalával?
        var claimedNumbers = NumberRegex().Matches(aiResult.MelyDokumentumbolOldalSzakasz)
            .Select(m => int.TryParse(m.Value, out var n) ? n : -1)
            .Where(n => n > 0)
            .ToList();

        aiResult.OldalValidacioOk = claimedNumbers.Count == 0 || claimedNumbers.Contains(hit.Page);
        if (!aiResult.OldalValidacioOk)
        {
            aiResult.MitKellASzakertonekEllenoriznie =
                $"{PageMismatchPrefix}(Találat: {hit.Page}. oldal). " + aiResult.MitKellASzakertonekEllenoriznie;
        }

        return aiResult;
    }

    [GeneratedRegex(@"\d+")]
    private static partial Regex NumberRegex();
}
