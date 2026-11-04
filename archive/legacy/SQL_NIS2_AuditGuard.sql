-- SQL Adatbázis Modell a NIS2-AuditGuard rendszerhez (Microsoft SQL Server)

-- 1. Jogszabályok tábla (Nem AI találja ki, fixen letárolt 2024. évi LXIX. tv.)
CREATE TABLE Jogszabalyok (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    TorvenyNeve NVARCHAR(255) NOT NULL,
    Paragrafus NVARCHAR(50) NOT NULL,
    HatalyosSzoveg NVARCHAR(MAX) NOT NULL,
    BiztonsagiOsztaly NVARCHAR(50) -- Alap, Jelentős, Magas
);

-- 2. NIS2 Követelmények és Kontrollok tábla
CREATE TABLE Kovetelmenyek (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    JogszabalyId INT FOREIGN KEY REFERENCES Jogszabalyok(Id),
    KontrollNeve NVARCHAR(255) NOT NULL,
    ElvartBizonyitek NVARCHAR(255) NOT NULL,
    AlapAuditKerdes NVARCHAR(MAX) NOT NULL
);

-- 3. Ügyfelek / Vizsgált Szervezetek tábla
CREATE TABLE Ugyfelek (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Cegnev NVARCHAR(255) NOT NULL,
    Agazat NVARCHAR(100) NOT NULL,
    FoglalkoztatottiLetszam INT,
    EvesArbevetelEur DECIMAL(18,2),
    EirBiztonsagiOsztaly NVARCHAR(50),
    ErintettsegStatusz NVARCHAR(50) -- Érintett (1. melléklet), Érintett (2. melléklet), Nem érintett
);

-- 4. Esemény- és AI Napló (Kötelező a kiírás szerint)
CREATE TABLE Esemynaplo (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Idopont DATETIME DEFAULT GETDATE(),
    UgyfelId INT FOREIGN KEY REFERENCES Ugyfelek(Id),
    EsemenyTipus NVARCHAR(100) NOT NULL, -- "AI_HIVAS_DOC_ELEMZES", "BEJELENTKEZES", stb.
    ModellTipus NVARCHAR(50), -- "Gemini Pro", "Ollama Fallback"
    FeldolgozottAdat_Anonimizalt NVARCHAR(MAX),
    AiKimenet_JSON NVARCHAR(MAX),
    HallucinacioGyanu BIT DEFAULT 0
);

-- 5. AI által elemzett hiányosságok (Gap-analízis eredményei)
CREATE TABLE Hianyossagok (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    UgyfelId INT FOREIGN KEY REFERENCES Ugyfelek(Id),
    KovetelmenyId INT FOREIGN KEY REFERENCES Kovetelmenyek(Id),
    AI_Allitas NVARCHAR(MAX) NOT NULL,
    AI_MegfelelesiStatusz NVARCHAR(50), -- RÉSZLEGES, HIÁNYZÓ, MEGFELELŐ
    ForrasDokumentumNeve NVARCHAR(255),
    ForrasOldalszam NVARCHAR(50),
    SzakertoiJovahagyas NVARCHAR(50) DEFAULT 'TERVEZET', -- TERVEZET, JÓVÁHAGYVA, ELUTASÍTVA
    AuditorMegjegyzes NVARCHAR(MAX)
);

-- Példa adatfeltöltés a Jogszabályokhoz:
INSERT INTO Jogszabalyok (TorvenyNeve, Paragrafus, HatalyosSzoveg, BiztonsagiOsztaly)
VALUES ('2024. évi LXIX. tv.', '15. § (1)', 'Az érintett szervezet az elektronikus információs rendszereinek és azok fizikai környezetének biztonsága érdekében köteles arányos és megfelelő technikai, szervezési intézkedéseket végrehajtani.', 'Alap');

