-- ==========================================================================================
-- PROJEKT: NIS2-AuditGuard - Mesterséges intelligenciával támogatott NIS2 audit-előkészítő
-- SZERZŐ:  Károlyi Béla (Neptun-kód: FULABV) - Milton Friedman Egyetem
-- LEÍRÁS:  Strukturált relációs adatbázis-séma (MS SQL Server LocalDB / Express) és
--          kezdeti jogszabályi tudásbázis (Seed Data) a 2024. évi LXIX. törvény és a
--          7/2024. (VI. 24.) MK rendelet alapján.
-- ==========================================================================================

CREATE DATABASE Nis2AuditGuardDb;
GO
USE Nis2AuditGuardDb;
GO

-- 1. TÁBLA: Jogszabályok (A tudásbázis legfelső szintje)
CREATE TABLE dbo.Jogszabalyok (
    JogszabalyId INT IDENTITY(1,1) PRIMARY KEY,
    Kod NVARCHAR(50) NOT NULL,                  -- Pl. '2024. évi LXIX. tv.'
    Cim NVARCHAR(300) NOT NULL,                 -- Jogszabály hivatalos címe
    ParagrafusSzakasz NVARCHAR(100) NOT NULL,   -- Pl. '6. § (3) bek.' vagy '9. § (4) bek.'
    HatalyosSzoveg NVARCHAR(MAX) NOT NULL,      -- A pontos törvényi szöveg (hallucináció ellen)
    HatalybaLepes DATE NOT NULL DEFAULT '2025-01-01'
);

-- 2. TÁBLA: NIS2 Követelmények (Jogszabály -> Követelmény kapcsolat)
CREATE TABLE dbo.Kovetelmenyek (
    KovetelmenyId INT IDENTITY(1,1) PRIMARY KEY,
    JogszabalyId INT NOT NULL,
    KovetelmenyKod NVARCHAR(50) NOT NULL UNIQUE,-- Pl. 'NIS2-AC-01', 'NIS2-CP-01'
    Terulet NVARCHAR(150) NOT NULL,             -- Pl. 'Hozzáférés-kezelés', 'Mentés és helyreállítás'
    Megnevezes NVARCHAR(250) NOT NULL,
    ElvartAllapotLeiras NVARCHAR(MAX) NOT NULL,
    MinBiztonsagiOsztaly NVARCHAR(20) NOT NULL CHECK (MinBiztonsagiOsztaly IN ('Alap', 'Jelentos', 'Magas')),
    CONSTRAINT FK_Kovetelmenyek_Jogszabalyok FOREIGN KEY (JogszabalyId) REFERENCES dbo.Jogszabalyok(JogszabalyId)
);

-- 3. TÁBLA: Kontrollok, Elvárt Bizonyítékok és Auditkérdések (Követelmény -> Kontroll -> Bizonyíték -> Kérdés)
CREATE TABLE dbo.Kontrollok_Es_Kerdesek (
    KontrollId INT IDENTITY(1,1) PRIMARY KEY,
    KovetelmenyId INT NOT NULL,
    ElvartKontrollLeiras NVARCHAR(MAX) NOT NULL,
    ElvartBizonyitekTipus NVARCHAR(250) NOT NULL, -- Pl. 'Hozzáférés-kezelési szabályzat + AD felülvizsgálati jegyzőkönyv'
    FelelosSzerepkor NVARCHAR(100) NOT NULL,      -- Pl. 'IT vezető / IBF / Ügyvezető'
    AlapAuditKerdes NVARCHAR(500) NOT NULL,
    MelyitoAuditKerdes NVARCHAR(500) NOT NULL,
    BizonyitekBekeroKerdes NVARCHAR(500) NOT NULL,
    EllenorzoAuditKerdes NVARCHAR(500) NOT NULL,
    CONSTRAINT FK_Kontrollok_Kovetelmenyek FOREIGN KEY (KovetelmenyId) REFERENCES dbo.Kovetelmenyek(KovetelmenyId)
);

-- 4. TÁBLA: Ügyfelek és Elektronikus Információs Rendszerek (EIR)
CREATE TABLE dbo.Ugyfelek_Es_EIR (
    UgyfelId INT IDENTITY(1,1) PRIMARY KEY,
    CegNev NVARCHAR(200) NOT NULL,
    Agazat NVARCHAR(150) NOT NULL,                -- 2. vagy 3. melléklet szerinti ágazat
    MellekletBesorolas NVARCHAR(50) NOT NULL,     -- 'Kiemelten kockázatos (2. mell.)' / 'Kockázatos (3. mell.)'
    FoglalkoztatottiLetszam INT NOT NULL,
    EvesNettoArbevetelMillioEur DECIMAL(10,2) NOT NULL,
    EirMegnevezes NVARCHAR(200) NOT NULL,
    BiztonsagiOsztaly NVARCHAR(20) NOT NULL CHECK (BiztonsagiOsztaly IN ('Alap', 'Jelentos', 'Magas')),
    AdatkezelesOrszaga NVARCHAR(100) NOT NULL DEFAULT 'Magyarorszag (Lokalis)', -- 2024. évi LXIX. tv. 22. § (1) g)
    EloszuresEredmeny NVARCHAR(100) NOT NULL,
    BecsultIranyarHuf DECIMAL(12,0) NOT NULL,
    LetrehozasIdopontja DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
);

-- 5. TÁBLA: Feltöltött Ügyféldokumentumok és Auditbizonyítékok
CREATE TABLE dbo.Feltoltott_Bizonyitekok (
    BizonyitekId INT IDENTITY(1,1) PRIMARY KEY,
    UgyfelId INT NOT NULL,
    DokumentumNev NVARCHAR(255) NOT NULL,
    DokumentumTipus NVARCHAR(100) NOT NULL,       -- Pl. 'IBSZ', 'Mentési szabályzat', 'AD jogosultságlista'
    KinyertSzovegIndexelt NVARCHAR(MAX) NOT NULL, -- Oldalszám és szakasz címkékkel ellátott szöveg
    DokumentumEvszam INT NOT NULL,                -- Eltéréskereséshez (pl. 3 éves jegyzőkönyv kiszűrése)
    Sha256Hash CHAR(64) NOT NULL,                 -- Sértetlenség igazolása (2024. évi LXIX. tv. 4. § 86.)
    FeltoltesIdopontja DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_Bizonyitekok_Ugyfelek FOREIGN KEY (UgyfelId) REFERENCES dbo.Ugyfelek_Es_EIR(UgyfelId)
);

-- 6. TÁBLA: Fehér Dobozos AI-Értékelések és Gap-Analízis Eredmények
CREATE TABLE dbo.AI_Ertekelesek_Gap (
    ErtekelesId INT IDENTITY(1,1) PRIMARY KEY,
    UgyfelId INT NOT NULL,
    KovetelmenyId INT NOT NULL,
    BizonyitekId INT NOT NULL,
    -- A 6 kötelező "Fekete doboz" ellenes mező:
    MitAllitAzAi NVARCHAR(MAX) NOT NULL,
    MireAlapozzaPontosIdezet NVARCHAR(MAX) NOT NULL,
    ForrasOldalEsSzakasz NVARCHAR(150) NOT NULL,
    KapcsolodoJogszabalyHivatkozas NVARCHAR(250) NOT NULL,
    MegbizhatosagSzazalek INT NOT NULL CHECK (MegbizhatosagSzazalek BETWEEN 0 AND 100),
    MitKellASzakertonekEllenoriznie NVARCHAR(MAX) NOT NULL,
    -- Kötelező előírás vs. AI jó gyakorlat megkülönböztetése:
    JogszabalyElorirasHiany NVARCHAR(MAX) NOT NULL,
    AiJoGyakorlatJavaslat NVARCHAR(MAX) NOT NULL,
    -- Determinisztikus C# validáció és Humán jóváhagyás (HITL):
    IdezetValidacioOk BIT NOT NULL DEFAULT 0,
    HasznaltAiMotor NVARCHAR(50) NOT NULL,        -- 'Gemini-2.5-Pro (Anonim)' vagy 'OpenClaw-Local-Fallback'
    SzakertoiStatusz NVARCHAR(60) NOT NULL DEFAULT 'TERVEZET - SZAKERTOI FELULVIZSGALAT SZUKSEGES',
    AuditorMegjegyzes NVARCHAR(MAX) NULL,
    ErtekelesIdopontja DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_Ertekelesek_Ugyfelek FOREIGN KEY (UgyfelId) REFERENCES dbo.Ugyfelek_Es_EIR(UgyfelId),
    CONSTRAINT FK_Ertekelesek_Kovetelmenyek FOREIGN KEY (KovetelmenyId) REFERENCES dbo.Kovetelmenyek(KovetelmenyId),
    CONSTRAINT FK_Ertekelesek_Bizonyitekok FOREIGN KEY (BizonyitekId) REFERENCES dbo.Feltoltott_Bizonyitekok(BizonyitekId)
);

-- 7. TÁBLA: Sérthetetlen Esemény- és AI-Napló (2024. évi LXIX. tv. 38. § e-f) pont)
CREATE TABLE dbo.Audit_Es_AI_Naplo (
    NaploId BIGINT IDENTITY(1,1) PRIMARY KEY,
    IdopontUtc DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    FelhasznaloAzonosito NVARCHAR(100) NOT NULL,
    MuveletTipus NVARCHAR(100) NOT NULL,          -- Pl. 'ELOSZURES', 'DOKUMENTUM_AI_ELEMZES', 'AUDITOR_JOVAHAGYAS'
    KuldesreKerultAnonimPrompt NVARCHAR(MAX) NULL,
    NyersAiValaszJson NVARCHAR(MAX) NULL,
    HasznaltModell NVARCHAR(50) NOT NULL,
    FallbackAktivalt BIT NOT NULL DEFAULT 0,
    ValaszidoMs INT NOT NULL,
    KliensIpHash NVARCHAR(64) NOT NULL
);
GO

-- ==========================================================================================
-- KEZDETI TUDÁSBÁZIS FELTÖLTÉSE (SEED DATA) - Hallucináció-mentes jogszabályi alap
-- ==========================================================================================

INSERT INTO dbo.Jogszabalyok (Kod, Cim, ParagrafusSzakasz, HatalyosSzoveg, HatalybaLepes)
VALUES
('2024. évi LXIX. tv.', 'Törvény Magyarország kiberbiztonságáról', '5. § (1) és 38. § c)', 
 'Az elektronikus információs rendszerek teljes életciklusában biztosítani kell a kezelt adatok bizalmasságát, sértetlenségét és rendelkezésre állását, valamint azt, hogy a feljogosított személyek és programok kizárólag a hozzáférési jogaik tárgyát képező adatokhoz férhetnek hozzá.', '2025-01-01'),
('7/2024. (VI. 24.) MK r.', 'MK rendelet a biztonsági osztályba sorolásról és a védelmi intézkedésekről', 'Hozzáférés-kezelés (AC)', 
 'A szervezet köteles dokumentált hozzáférés-kezelési eljárást működtetni, a felhasználói és rendszergazdai jogosultságokat nyilvántartani, kilépéskor haladéktalanul visszavonni, és legalább évente felülvizsgálni.', '2025-01-01'),
('2024. évi LXIX. tv.', 'Törvény Magyarország kiberbiztonságáról', '38. § h) és 7/2024. MK r. (CP)', 
 'Fizikai vagy műszaki biztonsági esemény bekövetkeztekor biztosítani kell az adatok és szolgáltatások rendelkezésre állásának mihamarabbi helyreállítását, rendszeres biztonsági mentések és dokumentált visszaállítási próbák végrehajtásával.', '2025-01-01'),
('2024. évi LXIX. tv.', 'Törvény Magyarország kiberbiztonságáról', '9. § (2)-(4) és 22. § (1) g)', 
 'Nem privát felhőszolgáltatás és külföldi adatkezelés kizárólag előzetes adatosztályozás alapján vehető igénybe; nyilván kell tartani az adatkezelés helyszínét és az ország megnevezését.', '2025-01-01');

INSERT INTO dbo.Kovetelmenyek (JogszabalyId, KovetelmenyKod, Terulet, Megnevezes, ElvartAllapotLeiras, MinBiztonsagiOsztaly)
VALUES
(2, 'NIS2-AC-02', 'Hozzáférés-kezelés', 'Jogosultságok életciklus-kezelése és időszakos felülvizsgálata',
 'A szervezet rendelkezik hozzáférés-kezelési szabályzattal, belépési/kilépési jogosultságkezelési folyamattal, és legalább évente dokumentáltan felülvizsgálja a kiosztott jogosultságokat.', 'Alap'),
(3, 'NIS2-CP-01', 'Üzletmenet-folytonosság és Mentés', 'Biztonsági mentések és éves visszaállítási próba',
 'A szervezet rendszeres, elkülönített biztonsági mentést készít az EIR adatairól, és legalább évente dokumentált visszaállítási próbával ellenőrzi a mentések sértetlenségét.', 'Alap'),
(4, 'NIS2-CLD-01', 'Felhőbiztonság és Adatszuverenitás', 'Adatosztályozás és felhőszolgáltatások nyilvántartása',
 'A szervezet elvégezte az adatosztályozást, és nyilvántartja az adatkezelés országát, valamint az igénybe vett felhőszolgáltatásokat.', 'Alap');

INSERT INTO dbo.Kontrollok_Es_Kerdesek (KovetelmenyId, ElvartKontrollLeiras, ElvartBizonyitekTipus, FelelosSzerepkor, AlapAuditKerdes, MelyitoAuditKerdes, BizonyitekBekeroKerdes, EllenorzoAuditKerdes)
VALUES
(1, 'Belépő/kilépő jogosultsági munkafolyamat és éves Active Directory jogosultság-felülvizsgálat.',
 'Hozzáférés-kezelési szabályzat, AD jogosultságlista, Éves felülvizsgálati jegyzőkönyv', 'IT Vezető / HR / IBF',
 'Hogyan történik egy munkavállaló jogosultságainak megadása és megszüntetése kilépéskor?',
 'Ki kezdeményezi a jogosultság megszüntetését, és milyen határidőn belül hajtja végre az IT?',
 'Kérem, mutassa be az elmúlt 12 hónapban elvégzett jogosultság-felülvizsgálat aláírt jegyzőkönyvét!',
 'Található-e az aktív AD-felhasználók között olyan személy, akinek a munkaviszonya már megszűnt?'),
(2, 'Automatizált napi/heti mentés és éves visszaállítási tesztelési eljárás.',
 'Mentési szabályzat, Mentési naplókivonat, Éves visszaállítási próba jegyzőkönyv', 'IT Vezető / IBF',
 'Milyen gyakorisággal és hová történik a kritikus EIR-ek adatainak biztonsági mentése?',
 'Fizikailag vagy logikailag (immutable) el van-e különítve a mentés a zsarolóvírusok ellen?',
 'Kérem, adja át a legutóbbi visszaállítási próba (restore test) dátumozott jegyzőkönyvét!',
 'Mennyi időt vett igénybe a tényleges visszaállítás a legutóbbi teszt során a BCP-ben rögzített RTO-hoz képest?');
GO

