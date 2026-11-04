# 🛡️ NIS2-AuditGuard
**Mesterséges intelligenciával támogatott, hibrid (Dual-Engine) NIS2 tanácsadói és audit-előkészítő rendszer**

* **Szerző / Projektvezető:** Károlyi Béla (Neptun-kód: `FULABV`)
* **Intézmény:** Milton Friedman Egyetem – *Szoftverfejlesztési projekt*
* **Oktató:** Dr. Bárkányi Pál PhD docens
* **Időszak:** 2026/2027. őszi félév (Konzultáció: 2026.11.20. | Védés: 2026.12.11.)

---

## 🎯 1. A projekt célja és koncepciója
A **NIS2-AuditGuard** egy kétzónás (publikus előszűrő + fizikailag/hálózatilag leválasztott belső tanácsadói/auditori munkafelület) `.NET 10` webalkalmazás, amely a magyarországi NIS2 kiberbiztonsági megfelelésre (*2024. évi LXIX. törvény*, *7/2024. (VI. 24.) MK rendelet*, *1/2025. SZTFH rendelet*) történő felkészülést és az audit előkészítését automatizálja.

A rendszer **nem helyettesíti a humán auditort**, és **nem „fekete dobozként” működik**: minden AI által generált megállapítást strukturált SQL tudásbázishoz köt, szó szerinti idézettel és oldalszámmal támaszt alá, és kötelező `TERVEZET – SZAKÉRTŐI FELÜLVIZSGÁLAT SZÜKSÉGES` címkével lát el (*Human-in-the-Loop*).

---

## 🏗️ 2. Rendszerarchitektúra és Kiemelt Innovációk

1. **Strukturált SQL Tudásbázis (`Jogszabály → Követelmény → Kontroll → Bizonyíték → Kérdés`):**
   * Helyi **MS SQL Server LocalDB** adatbázis 7 relációs táblával (`database/04_SQL_Adatbazis_Modell_es_Seed_NIS2_AuditGuard.sql` és EF Core `AuditGuardDbContext`).
   * A jogszabályi paragrafusokat nem az AI találja ki, hanem a determinisztikus SQL adatbázis szolgáltatja.
2. **Adatvédelmi Pajzs (`PiiAnonymizerService` – NIS2 9. § megfelelés):**
   * Az ügyfelek dokumentumai soha nem tárolódnak az EU-n kívül.
   * Külső API-hívás előtt a C# maszkoló modul visszafordítható tokenekre cseréli a cégneveket (`[ÜGYFÉL_SZERVEZET]`), személyneveket, e-mail címeket, adószámokat, bankszámlaszámokat, telefonszámokat és belső IP-címeket.
3. **Áramszünet- és Hiba-tűrő Hibrid AI Motor (`DualEngineAiOrchestrator` – ADR-09 & ADR-10):**
   * **Primary Engine:** Google AI Studio (**Gemini `gemini-3.8-flash`**, `/v1beta/interactions`) `temperature = 0.1` és kényszerített JSON-sémával (`response_format`). *Ingyenes API-kulcs mellett csak szintetikus demó-dokumentumok küldhetők online!*
   * **Fallback / Offline Engine:** Helyi szerveren (`localhost:11434`) futó **Ollama** (`qwen2.5:7b-instruct-q4_K_M`) lokális nyelvi modell, amely 5 másodperces időtúllépés, internetkiesés, áramszünet (UPS üzem), valós ügyféladat vagy *„Magas”* biztonsági osztályú EIR esetén 100%-ig offline veszi át a feldolgozást.
   * **Opcionális értesítő:** **OpenClaw Gateway → Discord** (kizárólag anonim metaadat: darabszámok és státuszok).
4. **Programozott Hallucináció-őr (`HallucinationValidatorService`):**
   * A C# backend normalizált szövegkereséssel ellenőrzi, hogy az AI által visszaadott `mire_alapozza_pontos_idezet` szó szerint és a megjelölt `[OLDAL n]` oldalon szerepel-e a feltöltött dokumentumban.
5. **Kétzónás Webes Kiberbiztonsági Védelem (`Zero Public Exposure`):**
   * **Publikus zóna (`Nis2AuditGuard.PublicWeb`):** IP-alapú `RateLimiter` (DDoS ellen), rejtett *Honeypot* csapdamező + 3 mp *Time-Trap* (Spam/Bot ellen), Anti-CSRF tokenek, biztonsági HTTP fejlécek. Admin belépési felület nem létezik.
   * **Auditori zóna (`Nis2AuditGuard.ManagerWeb`):** Kizárólag `127.0.0.1` (Loopback) interfészen érhető el; Prompt Injection elleni XML-szeparációval (`<anonimizalt_dokumentum>`) és 3-2-1-1-0 AES-256-GCM DRP mentéssel.

---

## 📂 3. A Repository Felépítése

```text
Nis2AuditGuard.sln
├── src/
│   ├── Nis2AuditGuard.Core/          # Domain entitások, EF Core DbContext, Seed, PII-maszkoló,
│   │                                 # PDF/DOCX szövegkinyerő, AI Orchestrator, Validátor, Export, DRP
│   ├── Nis2AuditGuard.PublicWeb/     # Publikus előszűrő, irányár-kalkulátor és JSON ügyfélcsomag-export
│   └── Nis2AuditGuard.ManagerWeb/    # Zárt (127.0.0.1) auditori dashboard, Gap-analízis és DRP konzol
├── tests/
│   ├── Nis2AuditGuard.Tests/         # xUnit unit és WebApplicationFactory integrációs tesztek
│   └── Nis2AuditGuard.Eval/          # Golden-set AI-eredményességmérő és dokumentum-segédeszköz
├── database/
│   └── 04_SQL_Adatbazis_Modell_es_Seed_NIS2_AuditGuard.sql
├── docs/                             # Projekt-dokumentáció (Markdown + beadandó .docx fájlok)
│   ├── CONTEXT.md, DECISIONS.md, TRACKING.md, HLD.md
│   ├── 01_PAD_Projekt_Alapito_Dokumentum_NIS2_AuditGuard_FULABV.docx
│   ├── 02_Specifikacio_HLD_BPMN_es_WebBiztonsag_NIS2_AuditGuard_FULABV.docx
│   └── 03_AI_Modell_es_Felhasznaloi_Dokumentacio_NIS2_AuditGuard_FULABV.docx
└── archive/                          # Korábbi statikus HTML prototípusok és kezdeti kódvázlatok
```

---

## 🚀 4. Telepítés és Futtatás (.NET 10 LTS)

1. **Adatbázis:** MS SQL Server LocalDB megléte esetén az alkalmazás automatikusan létrehozza és seedeli a `Nis2AuditGuardDb` adatbázist (vagy futtatható a `database/04_SQL_Adatbazis_Modell_es_Seed_NIS2_AuditGuard.sql` script). LocalDB hiányában a rendszer SQLite adatbázisra vált.
2. **AI-kulcs beállítása (User Secrets – soha nem kerül Gitbe):**
   ```powershell
   dotnet user-secrets set "AI:GeminiApiKey" "<SAJÁT_GEMINI_API_KULCS>" --project src/Nis2AuditGuard.ManagerWeb
   ```
3. **Fordítás, tesztelés és indítás:**
   ```powershell
   dotnet build Nis2AuditGuard.sln -warnaserror
   dotnet test tests/Nis2AuditGuard.Tests
   dotnet run --project src/Nis2AuditGuard.PublicWeb
   dotnet run --project src/Nis2AuditGuard.ManagerWeb
   ```
