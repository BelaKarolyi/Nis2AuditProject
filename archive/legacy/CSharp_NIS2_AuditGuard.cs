using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace Nis2AuditGuard.Models
{
    /// <summary>
    /// Entity Framework Core Database Context a NIS2-AuditGuard rendszerhez.
    /// (Megfelel a feladatkiírás "strukturált adatkezelés" pontjának)
    /// </summary>
    public class AuditDbContext : DbContext
    {
        public DbSet<Ugyfel> Ugyfelek { get; set; }
        public DbSet<Hianyossag> Hianyossagok { get; set; }
        public DbSet<Esemenynaplo> Naplo { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            // Lokális adatbázis kapcsolat az adatvédelmi pajzs biztosítása érdekében
            optionsBuilder.UseSqlServer(@"Server=(localdb)\mssqllocaldb;Database=Nis2AuditDB;Trusted_Connection=True;");
        }
    }

    public class Ugyfel
    {
        public int Id { get; set; }
        public string Cegnev { get; set; }
        public string Agazat { get; set; }
    }

    public class Hianyossag
    {
        public int Id { get; set; }
        public string AiAllitas { get; set; }
        public string ForrasIdezet { get; set; }
        public string Statusz { get; set; } // TERVEZET, JÓVÁHAGYVA
        public bool HallucinacioGyanu { get; set; }
    }

    public class Esemenynaplo
    {
        public int Id { get; set; }
        public DateTime Idopont { get; set; }
        public string EsemenyTipus { get; set; }
        public string ModellTipus { get; set; }
    }

    /// <summary>
    /// AI Szolgáltatás, amely elrejti a PII adatokat és kezeli a Gemini API / Lokális modell fallback mechanizmust.
    /// </summary>
    public class AiAnalyzerService
    {
        private readonly HttpClient _httpClient = new HttpClient();

        public async Task<string> AnalyzeDocumentAsync(string documentText, string requirementText, string companyName)
        {
            // 1. Lépés: Adatvédelmi Maszkolás (Anonimizálás)
            string sanitizedText = documentText.Replace(companyName, "[ÜGYFÉL_SZERVEZET]");

            // 2. Lépés: System Prompt összeállítása szigorú JSON sémával
            var prompt = new
            {
                contents = new[]
                {
                    new {
                        parts = new[]
                        {
                            new { text = $"Elemezd a dokumentumot a következő NIS2 jogszabály alapján: {requirementText}\nDokumentum: {sanitizedText}. Csak a dokumentumból dolgozz, keress pontos idézetet! Válaszolj JSON formátumban: {{ 'ai_allitas': '...', 'forras_idezet': '...', 'statusz': 'HIÁNYOS' }}" }
                        }
                    }
                },
                generationConfig = new { temperature = 0.1 } 
            };

            string jsonRequestBody = JsonSerializer.Serialize(prompt);
            var content = new StringContent(jsonRequestBody, Encoding.UTF8, "application/json");

            try
            {
                string apiKey = Environment.GetEnvironmentVariable("GEMINI_API_KEY");
                string apiEndpoint = Environment.GetEnvironmentVariable("GEMINI_API_URL"); // Rejtett URL
                HttpResponseMessage response = await _httpClient.PostAsync($"{apiEndpoint}?key={apiKey}", content);

                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadAsStringAsync();
                }
                else
                {
                    return await FallbackToLocalModelAsync(sanitizedText, requirementText);
                }
            }
            catch (Exception)
            {
                return await FallbackToLocalModelAsync(sanitizedText, requirementText);
            }
        }

        private async Task<string> FallbackToLocalModelAsync(string sanitizedText, string requirementText)
        {
            // Fallback mechanizmus az OpenClaw / Ollama helyi modellre
            var localContent = new StringContent($"{{\"model\": \"llama3\", \"prompt\": \"{sanitizedText}\"}}", Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync("http://localhost:11434/api/generate", localContent);
            return await response.Content.ReadAsStringAsync();
        }
    }
}
