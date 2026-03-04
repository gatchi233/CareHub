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
    public DbSet<MarEntry> MarEntries => Set<MarEntry>();
    public DbSet<MedicationInventoryLedger> MedicationInventoryLedgers => Set<MedicationInventoryLedger>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Resident>().HasKey(x => x.Id);
        modelBuilder.Entity<Medication>().HasKey(x => x.Id);
        modelBuilder.Entity<Observation>().HasKey(x => x.Id);

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

        // AppUser
        modelBuilder.Entity<AppUser>().HasKey(x => x.Id);
        modelBuilder.Entity<AppUser>()
            .HasIndex(x => x.Username)
            .IsUnique();

        // MarEntry
        modelBuilder.Entity<MarEntry>().HasKey(x => x.Id);

        modelBuilder.Entity<MarEntry>()
            .HasIndex(m => m.ClientRequestId)
            .IsUnique();

        modelBuilder.Entity<MarEntry>()
            .HasOne(m => m.Resident)
            .WithMany()
            .HasForeignKey(m => m.ResidentId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<MarEntry>()
            .HasOne(m => m.Medication)
            .WithMany()
            .HasForeignKey(m => m.MedicationId)
            .OnDelete(DeleteBehavior.Restrict);

        // MedicationInventoryLedger
        modelBuilder.Entity<MedicationInventoryLedger>().HasKey(x => x.Id);

        modelBuilder.Entity<MedicationInventoryLedger>()
            .HasIndex(l => l.MarEntryId)
            .IsUnique();

        modelBuilder.Entity<MedicationInventoryLedger>()
            .HasOne(l => l.Medication)
            .WithMany()
            .HasForeignKey(l => l.MedicationId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<MedicationInventoryLedger>()
            .HasOne(l => l.MarEntry)
            .WithMany()
            .HasForeignKey(l => l.MarEntryId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
