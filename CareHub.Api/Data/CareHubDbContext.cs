using CareHub.Api.Entities;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Reflection.Emit;

namespace CareHub.Api.Data;

public sealed class CareHubDbContext : DbContext
{
    public CareHubDbContext(DbContextOptions<CareHubDbContext> options) : base(options) { }

    public DbSet<Resident> Residents => Set<Resident>();
    public DbSet<Medication> Medications => Set<Medication>();
    public DbSet<Observation> Observations => Set<Observation>();
    public DbSet<AppUser> AppUsers => Set<AppUser>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Keep it simple for M0; expand constraints later.
        modelBuilder.Entity<Resident>().HasKey(x => x.Id);
        modelBuilder.Entity<Medication>().HasKey(x => x.Id);
        modelBuilder.Entity<Observation>().HasKey(x => x.Id);
        modelBuilder.Entity<AppUser>().HasKey(x => x.Username);

        // Optional relationship if your JSON already maps meds to residents:
        modelBuilder.Entity<Medication>()
            .HasOne<Resident>()
            .WithMany()
            .HasForeignKey(x => x.ResidentId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Observation>()
            .HasOne<Resident>()
            .WithMany()
            .HasForeignKey(x => x.ResidentId)
            .OnDelete(DeleteBehavior.Cascade);

        // Backward-compatible mapping: database column is RecordedAtUtc.
        modelBuilder.Entity<Observation>()
            .Property(x => x.RecordedAt)
            .HasColumnName("RecordedAtUtc");
    }
}
