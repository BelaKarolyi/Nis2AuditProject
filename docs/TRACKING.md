# PROJECT TRACKING & STATUS LOG: NIS2-AuditGuard

* **Utolsó frissítés:** 2026. november 4.
* **Projektvezető:** Károlyi Béla (Neptun-kód: `FULABV`)
* **Aktuális fázis:** `.NET 10` Solution és `Core` réteg felépítése + Dokumentáció szinkronizálása (ADR-08–13) → Backend szolgáltatások és Web-zónák bekötése a nov. 20-i konzultációra.

## 1. Tantárgyi Követelmények Teljesülési Mátrixa (Cél: 100% / Jeles)

| Értékelési kategória (Dr. Bárkányi Pál) | Súly | Státusz | Elkészült produktum a Repository-ban (`docs/`, `src/`, `database/`) |
|---|---|---|---|
| **1. Projekt Alapító Dokumentum (PAD)**<br>Sablon kitöltése (10%) + Folyamatos karbantartás (5%) | **15%** | ✅ KÉSZ (v1.3-ra frissítve, folyamatosan naplózva) | `docs/01_PAD_Projekt_Alapito_Dokumentum_NIS2_AuditGuard_FULABV.docx`, `docs/PAD_NIS2_AuditGuard.docx`, `docs/DECISIONS.md`, `docs/TRACKING.md` |
| **2. Feladat Specifikáció**<br>Megoldási alternatívák (5%), Folyamatábrák/BPMN (5%), Adatbázis és struktúrák (10%) | **20%** | ✅ KÉSZ (v1.3 szinkronizálva a .NET 10 / „B” architektúrával) | `docs/02_Specifikacio_HLD_BPMN_es_WebBiztonsag_NIS2_AuditGuard_FULABV.docx`, `docs/HLD_NIS2_AuditGuard.docx`, `docs/HLD.md`, `database/04_SQL_Adatbazis_Modell_es_Seed_NIS2_AuditGuard.sql` |
| **3. Feladat Megoldás és Prototípus**<br>Program elkészítése (25%), Kód tisztasága (5%), Kommentek (5%), Futtatható verzió hibátlansága (10%) | **45%** | 🔄 FOLYAMATBAN (`.NET 10` Solution, EF Core DbContext, PDF/DOCX extractor és PII-anonimizáló kész, 0 warninggal fordul) | `Nis2AuditGuard.sln`, `src/Nis2AuditGuard.Core/`, `src/Nis2AuditGuard.PublicWeb/`, `src/Nis2AuditGuard.ManagerWeb/`, `tests/` |
| **4. Dokumentációk**<br>Felhasználói (10%), Fejlesztői (10%) + Kötelező AI-modell dokumentáció | **20%** | ✅ ALAPVERZIÓ FRISSÍTVE (Végleges képernyőképekkel és golden-set mérési táblázattal bővítendő) | `docs/03_AI_Modell_es_Felhasznaloi_Dokumentacio_NIS2_AuditGuard_FULABV.docx`, `docs/Dokumentacio_Fejlesztoi_Felhasznaloi_AI_NIS2.docx` |

## 2. Elvégzett Feladatok Naplója (Completed Milestones)
* **[2026.10.08 – 2026.10.15.] Koncepció és 1. Feladat (Ügyféltalálkozó):** Meghatározva az első ügyféltalálkozó kérdéssora, a bekérendő NIS2 dokumentumok (IBSZ, BCP/DRP, AD-listák, mentési jegyzőkönyvek) és az interjúalanyok (Ügyvezető, IBF, IT vezető, HR/Beszerzés).
* **[2026.10.20 – 2026.10.23.] Architektúra, PAD és HLD:** Megtervezve a hibrid Dual-Engine AI architektúra, a 7 táblás MS SQL LocalDB séma, a BPMN folyamatábra és a C# magkód vázlata.
* **[2026.10.26.] Publikus Landing Page prototípus (Zero Public Exposure):** Elkészült a `LandingPage_NIS2_AuditGuard.html` (1. AI-modul: érintettségi előszűrő, dinamikus irányár-kalkulátor, Honeypot + Time-trap botvédelem, GDPR Cookie Banner, WCAG akadálymentesítés, JSON export az offline rendszerhez).
* **[2026.10.28.] Zárt Offline Menedzseri Konzol és 3-2-1 DRP Backup prototípus:** Elkészült a `Manager_Offline_Dashboard.html` (lokális PII-anonimizáló előnézet, 6-pontos fehér dobozos Gap-kártyák, idézet-ellenőrző hallucináció-szűrő, VMI/SZEKI mutatók, 3 szerepkörös vezetői összefoglaló, Word/Excel export, valamint SHA-256 + 3-2-1 titkosított mentés és Visszaállítási Próba Jegyzőkönyv).
* **[2026.11.02.] Session Lezárás és Munkafolyamat-szétválasztás:** Context, Tracking és Decision fájlok rögzítése.
* **[2026.11.03.] Repository strukturálás és .NET 10 Solution felépítése:**
  * Repository rendezése `src/`, `tests/`, `database/`, `docs/` és `archive/` mappákba (`git mv` előzménymegőrzéssel).
  * `Nis2AuditGuard.sln` létrehozása 5 projekttel (`Core`, `PublicWeb`, `ManagerWeb`, `Tests`, `Eval`), `TreatWarningsAsErrors=true` minőségkapuval és javított `System.Security.Cryptography.Pkcs 10.0.14` függőséggel.
  * Elkészült az `AuditGuardDbContext` (7 tábla), a `DocumentTextExtractor` (PDF/DOCX/TXT oldalszám-indexeléssel + SHA-256 hash-sel, amellyel a 61 oldalas 2024. évi LXIX. törvény PDF is feldolgozásra került) és a visszafordítható token-térképes `PiiAnonymizerService`.
* **[2026.11.04.] Architekturális döntések véglegesítése (ADR-08–13) és Dokumentáció-szinkron:**
  * Rögzítve a „B” architektúra (C# Dual-Engine Orchestrator: `gemini-3.8-flash` + közvetlen lokális Ollama + opcionális OpenClaw Discord-értesítő) és az ingyenes Gemini API-kulcsra vonatkozó adatvédelmi szabály (online csak szintetikus demó-adat, valós adat csak Ollamán).
  * A HLD, PAD, Specifikáció és AI/Fejlesztői dokumentumok szinkronizálása (Markdown + `.docx`).

## 3. Következő Lépések és Teendők (Next Actions / Backlog)

### 1–2. hét (nov. 4–16.): Core szolgáltatások és Tudásbázis
1. **Bővített NIS2 Seed (`SeedData.cs`):** 12–15 követelmény és kontroll a 2024. évi LXIX. tv. és a 7/2024. MK rendelet fő családjai (AC, AT, AU, CP, IA, IR, RA, SC, SI, SR, CLD) mentén.
2. **Hallucináció-őr és Előszűrő (`HallucinationValidatorService`, `PreScreeningService`):** Szó szerinti + oldalszám-validáció, méret- és irányár-számítás.
3. **AI-motorok és Orchestrator (`GeminiAiEngine`, `OllamaAiEngine`, `DualEngineAiOrchestrator`):** 5 mp timeout, automatikus failover, „Magas” osztály és „Valós ügyféladat ingyenes kulccsal” lokális kényszerítés, teljes naplózás (`Audit_Es_AI_Naplo`).
4. **Export és DRP Backup (`ExportService`, `BackupService`):** Valódi `.docx` (OpenXml) és `.xlsx` (ClosedXML) generálás, SQL CHECKSUM / SQLite backup + AES-256-GCM titkosítás + Visszaállítási Próba Jegyzőkönyv.

### 3. hét (nov. 17–20.): Web-zónák bekötése és Konzultációs Demó
5. **`Nis2AuditGuard.PublicWeb`:** A `LandingPage_NIS2_AuditGuard.html` bekötése valódi `/api/prescreen` végpontra, RateLimiter, Honeypot, Time-Trap, Anti-CSRF.
6. **`Nis2AuditGuard.ManagerWeb` (`127.0.0.1`):** A `Manager_Offline_Dashboard.html` bekötése a C# API végpontokra (JSON import, PDF/DOCX feltöltés, PII-előnézet, Gap-analízis, HITL jóváhagyás, DOCX/XLSX export, DRP backup).
7. **`xUnit` tesztcsomag:** Unit és `WebApplicationFactory` integrációs tesztek futtatása.

### 4–5. hét (nov. 21 – dec. 10.): OpenClaw értesítő, AI-mérés és Védés
8. **AI-eredményesség mérése (`Nis2AuditGuard.Eval`):** Golden-set mintaszabályzatok kiértékelése, mérési táblázat beemelése a dokumentációba.
9. **Opcionális OpenClaw → Discord értesítő** közös beállítása (csak metaadat).
10. **Védési főpróba** (élő online ↔ offline failover bemutatóval).

### Konzultáció és Végső Védés (Egyetemi órarend szerint)
* **2026. november 20. (13:30 – 16:05, C107):** Konzultáció és előrehaladási bemutató Dr. Bárkányi Pálnál.
* **2026. december 11. (16:20 – 20:20):** Végső projektbemutató és védés (Offline/Failover élő demóval).
