# PROJECT CONTEXT: NIS2-AuditGuard

* **Utolsó frissítés:** 2026. november 4.
* **Projektvezető / Fejlesztő:** Károlyi Béla (Neptun-kód: `FULABV`) – Egyszemélyes projekt-team
* **Intézmény és Kurzus:** Milton Friedman Egyetem – *Szoftverfejlesztési projekt* (Oktató: Dr. Bárkányi Pál PhD docens)
* **Végső beadási és prezentációs határidő:** 2026. december 11. (Konzultáció: 2026. november 20.)

## 1. Projekt Küldetés és Alapkoncepció
A **NIS2-AuditGuard** egy mesterséges intelligenciával támogatott, kétzónás (publikus előszűrő + fizikailag/hálózatilag izolált belső auditori munkaállomás) webalkalmazás, amely a magyarországi NIS2 kiberbiztonsági megfelelésre (*2024. évi LXIX. törvény*, *7/2024. (VI. 24.) MK rendelet*, *1/2025. SZTFH rendelet*) történő felkészülést és az audit előkészítését automatizálja.

* **Fő oktatási elv:** A rendszer nem helyettesíti az auditort, és nem „fekete dobozként” működik. Minden AI-megállapításhoz kötelezően megjeleníti a **6 átláthatósági elemet**:
  1. Mit állít az AI?
  2. Mire alapozza – pontos, szó szerinti idézet?
  3. Mely dokumentumból / oldalról / szakaszból származik?
  4. Mely jogszabályi követelményhez kapcsolódik?
  5. Mennyire biztos az eredmény (%)?
  6. Mit kell a szakértőnek ellenőriznie?
  
  Minden eredmény alapértelmezetten `TERVEZET – SZAKÉRTŐI FELÜLVIZSGÁLAT SZÜKSÉGES` státuszt kap (*Human-in-the-Loop*).

* **Választott AI-funkciók:**
  1. NIS2 érintettségi előszűrés (dinamikus kérdező + determinisztikus irányár-kalkulátor a publikus Landing Page-en).
  2. NIS2 követelmény–kontroll megfeleltetés (kötelező jogszabályi előírás vs. AI jó gyakorlat éles szétválasztása).
  3. Dokumentumelemző AI, dokumentumok közötti eltéréskeresés és hiányosságfelismerés (*Gap Analysis*).
  4. Auditkérdés-generálás és 3-szintű (Ügyvezető / IT vezető / Auditor) vezetői összefoglaló exportálása (Word `.docx` / Excel `.xlsx`).

## 2. Technológiai Stack és Eszközkörnyezet (2026.11.04. állapot)
* **Kódfejlesztési környezet (IDE / Agent):** Google Antigravity + Visual Studio (a C# .NET 10 backend, az SQL integráció, a tesztek és a frontend-backend összekötés iteratív fejlesztéséhez).
* **UI/UX Design környezet:** Bootstrap 5 + Stitch (a publikus Landing Page és a zárt Menedzseri/Auditori Dashboard vizuális finomhangolásához).
* **Architektúra és Dokumentáció:** GitHub-on verziókövetett Markdown (`docs/*.md`) és szinkronizált Word (`.docx`) dokumentumok.
* **Backend & Adatbázis:** C# ASP.NET Core (`.NET 10.0 LTS`), Entity Framework Core, helyi **MS SQL Server LocalDB** (fejlesztői/teszt fallbackként SQLite támogatással – 100% belföldi/lokális adattárolás).
* **AI Orchestration („B” architektúra – C# Dual-Engine Smart Failover, ADR-09 & ADR-10):**
  * **Primary Engine (Online – Szintetikus/Demó vagy Paid Tier):** Google AI Studio (`gemini-3.8-flash`, konfigurálhatóan `gemini-4-argon`) a `/v1beta/interactions` végponton, kényszerített JSON sémával (`response_format`), előtte lokális C# `PiiAnonymizerService` maszkoló pajzzsal. Ingyenes API-kulcs esetén csak szintetikus demó-dokumentumok küldhetők online.
  * **Fallback / Offline Engine (Valós ügyféladat & Magas EIR):** Lokális **Ollama** (`qwen2.5:7b-instruct-q4_K_M` vagy ekvivalens kvantált helyi LLM) áramszünet, internetkiesés (5 mp timeout), ingyenes kulcs melletti valós ügyféladat vagy „Magas” biztonsági osztályú EIR auditja esetére.
  * **Opcionális értesítési csatorna:** **OpenClaw Gateway → Discord** (kizárólag anonim metaadat: megállapítások száma, státusz; ügyféladat és idézet soha nem hagyja el a gépet).

## 3. Kiberbiztonsági, Adatvédelmi és Mentési Keretrendszer
* **Zero Public Exposure (ADR-05 & ADR-12):** Két különálló webprojekt. A `Nis2AuditGuard.PublicWeb` nem tartalmaz admin végpontot; a `Nis2AuditGuard.ManagerWeb` kizárólag `127.0.0.1` (Loopback) címen figyel, és middleware-rel is tilt minden nem-loopback kérést.
* **Webes védelem a publikus oldalon:** IP-alapú Rate Limiting (DDoS ellen, HTTP 429), láthatatlan Honeypot mező és 3 másodperces időcsapda (*Time-Trap* spam botok ellen), Anti-CSRF tokenek, biztonsági HTTP fejlécek (CSP, X-Frame-Options), GDPR + AI adatkezelési nyilatkozat.
* **3-2-1-1-0 Backup & Katasztrófa-elhárítás (DRP – ADR-06):** SQL `BACKUP WITH CHECKSUM` + SHA-256 sértetlenségi hash, BitLockerrel védett Air-Gap külső meghajtó (MKIK-63), kliensoldali Zero-Knowledge AES-256-GCM titkosítás (`System.Security.Cryptography.AesGcm`), valamint gombnyomásra futtatható Visszaállítási Próba Jegyzőkönyv generátor.
* **Kódminőségi és AI-biztonsági fókuszpontok:**
  * SQL Injection elleni védelem (paraméterezett EF Core LINQ lekérdezések).
  * API-kulcsok szigorú kizárása Gitből (`.gitignore` + User Secrets / Environment Variables + `x-goog-api-key` HTTP header).
  * Groundedness Detection (`HallucinationValidatorService`: normalizált szó szerinti idézet- és oldalszám-ellenőrzés).
  * Figyelmeztetésmentes fordítás (`TreatWarningsAsErrors=true`) és automatizált `xUnit` + `Nis2AuditGuard.Eval` tesztcsomag (ADR-11).

## 4. A Repository Mappaszerkezete és Fő Dokumentumai
* `Nis2AuditGuard.sln` – .NET 10 Solution:
  * `src/Nis2AuditGuard.Core/` – Domain entitások, `AuditGuardDbContext`, `SeedData`, `PiiAnonymizerService`, `DocumentTextExtractor`, `HallucinationValidatorService`, `DualEngineAiOrchestrator`, `ExportService`, `BackupService`.
  * `src/Nis2AuditGuard.PublicWeb/` – Publikus előszűrő, irányár-kalkulátor és JSON ügyfélcsomag-exportáló.
  * `src/Nis2AuditGuard.ManagerWeb/` – Zárt (`127.0.0.1`) auditori munkaállomás és DRP backup konzol.
  * `tests/Nis2AuditGuard.Tests/` – `xUnit` unit és integrációs tesztek.
  * `tests/Nis2AuditGuard.Eval/` – AI-eredményességmérő (golden-set) és PDF/DOCX szövegkinyerő segédeszköz.
* `database/04_SQL_Adatbazis_Modell_es_Seed_NIS2_AuditGuard.sql` – Relációs T-SQL adatbázis modell és NIS2 törvényi seed adatok.
* `docs/` – Beadandó dokumentumok és élő projekt-nyilvántartás:
  * `DECISIONS.md`, `CONTEXT.md`, `TRACKING.md`, `HLD.md` (verziókövetett Markdown források).
  * `01_PAD_Projekt_Alapito_Dokumentum_NIS2_AuditGuard_FULABV.docx` / `PAD_NIS2_AuditGuard.docx` – Projekt Alapító Dokumentum (15%).
  * `02_Specifikacio_HLD_BPMN_es_WebBiztonsag_NIS2_AuditGuard_FULABV.docx` / `HLD_NIS2_AuditGuard.docx` – Specifikáció, Alternatívák, HLD, BPMN 2.0 (20%).
  * `03_AI_Modell_es_Felhasznaloi_Dokumentacio_NIS2_AuditGuard_FULABV.docx` / `Dokumentacio_Fejlesztoi_Felhasznaloi_AI_NIS2.docx` – Fejlesztői, Felhasználói és AI-modell dokumentáció (20%).
* `archive/` – Korábbi statikus HTML prototípusok (`archive/prototypes/`) és kezdeti egyfájlos kódvázlatok (`archive/legacy/`).

