using Microsoft.EntityFrameworkCore;
using Nis2AuditGuard.Core.Domain;

namespace Nis2AuditGuard.Core.Data;

/// <summary>
/// Strukturált NIS2 jogszabályi tudásbázis (Seed Data) a 2024. évi LXIX. törvény,
/// a 7/2024. (VI. 24.) MK rendelet és az 1/2025. SZTFH rendelet alapján.
/// Garantálja, hogy a jogszabályhelyeket nem az AI találja ki, hanem az SQL adatbázis adja (ADR-04).
/// </summary>
public static class SeedData
{
    /// <summary>Regisztrálja a kezdeti jogszabályokat, követelményeket és kontrollkérdéseket.</summary>
    /// <param name="modelBuilder">Az EF Core modellépítő példánya.</param>
    public static void Apply(ModelBuilder modelBuilder)
    {
        var hataly = new DateOnly(2025, 1, 1);

        modelBuilder.Entity<Jogszabaly>().HasData(
            new Jogszabaly
            {
                JogszabalyId = 1,
                Kod = "2024. évi LXIX. tv.",
                Cim = "Törvény Magyarország kiberbiztonságáról",
                ParagrafusSzakasz = "5. § (1) és 38. § c)",
                HatalyosSzoveg = "Az elektronikus információs rendszerek teljes életciklusában biztosítani kell a kezelt adatok bizalmasságát, sértetlenségét és rendelkezésre állását, valamint azt, hogy a feljogosított személyek és programok kizárólag a hozzáférési jogaik tárgyát képező adatokhoz férhetnek hozzá.",
                HatalybaLepes = hataly
            },
            new Jogszabaly
            {
                JogszabalyId = 2,
                Kod = "7/2024. (VI. 24.) MK r.",
                Cim = "MK rendelet a biztonsági osztályba sorolásról és a védelmi intézkedésekről",
                ParagrafusSzakasz = "Hozzáférés-kezelés (AC)",
                HatalyosSzoveg = "A szervezet köteles dokumentált hozzáférés-kezelési eljárást működtetni, a felhasználói és rendszergazdai jogosultságokat nyilvántartani, kilépéskor haladéktalanul visszavonni, és legalább évente felülvizsgálni.",
                HatalybaLepes = hataly
            },
            new Jogszabaly
            {
                JogszabalyId = 3,
                Kod = "2024. évi LXIX. tv.",
                Cim = "Törvény Magyarország kiberbiztonságáról",
                ParagrafusSzakasz = "38. § h) és 7/2024. MK r. (CP)",
                HatalyosSzoveg = "Fizikai vagy műszaki biztonsági esemény bekövetkeztekor biztosítani kell az adatok és szolgáltatások rendelkezésre állásának mihamarabbi helyreállítását, rendszeres biztonsági mentések és dokumentált visszaállítási próbák végrehajtásával.",
                HatalybaLepes = hataly
            },
            new Jogszabaly
            {
                JogszabalyId = 4,
                Kod = "2024. évi LXIX. tv.",
                Cim = "Törvény Magyarország kiberbiztonságáról",
                ParagrafusSzakasz = "9. § (2)-(4) és 22. § (1) g)",
                HatalyosSzoveg = "Nem privát felhőszolgáltatás és külföldi adatkezelés kizárólag előzetes adatosztályozás alapján vehető igénybe; nyilván kell tartani az adatkezelés helyszínét és az ország megnevezését.",
                HatalybaLepes = hataly
            },
            new Jogszabaly
            {
                JogszabalyId = 5,
                Kod = "2024. évi LXIX. tv.",
                Cim = "Törvény Magyarország kiberbiztonságáról",
                ParagrafusSzakasz = "6. § (1)-(3) és 38. § a)",
                HatalyosSzoveg = "Az érintett szervezet vezetője felel az információbiztonsági irányítási rendszer kialakításáért, az elektronikus információs rendszerek biztonsági osztályba sorolásáért és a kockázatelemzés legalább évenkénti felülvizsgálatáért.",
                HatalybaLepes = hataly
            },
            new Jogszabaly
            {
                JogszabalyId = 6,
                Kod = "2024. évi LXIX. tv.",
                Cim = "Törvény Magyarország kiberbiztonságáról",
                ParagrafusSzakasz = "25–26. § és 7/2024. MK r. (IR)",
                HatalyosSzoveg = "A jelentős kiberbiztonsági инциdenst az észleléstől számított 24 órán belül korai figyelmeztetéssel, 72 órán belül incidensbejelentéssel, egy hónapon belül pedig zárójelentéssel kell bejelenteni az eseménykezelő központnak (CSIRT).",
                HatalybaLepes = hataly
            },
            new Jogszabaly
            {
                JogszabalyId = 7,
                Kod = "7/2024. (VI. 24.) MK r.",
                Cim = "MK rendelet a biztonsági osztályba sorolásról és a védelmi intézkedésekről",
                ParagrafusSzakasz = "Azonosítás és hitelesítés (IA) – 38. § i)",
                HatalyosSzoveg = "Kiemelt jogosultságú hozzáférések és távoli elérés esetén kötelező a többtényezős hitelesítés (MFA) alkalmazása, valamint a jelszó- és hitelesítőadat-kezelési szabályok kikényszerítése.",
                HatalybaLepes = hataly
            },
            new Jogszabaly
            {
                JogszabalyId = 8,
                Kod = "2024. évi LXIX. tv.",
                Cim = "Törvény Magyarország kiberbiztonságáról",
                ParagrafusSzakasz = "38. § e)-f) és 7/2024. MK r. (AU)",
                HatalyosSzoveg = "Az elektronikus információs rendszerben végzett biztonsági szempontból releváns eseményeket manipulációtól védett módon, szinkronizált időbélyeggel naplózni kell, és a naplókat rendszeresen elemezni szükséges.",
                HatalybaLepes = hataly
            },
            new Jogszabaly
            {
                JogszabalyId = 9,
                Kod = "2024. évi LXIX. tv.",
                Cim = "Törvény Magyarország kiberbiztonságáról",
                ParagrafusSzakasz = "18. § és 7/2024. MK r. (AT)",
                HatalyosSzoveg = "A szervezet vezetői és munkavállalói számára rendszeres, szerepkörhöz igazított kiberbiztonsági tudatosító képzést és adathalász-szimulációs oktatást kell biztosítani, dokumentált részvételi nyilvántartással.",
                HatalybaLepes = hataly
            },
            new Jogszabaly
            {
                JogszabalyId = 10,
                Kod = "2024. évi LXIX. tv.",
                Cim = "Törvény Magyarország kiberbiztonságáról",
                ParagrafusSzakasz = "17. § és 38. § d) (SR – Ellátási lánc)",
                HatalyosSzoveg = "Az érintett szervezet köteles értékelni a közvetlen beszállítói és szolgáltatói kiberbiztonsági kockázatokat, és a szerződésekben rögzíteni a biztonsági, incidensbejelentési és auditálási követelményeket.",
                HatalybaLepes = hataly
            },
            new Jogszabaly
            {
                JogszabalyId = 11,
                Kod = "7/2024. (VI. 24.) MK r.",
                Cim = "MK rendelet a biztonsági osztályba sorolásról és a védelmi intézkedésekről",
                ParagrafusSzakasz = "Rendszer- és információintegritás (SI)",
                HatalyosSzoveg = "A szervezet köteles gondoskodni a sérülékenységek rendszeres azonosításáról, a biztonsági frissítések (patch management) kockázatalapú telepítéséről és a kártékony kódok elleni védelemről.",
                HatalybaLepes = hataly
            },
            new Jogszabaly
            {
                JogszabalyId = 12,
                Kod = "7/2024. (VI. 24.) MK r.",
                Cim = "MK rendelet a biztonsági osztályba sorolásról és a védelmi intézkedésekről",
                ParagrafusSzakasz = "Rendszer- és kommunikációvédelem (SC)",
                HatalyosSzoveg = "A hálózati szegmentációt, a nyilvános és belső adminisztratív interfészek leválasztását, valamint az átvitt és tárolt adatok korszerű kriptográfiai védelmét biztosítani kell.",
                HatalybaLepes = hataly
            });

        modelBuilder.Entity<Kovetelmeny>().HasData(
            new Kovetelmeny
            {
                KovetelmenyId = 1,
                JogszabalyId = 2,
                KovetelmenyKod = "NIS2-AC-02",
                Terulet = "Hozzáférés-kezelés (AC)",
                Megnevezes = "Jogosultságok életciklus-kezelése és éves felülvizsgálata",
                ElvartAllapotLeiras = "A szervezet rendelkezik hozzáférés-kezelési szabályzattal, belépési/kilépési jogosultságkezelési folyamattal, és legalább évente dokumentáltan felülvizsgálja a kiosztott jogosultságokat.",
                MinBiztonsagiOsztaly = BiztonsagiOsztalyok.Alap
            },
            new Kovetelmeny
            {
                KovetelmenyId = 2,
                JogszabalyId = 3,
                KovetelmenyKod = "NIS2-CP-01",
                Terulet = "Üzletmenet-folytonosság és Mentés (CP)",
                Megnevezes = "Rendszeres biztonsági mentések és éves visszaállítási próba",
                ElvartAllapotLeiras = "A szervezet rendszeres, elkülönített (3-2-1 elvű) biztonsági mentést készít az EIR adatairól, és legalább évente dokumentált visszaállítási próbával ellenőrzi a mentések sértetlenségét és az RTO teljesülését.",
                MinBiztonsagiOsztaly = BiztonsagiOsztalyok.Alap
            },
            new Kovetelmeny
            {
                KovetelmenyId = 3,
                JogszabalyId = 4,
                KovetelmenyKod = "NIS2-CLD-01",
                Terulet = "Felhőbiztonság és Adatszuverenitás (CLD)",
                Megnevezes = "Előzetes adatosztályozás és felhőszolgáltatások ország-nyilvántartása",
                ElvartAllapotLeiras = "A szervezet elvégezte az adatosztályozást, és nyilvántartja az adatkezelés országát, valamint az igénybe vett felhő- és külső AI-szolgáltatásokat.",
                MinBiztonsagiOsztaly = BiztonsagiOsztalyok.Alap
            },
            new Kovetelmeny
            {
                KovetelmenyId = 4,
                JogszabalyId = 5,
                KovetelmenyKod = "NIS2-RA-01",
                Terulet = "Kockázatértékelés és EIR besorolás (RA)",
                Megnevezes = "EIR leltár, biztonsági osztályba sorolás és éves kockázatelemzés",
                ElvartAllapotLeiras = "A szervezet teljes körű EIR-leltárt vezet, minden rendszert biztonsági osztályba (Alap/Jelentős/Magas) sorol, és legalább évente vezető által jóváhagyott kockázatelemzést végez.",
                MinBiztonsagiOsztaly = BiztonsagiOsztalyok.Alap
            },
            new Kovetelmeny
            {
                KovetelmenyId = 5,
                JogszabalyId = 6,
                KovetelmenyKod = "NIS2-IR-01",
                Terulet = "Incidenskezelés (IR)",
                Megnevezes = "24/72 órás CSIRT incidensbejelentési és válaszadási eljárásrend",
                ElvartAllapotLeiras = "A szervezet dokumentált incidenskezelési tervvel rendelkezik, amely rögzíti az észleléstől számított 24 órás korai figyelmeztetést, a 72 órás bejelentést és a 30 napos zárójelentést.",
                MinBiztonsagiOsztaly = BiztonsagiOsztalyok.Alap
            },
            new Kovetelmeny
            {
                KovetelmenyId = 6,
                JogszabalyId = 7,
                KovetelmenyKod = "NIS2-IA-01",
                Terulet = "Azonosítás és Hitelesítés (IA)",
                Megnevezes = "Többtényezős hitelesítés (MFA) és privilegizált fiókok védelme",
                ElvartAllapotLeiras = "Minden rendszergazdai (privilegizált) fiókhoz és távoli hozzáféréshez kötelezően be van kapcsolva az erős többtényezős hitelesítés (MFA), megosztott adminfiók nem használható.",
                MinBiztonsagiOsztaly = BiztonsagiOsztalyok.Alap
            },
            new Kovetelmeny
            {
                KovetelmenyId = 7,
                JogszabalyId = 8,
                KovetelmenyKod = "NIS2-AU-01",
                Terulet = "Naplózás és Monitoring (AU)",
                Megnevezes = "Sérthetetlen biztonsági eseménynaplózás és központi naplóelemzés",
                ElvartAllapotLeiras = "Az EIR-ek biztonsági eseményeit NTP-szinkronizált időbélyeggel, módosítástól védett módon naplózzák, és a naplókat meghatározott megőrzési idővel központilag elemzik.",
                MinBiztonsagiOsztaly = BiztonsagiOsztalyok.Jelentos
            },
            new Kovetelmeny
            {
                KovetelmenyId = 8,
                JogszabalyId = 9,
                KovetelmenyKod = "NIS2-AT-01",
                Terulet = "Biztonságtudatosság és Képzés (AT)",
                Megnevezes = "Kötelező vezetői és munkavállalói kiberbiztonsági oktatás",
                ElvartAllapotLeiras = "A szervezet belépéskor és legalább évente dokumentált kiberbiztonsági oktatást tart a dolgozóknak és külön a felsővezetésnek, vizsgával vagy jelenléti ívvel igazolva.",
                MinBiztonsagiOsztaly = BiztonsagiOsztalyok.Alap
            },
            new Kovetelmeny
            {
                KovetelmenyId = 9,
                JogszabalyId = 10,
                KovetelmenyKod = "NIS2-SR-01",
                Terulet = "Ellátási lánc biztonsága (SR)",
                Megnevezes = "Beszállítói kockázatértékelés és szerződéses SLA klauzulák",
                ElvartAllapotLeiras = "A kritikus IT és felhős beszállítókról nyilvántartás és kockázatértékelés készül; a szerződések tartalmazzák a NIS2 biztonsági, incidensértesítési és audit-együttműködési záradékokat.",
                MinBiztonsagiOsztaly = BiztonsagiOsztalyok.Alap
            },
            new Kovetelmeny
            {
                KovetelmenyId = 10,
                JogszabalyId = 11,
                KovetelmenyKod = "NIS2-SI-01",
                Terulet = "Rendszer- és információintegritás (SI)",
                Megnevezes = "Sérülékenységmenedzsment és biztonsági frissítések (Patching)",
                ElvartAllapotLeiras = "Dokumentált eljárás határozza meg a kritikus biztonsági frissítések telepítési határidejét, a rendszeres sérülékenységvizsgálatot és a végpontvédelmi (EDR/AV) felügyeletet.",
                MinBiztonsagiOsztaly = BiztonsagiOsztalyok.Alap
            },
            new Kovetelmeny
            {
                KovetelmenyId = 11,
                JogszabalyId = 12,
                KovetelmenyKod = "NIS2-SC-01",
                Terulet = "Rendszer- és kommunikációvédelem (SC)",
                Megnevezes = "Hálózati szegmentáció, adminisztratív leválasztás és titkosítás",
                ElvartAllapotLeiras = "Az adminisztratív felületek nem érhetők el a nyilvános internetről (Zero Public Exposure / VPN), a hálózat szegmentált, az adattovábbítás és adattárolás titkosított (TLS 1.3 / AES-256).",
                MinBiztonsagiOsztaly = BiztonsagiOsztalyok.Jelentos
            });

        modelBuilder.Entity<KontrollEsKerdes>().HasData(
            new KontrollEsKerdes
            {
                KontrollId = 1,
                KovetelmenyId = 1,
                ElvartKontrollLeiras = "Belépő/kilépő jogosultsági munkafolyamat és éves Active Directory jogosultság-felülvizsgálat.",
                ElvartBizonyitekTipus = "Hozzáférés-kezelési szabályzat, AD jogosultságlista, Éves felülvizsgálati jegyzőkönyv",
                FelelosSzerepkor = "IT Vezető / HR / IBF",
                AlapAuditKerdes = "Hogyan történik egy munkavállaló jogosultságainak megadása és megszüntetése kilépéskor?",
                MelyitoAuditKerdes = "Ki kezdeményezi a jogosultság megszüntetését, és milyen határidőn belül hajtja végre az IT?",
                BizonyitekBekeroKerdes = "Kérem, mutassa be az elmúlt 12 hónapban elvégzett jogosultság-felülvizsgálat aláírt jegyzőkönyvét!",
                EllenorzoAuditKerdes = "Található-e az aktív AD-felhasználók között olyan személy, akinek a munkaviszonya már megszűnt?"
            },
            new KontrollEsKerdes
            {
                KontrollId = 2,
                KovetelmenyId = 2,
                ElvartKontrollLeiras = "Automatizált napi/heti 3-2-1 mentés és éves dokumentált visszaállítási tesztelési eljárás.",
                ElvartBizonyitekTipus = "Mentési szabályzat, Mentési naplókivonat, Éves visszaállítási próba jegyzőkönyv",
                FelelosSzerepkor = "IT Vezető / IBF",
                AlapAuditKerdes = "Milyen gyakorisággal és hová történik a kritikus EIR-ek adatainak biztonsági mentése?",
                MelyitoAuditKerdes = "Fizikailag vagy logikailag (air-gap / immutable) el van-e különítve a mentés a zsarolóvírusok ellen?",
                BizonyitekBekeroKerdes = "Kérem, adja át a legutóbbi visszaállítási próba (restore test) dátumozott jegyzőkönyvét!",
                EllenorzoAuditKerdes = "Mennyi időt vett igénybe a tényleges visszaállítás a legutóbbi teszt során a BCP-ben rögzített RTO-hoz képest?"
            },
            new KontrollEsKerdes
            {
                KontrollId = 3,
                KovetelmenyId = 3,
                ElvartKontrollLeiras = "Adatosztályozási eljárás és külső felhő-/AI-szolgáltatások adatkezelési ország-nyilvántartása.",
                ElvartBizonyitekTipus = "Adatosztályozási szabályzat, Felhőszolgáltatói és adatkezelési ország-nyilvántartás",
                FelelosSzerepkor = "IBF / Ügyvezető / Jogi felelős",
                AlapAuditKerdes = "Milyen felhőszolgáltatásokat és külső AI-eszközöket vesz igénybe a szervezet az EIR-ek működtetéséhez?",
                MelyitoAuditKerdes = "Elvégezték-e az előzetes adatosztályozást a felhőbe kerülő adatokra a 2024. évi LXIX. tv. 9. § szerint?",
                BizonyitekBekeroKerdes = "Kérem, mutassa be a 22. § (1) g) pont szerinti adatkezelési helyszín- és ország-nyilvántartást!",
                EllenorzoAuditKerdes = "Történik-e EGT-n kívüli adattárolás vagy maszkolatlan ügyféladat-beküldés nyilvános AI-szolgáltatásba?"
            },
            new KontrollEsKerdes
            {
                KontrollId = 4,
                KovetelmenyId = 4,
                ElvartKontrollLeiras = "EIR-leltár vezetése, 7/2024. MK rendelet szerinti osztályba sorolás és éves kockázatelemzés.",
                ElvartBizonyitekTipus = "EIR-leltár, Biztonsági osztályba sorolási dokumentum, Kockázatelemzési jelentés és Kockázatkezelési terv",
                FelelosSzerepkor = "Ügyvezető / IBF",
                AlapAuditKerdes = "Milyen módszertan alapján sorolták biztonsági osztályba a szervezet elektronikus információs rendszereit?",
                MelyitoAuditKerdes = "Mikor történt a legutóbbi átfogó kockázatelemzés, és hogyan kezelték a feltárt magas kockázatokat?",
                BizonyitekBekeroKerdes = "Kérem, mutassa be a cégvezető által aláírt, hatályos kockázatelemzést és maradékkockázat-elfogadást!",
                EllenorzoAuditKerdes = "Minden, az üzleti folyamatok szempontjából kritikus rendszer szerepel-e az EIR-leltárban?"
            },
            new KontrollEsKerdes
            {
                KontrollId = 5,
                KovetelmenyId = 5,
                ElvartKontrollLeiras = "Incidenskezelési eljárásrend és CSIRT bejelentési munkafolyamat (24 óra / 72 óra / 30 nap).",
                ElvartBizonyitekTipus = "Incidenskezelési szabályzat, Incidensnapló, CSIRT bejelentési sablonok és riasztási lánc",
                FelelosSzerepkor = "IBF / IT Vezető / Ügyvezető",
                AlapAuditKerdes = "Mi a teendő és ki a felelős, ha a szervezet jelentős kiberbiztonsági incidenst észlel?",
                MelyitoAuditKerdes = "Hogyan biztosítják, hogy munkaidőn kívül is teljesüljön a 24 órás korai figyelmeztetési határidő?",
                BizonyitekBekeroKerdes = "Kérem, mutassa be az incidenskezelési szabályzatot és az elmúlt év incidensnaplóját!",
                EllenorzoAuditKerdes = "Rögzítve vannak-e a szabályzatban az NKI / CSIRT pontos elérhetőségei és a 24/72 órás határidők?"
            },
            new KontrollEsKerdes
            {
                KontrollId = 6,
                KovetelmenyId = 6,
                ElvartKontrollLeiras = "Többtényezős hitelesítés (MFA) kikényszerítése és privilegizált hozzáférések egyedi azonosítása.",
                ElvartBizonyitekTipus = "Hitelesítési szabályzat, AD/IdP MFA konfigurációs képernyőkép, Rendszergazdai fióklista",
                FelelosSzerepkor = "IT Vezető / Rendszergazda",
                AlapAuditKerdes = "Mely rendszereken és felhasználói csoportoknál kötelező jelenleg a többtényezős hitelesítés (MFA)?",
                MelyitoAuditKerdes = "Használnak-e a rendszergazdák a napi munkához és az adminisztrációhoz elkülönített fiókokat?",
                BizonyitekBekeroKerdes = "Kérem, mutassa be a címtárban vagy VPN-en beállított MFA-kényszerítési házirendet!",
                EllenorzoAuditKerdes = "Van-e olyan távoli hozzáférés vagy adminfiók, amely egyfaktoros jelszóval is elérhető?"
            },
            new KontrollEsKerdes
            {
                KontrollId = 7,
                KovetelmenyId = 7,
                ElvartKontrollLeiras = "Központi, manipulációtól védett eseménynaplózás, időszinkronizáció és riasztás.",
                ElvartBizonyitekTipus = "Naplózási szabályzat, SIEM/Syslog konfiguráció, Naplómegőrzési és felülvizsgálati bizonyíték",
                FelelosSzerepkor = "IT Vezető / IBF",
                AlapAuditKerdes = "Milyen biztonsági eseményeket naplóznak az EIR-ekben, és mennyi ideig őrzik meg a naplókat?",
                MelyitoAuditKerdes = "Hogyan védik a naplóállományokat attól, hogy egy kompromittált rendszergazda törölje vagy módosítsa őket?",
                BizonyitekBekeroKerdes = "Kérem, mutassa be a központi naplógyűjtő konfigurációját és a legutóbbi naplóelemzési jegyzőkönyvet!",
                EllenorzoAuditKerdes = "Egységes NTP időforrásra vannak-e szinkronizálva a naplózó szerverek és hálózati eszközök?"
            },
            new KontrollEsKerdes
            {
                KontrollId = 8,
                KovetelmenyId = 8,
                ElvartKontrollLeiras = "Éves kiberbiztonsági tudatosító képzés a dolgozóknak és dedikált vezetői felkészítés.",
                ElvartBizonyitekTipus = "Képzési tematika, Oktatási jelenléti ívek / e-learning riportok, Vezetői képzési igazolás",
                FelelosSzerepkor = "IBF / HR Vezető / Ügyvezető",
                AlapAuditKerdes = "Milyen rendszerességgel részesülnek a munkavállalók és a vezetők kiberbiztonsági oktatásban?",
                MelyitoAuditKerdes = "Hogyan pótolják az oktatást az újonnan belépő vagy tartósan távol lévő dolgozók esetében?",
                BizonyitekBekeroKerdes = "Kérem, adja át az idei év kiberbiztonsági oktatásának tematikáját és a részvételi arányt igazoló listát!",
                EllenorzoAuditKerdes = "A szervezet ügyvezetője részt vett-e a törvény által előírt vezetői kiberbiztonsági képzésen?"
            },
            new KontrollEsKerdes
            {
                KontrollId = 9,
                KovetelmenyId = 9,
                ElvartKontrollLeiras = "Kritikus beszállítók nyilvántartása, kockázatértékelése és szerződéses kiberbiztonsági záradékok.",
                ElvartBizonyitekTipus = "Beszállítói nyilvántartás és kockázatértékelés, Mintaszerződés / SLA kiberbiztonsági melléklet",
                FelelosSzerepkor = "Beszerzési Vezető / IBF / Jogi felelős",
                AlapAuditKerdes = "Hogyan mérlegelik a kiberbiztonsági szempontokat egy új IT-beszállító kiválasztásakor?",
                MelyitoAuditKerdes = "Milyen határidővel köteles a beszállító értesíteni Önöket egy nála bekövetkezett biztonsági incidensről?",
                BizonyitekBekeroKerdes = "Kérem, mutassa be a kritikus beszállítók listáját és egy hatályos szerződés biztonsági mellékletét!",
                EllenorzoAuditKerdes = "Felülvizsgálják-e évente a kritikus beszállítók megfelelőségét és SLA-teljesítését?"
            },
            new KontrollEsKerdes
            {
                KontrollId = 10,
                KovetelmenyId = 10,
                ElvartKontrollLeiras = "Sérülékenységvizsgálat, kockázatalapú hibajavítás (patch management) és végpontvédelem.",
                ElvartBizonyitekTipus = "Sérülékenység- és frissítéskezelési szabályzat, Patch-állapot riport, Sérülékenységvizsgálati jegyzőkönyv",
                FelelosSzerepkor = "IT Vezető / Rendszergazda",
                AlapAuditKerdes = "Milyen határidőn belül telepítik a kritikus biztonsági frissítéseket a szerverekre és munkaállomásokra?",
                MelyitoAuditKerdes = "Hogyan kezelik azokat a rendszereket (pl. ipari/legacy gépek), amelyekre már nem érhető el gyártói frissítés?",
                BizonyitekBekeroKerdes = "Kérem, mutassa be a legutóbbi sérülékenységvizsgálat eredményét és a frissítési státuszriportot!",
                EllenorzoAuditKerdes = "Üzemel-e a hálózatban támogatásból kifutott (EOL) operációs rendszer kompenzáló kontroll nélkül?"
            },
            new KontrollEsKerdes
            {
                KontrollId = 11,
                KovetelmenyId = 11,
                ElvartKontrollLeiras = "Hálózati szegmentáció, adminisztrációs felületek zárt elérése (Zero Public Exposure) és titkosítás.",
                ElvartBizonyitekTipus = "Hálózati topológia és tűzfalszabályzat, Kriptográfiai szabályzat, Külső port-scan bizonyíték",
                FelelosSzerepkor = "IT Vezető / Hálózati rendszergazda",
                AlapAuditKerdes = "Hogyan vannak elkülönítve a publikus szolgáltatások a belső vállalati és adminisztratív hálózattól?",
                MelyitoAuditKerdes = "Elérhető-e bármilyen menedzsment-felület (RDP, SSH, admin portál) közvetlenül az internetről VPN nélkül?",
                BizonyitekBekeroKerdes = "Kérem, mutassa be a hálózati szegmentációs ábrát és a tűzfal adminisztratív hozzáférési szabályait!",
                EllenorzoAuditKerdes = "Alkalmaznak-e titkosítást a hordozható adathordozókon és a biztonsági mentéseken?"
            });
    }
}
