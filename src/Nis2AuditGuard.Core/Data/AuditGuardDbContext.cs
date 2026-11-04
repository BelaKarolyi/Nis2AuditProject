using Microsoft.EntityFrameworkCore;
using Nis2AuditGuard.Core.Domain;

namespace Nis2AuditGuard.Core.Data;

/// <summary>
/// Entity Framework Core adatbázis-kontextus a 7 táblás NIS2-AuditGuard sémához.
/// Minden lekérdezés LINQ-alapú, ezért automatikusan paraméterezett (SQL Injection elleni védelem).
/// </summary>
public class AuditGuardDbContext(DbContextOptions<AuditGuardDbContext> options) : DbContext(options)
{
    /// <summary>1. Jogszabályhelyek.</summary>
    public DbSet<Jogszabaly> Jogszabalyok => Set<Jogszabaly>();

    /// <summary>2. Követelmények.</summary>
    public DbSet<Kovetelmeny> Kovetelmenyek => Set<Kovetelmeny>();

    /// <summary>3. Kontrollok és auditkérdések.</summary>
    public DbSet<KontrollEsKerdes> KontrollokEsKerdesek => Set<KontrollEsKerdes>();

    /// <summary>4. Ügyfelek és EIR-ek.</summary>
    public DbSet<UgyfelEsEir> Ugyfelek => Set<UgyfelEsEir>();

    /// <summary>5. Feltöltött bizonyítékok.</summary>
    public DbSet<FeltoltottBizonyitek> Bizonyitekok => Set<FeltoltottBizonyitek>();

    /// <summary>6. AI-értékelések.</summary>
    public DbSet<AiErtekelesGap> Ertekelesek => Set<AiErtekelesGap>();

    /// <summary>7. Esemény- és AI-napló.</summary>
    public DbSet<AuditEsAiNaplo> Naplo => Set<AuditEsAiNaplo>();

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Jogszabaly>(e =>
        {
            e.ToTable("Jogszabalyok");
            e.HasKey(x => x.JogszabalyId);
            e.Property(x => x.Kod).HasMaxLength(50).IsRequired();
            e.Property(x => x.Cim).HasMaxLength(300).IsRequired();
            e.Property(x => x.ParagrafusSzakasz).HasMaxLength(100).IsRequired();
            e.Property(x => x.HatalyosSzoveg).IsRequired();
        });

        modelBuilder.Entity<Kovetelmeny>(e =>
        {
            e.ToTable("Kovetelmenyek", t => t.HasCheckConstraint(
                "CK_Kovetelmenyek_MinBiztonsagiOsztaly", "MinBiztonsagiOsztaly IN ('Alap','Jelentos','Magas')"));
            e.HasKey(x => x.KovetelmenyId);
            e.HasIndex(x => x.KovetelmenyKod).IsUnique();
            e.Property(x => x.KovetelmenyKod).HasMaxLength(50).IsRequired();
            e.Property(x => x.Terulet).HasMaxLength(150).IsRequired();
            e.Property(x => x.Megnevezes).HasMaxLength(250).IsRequired();
            e.Property(x => x.MinBiztonsagiOsztaly).HasMaxLength(20).IsRequired();
            e.HasOne(x => x.Jogszabaly).WithMany(j => j.Kovetelmenyek)
                .HasForeignKey(x => x.JogszabalyId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<KontrollEsKerdes>(e =>
        {
            e.ToTable("Kontrollok_Es_Kerdesek");
            e.HasKey(x => x.KontrollId);
            e.Property(x => x.ElvartBizonyitekTipus).HasMaxLength(250).IsRequired();
            e.Property(x => x.FelelosSzerepkor).HasMaxLength(100).IsRequired();
            e.Property(x => x.AlapAuditKerdes).HasMaxLength(500).IsRequired();
            e.Property(x => x.MelyitoAuditKerdes).HasMaxLength(500).IsRequired();
            e.Property(x => x.BizonyitekBekeroKerdes).HasMaxLength(500).IsRequired();
            e.Property(x => x.EllenorzoAuditKerdes).HasMaxLength(500).IsRequired();
            e.HasOne(x => x.Kovetelmeny).WithMany(k => k.Kontrollok)
                .HasForeignKey(x => x.KovetelmenyId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<UgyfelEsEir>(e =>
        {
            e.ToTable("Ugyfelek_Es_EIR", t => t.HasCheckConstraint(
                "CK_Ugyfelek_BiztonsagiOsztaly", "BiztonsagiOsztaly IN ('Alap','Jelentos','Magas')"));
            e.HasKey(x => x.UgyfelId);
            e.Property(x => x.CegNev).HasMaxLength(200).IsRequired();
            e.Property(x => x.Agazat).HasMaxLength(150).IsRequired();
            e.Property(x => x.MellekletBesorolas).HasMaxLength(50).IsRequired();
            e.Property(x => x.EvesNettoArbevetelMillioEur).HasPrecision(10, 2);
            e.Property(x => x.EirMegnevezes).HasMaxLength(200).IsRequired();
            e.Property(x => x.BiztonsagiOsztaly).HasMaxLength(20).IsRequired();
            e.Property(x => x.AdatkezelesOrszaga).HasMaxLength(100).IsRequired();
            e.Property(x => x.EloszuresEredmeny).HasMaxLength(200).IsRequired();
            e.Property(x => x.BecsultIranyarHuf).HasPrecision(12, 0);
        });

        modelBuilder.Entity<FeltoltottBizonyitek>(e =>
        {
            e.ToTable("Feltoltott_Bizonyitekok");
            e.HasKey(x => x.BizonyitekId);
            e.Property(x => x.DokumentumNev).HasMaxLength(255).IsRequired();
            e.Property(x => x.DokumentumTipus).HasMaxLength(100).IsRequired();
            e.Property(x => x.KinyertSzovegIndexelt).IsRequired();
            e.Property(x => x.Sha256Hash).HasMaxLength(64).IsFixedLength().IsRequired();
            e.HasOne(x => x.Ugyfel).WithMany(u => u.Bizonyitekok)
                .HasForeignKey(x => x.UgyfelId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<AiErtekelesGap>(e =>
        {
            e.ToTable("AI_Ertekelesek_Gap", t => t.HasCheckConstraint(
                "CK_Ertekelesek_Megbizhatosag", "MegbizhatosagSzazalek BETWEEN 0 AND 100"));
            e.HasKey(x => x.ErtekelesId);
            e.Property(x => x.ForrasOldalEsSzakasz).HasMaxLength(250).IsRequired();
            e.Property(x => x.KapcsolodoJogszabalyHivatkozas).HasMaxLength(250).IsRequired();
            e.Property(x => x.HasznaltAiMotor).HasMaxLength(100).IsRequired();
            e.Property(x => x.SzakertoiStatusz).HasMaxLength(60).IsRequired();
            e.HasOne(x => x.Kovetelmeny).WithMany()
                .HasForeignKey(x => x.KovetelmenyId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Bizonyitek).WithMany()
                .HasForeignKey(x => x.BizonyitekId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne<UgyfelEsEir>().WithMany()
                .HasForeignKey(x => x.UgyfelId).OnDelete(DeleteBehavior.NoAction);
        });

        modelBuilder.Entity<AuditEsAiNaplo>(e =>
        {
            e.ToTable("Audit_Es_AI_Naplo");
            e.HasKey(x => x.NaploId);
            e.Property(x => x.FelhasznaloAzonosito).HasMaxLength(100).IsRequired();
            e.Property(x => x.MuveletTipus).HasMaxLength(100).IsRequired();
            e.Property(x => x.HasznaltModell).HasMaxLength(100).IsRequired();
            e.Property(x => x.KliensIpHash).HasMaxLength(64).IsRequired();
            e.HasIndex(x => x.IdopontUtc);
        });

        SeedData.Apply(modelBuilder);
    }
}

