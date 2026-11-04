namespace Nis2AuditGuard.Core.Options;

/// <summary>
/// AI-motorok konfigurációja ("AI" szekció az appsettings.json-ban).
/// Az API-kulcs SOHA nem kerül a repóba: User Secrets vagy környezeti változó (AI__GeminiApiKey).
/// </summary>
public sealed class AiOptions
{
    /// <summary>A konfigurációs szekció neve.</summary>
    public const string SectionName = "AI";

    /// <summary>Gemini Interactions API végpont.</summary>
    public string GeminiEndpoint { get; set; } = "https://generativelanguage.googleapis.com/v1beta/interactions";

    /// <summary>Gemini API kulcs (csak titkos tárolóból!).</summary>
    public string? GeminiApiKey { get; set; }

    /// <summary>Gemini modell neve (ADR-09: a 2.5/1.5 generáció elavult).</summary>
    public string GeminiModel { get; set; } = "gemini-3.8-flash";

    /// <summary>Az Interactions API revíziója.</summary>
    public string GeminiApiRevision { get; set; } = "2026-05-20";

    /// <summary>
    /// ADR-09 adatvédelmi szabály: fizetős (Paid Tier) Gemini előfizetés használatban van-e.
    /// Ha hamis (alapértelmezett, ingyenes kulcs), akkor KIZÁRÓLAG szintetikus demó-dokumentumok
    /// küldhetők a felhőbe; valós ügyféldokumentumok automatikusan a helyi Ollama motorra irányulnak.
    /// </summary>
    public bool UsePaidGeminiTier { get; set; }

    /// <summary>Rögzített dekódolási seed a reprodukálhatósághoz.</summary>
    public int Seed { get; set; } = 42;

    /// <summary>Elsődleges motor időkorlátja másodpercben (BCP: utána automatikus fallback).</summary>
    public int PrimaryTimeoutSeconds { get; set; } = 5;

    /// <summary>Helyi Ollama végpont.</summary>
    public string OllamaEndpoint { get; set; } = "http://localhost:11434/api/generate";

    /// <summary>Helyi (kvantált) modell neve.</summary>
    public string OllamaModel { get; set; } = "qwen2.5:7b-instruct-q4_K_M";

    /// <summary>Helyi modell időkorlátja másodpercben (CPU-n lassabb lehet).</summary>
    public int LocalTimeoutSeconds { get; set; } = 180;

    /// <summary>
    /// Ha sem a Gemini, sem a lokális Ollama folyamat nem érhető el (pl. még nincs telepítve az Ollama),
    /// engedélyezi-e a beépített, szabályalapú C# offline vész-elemzőt, hogy a bemutató soha ne omoljon össze.
    /// </summary>
    public bool EnableBuiltInEmergencyFallback { get; set; } = true;

    /// <summary>Opcionális OpenClaw Gateway / Discord webhook végpont (kizárólag anonim metaadat-értesítésre).</summary>
    public string? OpenClawNotificationEndpoint { get; set; }

    /// <summary>Az elemzésbe küldött dokumentumszöveg maximális hossza (karakter).</summary>
    public int MaxDocumentChars { get; set; } = 60_000;
}
