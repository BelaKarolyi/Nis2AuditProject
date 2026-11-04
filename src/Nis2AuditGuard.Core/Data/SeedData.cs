using Microsoft.EntityFrameworkCore;

namespace Nis2AuditGuard.Core.Data;

/// <summary>Kezdeti tudásbázis (ideiglenes váz – a végleges seed a törvényszöveg alapján készül).</summary>
public static class SeedData
{
    /// <summary>Seed adatok regisztrálása.</summary>
    /// <param name="modelBuilder">EF Core model builder.</param>
    public static void Apply(ModelBuilder modelBuilder) => _ = modelBuilder;
}

