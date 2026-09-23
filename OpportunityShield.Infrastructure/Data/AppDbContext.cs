using Microsoft.EntityFrameworkCore;
using Oppurtunityshield.Domain.Entities;

namespace Oppurtunityshield.Infrastructure.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Analysis> Analyses => Set<Analysis>();
    public DbSet<AnalysisResult> AnalysisResults => Set<AnalysisResult>();
    public DbSet<Signal> Signals => Set<Signal>();
    public DbSet<Evidence> EvidenceItems => Set<Evidence>();
    public DbSet<Feedback> Feedbacks => Set<Feedback>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Analysis>(entity =>
        {
            entity.HasIndex(a => a.SessionId);
            entity.HasIndex(a => a.CreatedAt);

            entity.Property(a => a.Content).HasColumnType("text");

            // Requires EF Core 8+ and Npgsql.EntityFrameworkCore.PostgreSQL 8+.
            // Stored as a jsonb column — this is transient progress UX state,
            // not something we need to query relationally.
            entity.OwnsMany(a => a.ProgressSteps, steps => steps.ToJson());

            entity.HasOne(a => a.Result)
                  .WithOne(r => r.Analysis)
                  .HasForeignKey<AnalysisResult>(r => r.AnalysisId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(a => a.Feedback)
                  .WithOne(f => f.Analysis)
                  .HasForeignKey<Feedback>(f => f.AnalysisId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<AnalysisResult>(entity =>
        {
            entity.Property(r => r.Summary).HasColumnType("text");
            entity.Property(r => r.Recommendation).HasColumnType("text");

            entity.HasMany(r => r.Signals)
                  .WithOne(s => s.AnalysisResult)
                  .HasForeignKey(s => s.AnalysisResultId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(r => r.EvidenceItems)
                  .WithOne(e => e.AnalysisResult)
                  .HasForeignKey(e => e.AnalysisResultId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Signal>(entity =>
        {
            entity.Property(s => s.Description).HasColumnType("text");
        });

        modelBuilder.Entity<Evidence>(entity =>
        {
            entity.Property(e => e.Detail).HasColumnType("text");
        });
    }
}
