# 🛡️ NIS2-AuditGuard
**Mesterséges intelligenciával támogatott, hibrid (Dual-Engine) NIS2 tanácsadói és audit-előkészítő rendszer**

* **Szerző / Projektvezető:** Károlyi Béla (Neptun-kód: `FULABV`)
* **Intézmény:** Milton Friedman Egyetem – *Szoftverfejlesztési projekt*
* **Oktató:** Dr. Bárkányi Pál PhD docens
* **Időszak:** 2026/2027. őszi félév

---

## 🎯 1. A projekt célja és koncepciója
A **NIS2-AuditGuard** egy kétzónás (publikus előszűrő + védett belső tanácsadói/auditori munkafelület) webalkalmazás, amely a magyarországi NIS2 kiberbiztonsági megfelelésre (*2024. évi LXIX. törvény*, *7/2024. (VI. 24.) MK rendelet*, *1/2025. SZTFH rendelet*) történő felkészülést és az audit előkészítését automatizálja.

A rendszer **nem helyettesíti a humán auditort**, és **nem „fekete dobozként” működik**: minden AI által generált megállapítást strukturált SQL tudásbázishoz köt, szó szerinti idézettel és oldalszámmal támaszt alá, és kötelező `TERVEZET – SZAKÉRTŐI FELÜLVIZSGÁLAT SZÜKSÉGES` címkével lát el.

---

## 🏗️ 2. Rendszerarchitektúra és Kiemelt Innovációk

1. **Strukturált SQL Tudásbázis (`Jogszabály → Követelmény → Kontroll → Bizonyíték → Kérdés`):**
   * Helyi **MS SQL Server LocalDB** adatbázis 7 relációs táblával (`04_SQL_Adatbazis_Modell_es_Seed_NIS2_AuditGuard.sql`).
   * A jogszabályi paragrafusokat nem az AI találja ki, hanem a determinisztikus SQL adatbázis szolgáltatja.
2. **Adatvédelmi Pajzs (`PiiAnonymizerService` – NIS2 9. § megfelelés):**
   * Az ügyfelek dokumentumai soha nem tárolódnak az EU-n kívül.
   * Külső API-hívás előtt a C# maszkoló modul eltávolítja a cégneveket (`[ÜGYFÉL_SZERVEZET]`), e-mail címeket, telefonszámokat és belső IP-címeket.
3. **Áramszünet- és Hiba-tűrő Hibrid AI Motor (`DualEngineAiOrchestrator`):**
   * **Primary Engine:** Google AI Studio (**Gemini 2.5 Pro API**) `temperature = 0.1` és kényszerített JSON-sémával.
   * **Fallback / Offline Engine:** Helyi szerveren (`localhost`) futó **OpenClaw + Ollama** lokális nyelvi modell, amely 5 másodperces időtúllépés, internetkiesés, áramszünet (akkumulátoros/UPS üzem) vagy *„Magas”* biztonsági osztályú EIR esetén 100%-ig offline veszi át a feldolgozást.
4. **Programozott Hallucináció-őr (`HallucinationValidatorService`):**
   * A C# backend ellenőrzi, hogy az AI által visszaadott `mire_alapozza_pontos_idezet` szó szerint szerepel-e a feltöltött dokumentumban.
5. **Webes Kiberbiztonsági Védelem:**
   * **DDoS ellen:** ASP.NET Core IP-alapú `RateLimiter` middleware.
   * **Spam/Bot ellen:** Rejtett *Honeypot* csapdamező és *Time-Trap* beküldési időellenőrzés.
   * **Prompt Injection ellen:** XML-tag szeparáció (`<anonimizalt_dokumentum>`).

---

## 📂 3. A Repository / Dokumentációs Csomag Felépítése

* `01_PAD_Projekt_Alapito_Dokumentum_NIS2_AuditGuard_FULABV` – Kitöltött Projekt Alapító Dokumentum (15%).
* `02_Specifikacio_HLD_BPMN_es_WebBiztonsag_NIS2_AuditGuard_FULABV` – Alternatíva-elemzés (5%), BPMN 2.0 folyamatábra (5%), Adatbázis-terv (10%), HLD és Webbiztonság.
* `03_AI_Modell_es_Felhasznaloi_Dokumentacio_NIS2_AuditGuard_FULABV` – Kötelező AI-modell dokumentáció + Felhasználói kézikönyv (20%).
* `04_SQL_Adatbazis_Modell_es_Seed_NIS2_AuditGuard.sql` – Futtatható T-SQL adatbázis-létrehozó és NIS2 seed script.
* `05_CSharp_Forraskod_NIS2_AuditGuard_Core.cs` – Felkommentelt C# (.NET 8) magkód (45%).

---

## 🚀 4. Telepítés és Futtatás

1. Futtassa a `04_SQL_Adatbazis_Modell_es_Seed_NIS2_AuditGuard.sql` scriptet **MS SQL Server LocalDB** környezetben.
2. Állítsa be az `appsettings.json` fájlban a `GeminiApiKey` és az `OpenClawLocalUrl` (`http://localhost:11434/api/generate`) értékeket.
3. Indítsa el az alkalmazást a `dotnet run` paranccsal.

