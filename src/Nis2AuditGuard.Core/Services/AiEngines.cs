using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using Nis2AuditGuard.Core.Domain;
using Nis2AuditGuard.Core.Options;

namespace Nis2AuditGuard.Core.Services;

/// <summary>Egy AI-motor nyers válasza a JSON objektummal és a naplózandó nyers JSON karakterlánccal.</summary>
/// <param name="Result">A deszerializált fehér dobozos eredmény.</param>
/// <param name="RawJson">A modell által visszaadott nyers JSON szöveg.</param>
/// <param name="ModelName">A ténylegesen használt modell neve.</param>
public sealed record AiEngineResponse(WhiteBoxAiGapResult Result, string RawJson, string ModelName);

/// <summary>Absztrakció az online (Gemini) és a helyi (Ollama) AI-motorokhoz (tesztelhetőség és DI).</summary>
public interface IAiEngine
{
    /// <summary>A motor egyedi azonosítója ("Gemini" vagy "Ollama").</summary>
    string EngineName { get; }

    /// <summary>Kiértékeli a promptot és strukturált JSON eredményt ad vissza.</summary>
    /// <param name="systemInstruction">Rendszerutasítás.</param>
    /// <param name="userPrompt">Felhasználói prompt az anonimizált dokumentummal és a követelménnyel.</param>
    /// <param name="ct">Megszakítási token.</param>
    Task<AiEngineResponse> GenerateGapAnalysisAsync(string systemInstruction, string userPrompt, CancellationToken ct = default);
}

/// <summary>
/// Google Gemini Interactions API (`/v1beta/interactions`) meghajtó (ADR-09).
/// - Modell: `gemini-3.8-flash` (konfigurálható).
/// - Hitelesítés: `x-goog-api-key` HTTP fejléc (nem URL-paraméter!).
/// - Strukturált kimenet: `response_format` JSON séma kényszerítéssel.
/// </summary>
public sealed class GeminiAiEngine(HttpClient httpClient, IOptions<AiOptions> options) : IAiEngine
{
    internal static readonly JsonSerializerOptions TolerantJsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        NumberHandling = JsonNumberHandling.AllowReadingFromString
    };

    private readonly AiOptions _options = options.Value;

    /// <inheritdoc />
    public string EngineName => "Gemini";

    /// <inheritdoc />
    public async Task<AiEngineResponse> GenerateGapAnalysisAsync(
        string systemInstruction,
        string userPrompt,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(_options.GeminiApiKey))
            throw new InvalidOperationException("Hiányzó Gemini API kulcs (AI:GeminiApiKey).");

        var payload = new
        {
            model = _options.GeminiModel,
            system_instruction = systemInstruction,
            input = userPrompt,
            store = false,
            generation_config = new { seed = _options.Seed, thinking_level = "minimal" },
            response_format = new
            {
                type = "text",
                mime_type = "application/json",
                schema = BuildJsonSchema()
            }
        };

        using var req = new HttpRequestMessage(HttpMethod.Post, _options.GeminiEndpoint)
        {
            Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json")
        };
        req.Headers.Add("x-goog-api-key", _options.GeminiApiKey);
        req.Headers.Add("Api-Revision", _options.GeminiApiRevision);

        using var resp = await httpClient.SendAsync(req, ct);
        resp.EnsureSuccessStatusCode();

        var body = await resp.Content.ReadAsStringAsync(ct);
        var innerJson = ExtractInteractionText(body);
        var result = JsonSerializer.Deserialize<WhiteBoxAiGapResult>(innerJson, TolerantJsonOptions) ?? new WhiteBoxAiGapResult();
        return new AiEngineResponse(result, innerJson, _options.GeminiModel);
    }

    /// <summary>Kinyeri a modell szöveges JSON kimenetét az Interactions API válaszból.</summary>
    internal static string ExtractInteractionText(string responseBody)
    {
        using var doc = JsonDocument.Parse(responseBody);
        var root = doc.RootElement;

        if (root.TryGetProperty("output_text", out var outText) && outText.ValueKind == JsonValueKind.String)
            return outText.GetString() ?? "{}";

        if (root.TryGetProperty("steps", out var steps) && steps.ValueKind == JsonValueKind.Array)
        {
            for (var i = steps.GetArrayLength() - 1; i >= 0; i--)
            {
                var step = steps[i];
                if (step.TryGetProperty("type", out var st) && st.GetString() == "model_output" &&
                    step.TryGetProperty("content", out var content) && content.ValueKind == JsonValueKind.Array)
                {
                    foreach (var part in content.EnumerateArray())
                    {
                        if (part.TryGetProperty("text", out var t) && t.ValueKind == JsonValueKind.String)
                            return t.GetString() ?? "{}";
                    }
                }
            }
        }

        // Visszafelé kompatibilitás (ha teszt/mock a klasszikus candidates szerkezetet adja vissza).
        if (root.TryGetProperty("candidates", out var cands) && cands.GetArrayLength() > 0)
        {
            return cands[0].GetProperty("content").GetProperty("parts")[0].GetProperty("text").GetString() ?? "{}";
        }

        return "{}";
    }

    internal static object BuildJsonSchema() => new
    {
        type = "object",
        properties = new
        {
            mit_allit_az_ai = new { type = "string" },
            mire_alapozza_pontos_idezet = new { type = "string" },
            mely_dokumentumbol_oldal_szakasz = new { type = "string" },
            mely_kovetelmenyhez_kapcsolodik = new { type = "string" },
            mennyire_biztos_szazalek = new { type = "integer" },
            mit_kell_a_szakertonek_ellenoriznie = new { type = "string" },
            jogszabaly_altal_eloirt_hiany = new { type = "string" },
            ai_altal_javasolt_jo_gyakorlat = new { type = "string" },
            generalt_audit_kerdesek = new { type = "array", items = new { type = "string" } }
        },
        required = new[]
        {
            "mit_allit_az_ai",
            "mire_alapozza_pontos_idezet",
            "mely_dokumentumbol_oldal_szakasz",
            "mely_kovetelmenyhez_kapcsolodik",
            "mennyire_biztos_szazalek",
            "mit_kell_a_szakertonek_ellenoriznie",
            "jogszabaly_altal_eloirt_hiany",
            "ai_altal_javasolt_jo_gyakorlat",
            "generalt_audit_kerdesek"
        }
    };
}

/// <summary>
/// Lokális Ollama (`/api/generate`) meghajtó az offline, áramszünet-tűrő (BCP) és
/// „Magas” biztonsági osztályú EIR elemzésekhez (ADR-02, ADR-10).
/// </summary>
public sealed class OllamaAiEngine(HttpClient httpClient, IOptions<AiOptions> options) : IAiEngine
{
    private readonly AiOptions _options = options.Value;

    /// <inheritdoc />
    public string EngineName => "Ollama";

    /// <inheritdoc />
    public async Task<AiEngineResponse> GenerateGapAnalysisAsync(
        string systemInstruction,
        string userPrompt,
        CancellationToken ct = default)
    {
        var payload = new
        {
            model = _options.OllamaModel,
            system = systemInstruction,
            prompt = userPrompt,
            format = GeminiAiEngine.BuildJsonSchema(),
            stream = false,
            options = new { temperature = 0.1, seed = _options.Seed, num_ctx = 4096, num_predict = 450 }
        };

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(TimeSpan.FromSeconds(_options.LocalTimeoutSeconds));

        using var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
        using var resp = await httpClient.PostAsync(_options.OllamaEndpoint, content, cts.Token);
        resp.EnsureSuccessStatusCode();

        var body = await resp.Content.ReadAsStringAsync(cts.Token);
        using var doc = JsonDocument.Parse(body);
        var innerJson = doc.RootElement.TryGetProperty("response", out var r)
            ? r.GetString() ?? "{}"
            : "{}";

        var result = JsonSerializer.Deserialize<WhiteBoxAiGapResult>(innerJson, GeminiAiEngine.TolerantJsonOptions) ?? new WhiteBoxAiGapResult();
        return new AiEngineResponse(result, innerJson, _options.OllamaModel);
    }
}

