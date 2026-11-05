# NIS2-AuditGuard – Élő Konzultációs és Védési Forgatókönyv (10 perces Demó)

- **Hallgató:** Károlyi Béla (Neptun-kód: `FULABV`)
- **Tantárgy:** Szoftverfejlesztési projekt – Milton Friedman Egyetem
- **Oktató:** Dr. Bárkányi Pál PhD docens
- **Konzultáció:** 2026. november 20. | **Végleges védés:** 2026. december 11.

---

## 0. Előkészület (1 perccel a bemutató előtt)

1. Ellenőrizd, hogy a tálcán fut-e az **Ollama** (és igény szerint az **OpenClaw**).
2. Nyiss egy PowerShell ablakot a projekt gyökerében (`c:\Users\Béci\source\repos\Nis2AuditProject`) és futtasd:
   ```powershell
   .\start-demo.ps1
   ```
3. A szkript ellenőrzi az Ollamát (`qwen2.5:7b-instruct-q4_K_M`), lefordítja a `.NET 10.0 LTS` solutiont, elindítja a két fizikailag szeparált webalkalmazást, és megnyitja a böngészőfüleket:
   - **1. Zóna (Publikus Előszűrő):** `http://localhost:5070`
   - **2. Zóna (Zárt Menedzseri Munkaállomás):** `http://127.0.0.1:5080`

---

## 1. felvonás: Fizikai Kétzónás Architektúra és Zero Public Exposure (0:00 – 2:00)

**Mit mutatsz meg (`http://localhost:5070`):**
1. Mutasd be a **Publikus NIS2 Előszűrő és Árajánlat-kalkulátor** oldalt (`Nis2AuditGuard.PublicWeb`).
2. Hangsúlyozd a **Zero Public Exposure (ADR-05, ADR-12)** elvet:
   - A publikus alkalmazásban **fizikailag nem létezik** `/admin`, `/login` vagy ügyféldokumentum-tároló végpont (támadási felület minimalizálása a *2024. évi LXIX. tv. 38. §* szerint).
   - A végpontot beépített **Rate Limiter** (15 kérés/perc/IP), rejtett **Honeypot** csapdamező és **3 másodperces Time-Trap** védi a botok ellen, külső Google reCAPTCHA adatmegosztás nélkül.
3. Válassz ki egy ágazatot (pl. *Gyártás / Energetika*), állíts be `85 fő` létszámot, `14 millió EUR` árbevételt és `Jelentős` biztonsági osztályt, majd kattints a kalkulációra és a **JSON ügyfél-adatcsomag letöltésére**.

---

## 2. felvonás: Zárt Menedzseri Munkaállomás és Determinisztikus SQL Tudásbázis (2:00 – 4:00)

**Mit mutatsz meg (`http://127.0.0.1:5080`):**
1. Válts át a **Zárt Menedzseri & Auditori Konzolra** (`Nis2AuditGuard.ManagerWeb`).
2. Mutasd meg a felső zöld/kék információs sávot:
   - A szerver kizárólag a `127.0.0.1` (Loopback) interfészen hallgat; minden külső IP-ről érkező kérést `403 Forbidden` válasszal elutasít.
   - A fejlécben élőben látszik: `✔ .NET 10 Backend AKTÍV (11 SQL követelmény | Ollama: qwen2.5:7b-instruct-q4_K_M)`.
3. **1. Modul (Ügyfél-import):** Kattints a **„Helyi betöltés”** vagy a `.json` fájl tallózása gombra – az előzőleg generált ügyfél bekerül a helyi SQL adatbázisba (`Ugyfel_Es_EIR` tábla).
4. **2. Modul (Determinisztikus SQL Tudásbázis):** Válts a legördülő listában az `AC`, `CP`, `IR`, `SR` követelmények között.
   - **Kulcsmondat az oktatónak:** *„A rendszerben soha nem a nyelvi modell találja ki a jogszabályhelyet vagy a paragrafust. A 2024. évi LXIX. törvény és a 7/2024. MK rendelet előírásai determinisztikus SQL relációkból (Jogszabaly_Hivatkozas -> NIS2_Kovetelmeny -> Kontroll_Es_Kerdes) érkeznek.”*

---

## 3. felvonás: Lokális C# PII-Anonimizáló és ADR-09 Adatvédelmi Szabály (4:00 – 6:00)

**Mit mutatsz meg (4. Modul – 1. Lépés):**
1. Kattints a **„Minta #1: Hiányos Szabályzat + Eltérő Bizonyíték”** gombra. Mutasd meg, hogy a szövegben valós cégnév (*Pannon-Gépész Zrt.*), személynév (*Kovács János*), belső IP-cím (`192.168.10.45`), e-mail, telefonszám és adószám szerepel.
2. Kattints a **„🛡️ 1. Lépés: Lokális C# PII-Anonimizáló Előnézet”** gombra!
3. Mutasd meg a megjelenő sötét kódablakot:
   - A C# `PiiAnonymizerService` még az AI-hívás előtt minden érzékeny adatot visszafordítható tokenekre (`[UGYFEL_SZERVEZET]`, `[BELSO_IP_01]`, `[EMAIL_01]`, `[SZEMELY_01]`, `[ADOSZAM_01]`) cserélt.
   - Hangsúlyozd az **ADR-09 szabályt**: ingyenes Gemini kulcs mellett valós ügyféladat soha nem megy felhőbe, és **„Magas” biztonsági osztályú EIR** választásakor a rendszer automatikusan átkapcsol **100% Lokális Ollama (`qwen2.5:7b-instruct-q4_K_M`)** üzemmódba.

---

## 4. felvonás: Fehér Dobozos AI Gap-Analízis, Élő Ollama és C# Hallucináció-Őr (6:00 – 8:30)

**Mit mutatsz meg (4. és 5. Modul):**
1. Kattints a **„🤖 2. Lépés: Fehér Dobozos Gap-Analízis & Idézet-Validáció Futtatása”** gombra!
   - A háttérben a `.NET 10` `DualEngineAiOrchestrator` meghívja a helyi Ollama (`qwen2.5:7b-instruct-q4_K_M`) modellt strukturált JSON sémával, majd a visszakapott választ átfuttatja a C# `HallucinationValidatorService`-en.
2. Mutasd be a **6 kötelező fehér dobozos (White-Box) elemet** a generált kártyán:
   1. *Mit állít az AI?*
   2. *Mire alapozza? (Szó szerinti idézet + C# `IndexedDocument.FindExact` validáció: `✔ Hiteles forrás`)*
   3. *Mely dokumentumból származik (fájlnév, oldalszám)?*
   4. *Mely követelményhez kapcsolódik (determinisztikus SQL jogszabályhely)?*
   5. *Megbízhatóság % és válaszidő (ms)*
   6. *Mit kell a szakértőnek ellenőriznie + Kötelező jogszabályi előírás vs. AI jó gyakorlat szétválasztása*
3. **Hallucináció-teszt élő bemutatása:**
   - Kattints a piros **„Minta #2: Hallucináció-Teszt Szöveg”** gombra, majd ismét a **„🤖 2. Lépés”** gombra!
   - Mutasd meg a piros `⚠️ HALLUCINÁCIÓ-GYANÚ: Az idézet nem található a feltöltött fájlban!` jelzést: a C# `HallucinationValidatorService` észlelte, hogy az idézet nem szerepel a feltöltött szövegben, `IdezetValidacioOk = false` státuszra állította, és figyelmeztette az auditort.
4. **Human-in-the-Loop (HITL):**
   - Mutasd meg, hogy minden kártya alapértelmezett státusza `TERVEZET – SZAKÉRTŐI FELÜLVIZSGÁLAT SZÜKSÉGES`.
   - Írj be egy megjegyzést, majd kattints a **„✔ Jóváhagyás”** gombra – a VMI és SZEKI mutatók, valamint az adatbázis-állapot azonnal frissül.

---

## 5. felvonás: AES-256-GCM DRP Mentés, Visszaállítási Próba és Valódi DOCX/XLSX Export (8:30 – 10:00)

**Mit mutatsz meg (3. és 6. Modul):**
1. **3. Modul (Offline Backup & DRP):**
   - Kattints a **„🔒 Titkosított 3-2-1 Mentési Csomag (AES-256-GCM) + SHA-256 Hash”** gombra -> megjelenik a PBKDF2 (100 000 iteráció) + AES-256-GCM mentés azonosítója és a `2024. évi LXIX. tv. 4. § 86.` szerinti SHA-256 sértetlenségi hash.
   - Kattints a **„♻️ Visszaállítási Próba (DRP Restore Verify) & DOCX Jegyzőkönyv Letöltése”** gombra -> a C# backend visszafejti a mentést, ellenőrzi az SHA-256 hash-t, kiszámítja az RTO-t (néhány ezredmásodperc), és **automatikusan letölti a hivatalos MS Word (`.docx`) Visszaállítási Próba Jegyzőkönyvet**!
2. **6. Modul (Szerepkör-alapú Jelentés & Export):**
   - Válts a három vezetői nézet (*Ügyvezetői / IT Vezetői / Auditori munkapéldány*) között, majd kattints az **Exportálás MS Word (`.docx`)** és **Exportálás Excel (`.xlsx`)** gombokra – valódi OpenXML `.docx` és ClosedXML `.xlsx` dokumentumok töltődnek le.
3. **Automata tesztek igazolása (igény esetén a terminálban):**
   ```powershell
   dotnet test Nis2AuditGuard.sln
   ```
   *(12/12 zöld xUnit teszt, 0 figyelmeztetés, 0 hiba).*

