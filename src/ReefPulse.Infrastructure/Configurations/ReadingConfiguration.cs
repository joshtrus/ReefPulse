using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ReefPulse.Domain;

namespace ReefPulse.Infrastructure.Configurations;

internal sealed class ReadingConfiguration : IEntityTypeConfiguration<Reading>
{
    public void Configure(EntityTypeBuilder<Reading> builder)
    {
        builder.ToTable("readings");
        builder.HasKey(r => r.Id);

        // Store the enum as its name rather than an int: the raw table stays legible and
        // adding new metric types later can't silently reshuffle existing numeric values.
        builder.Property(r => r.Metric)
            .HasConversion<string>()
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(r => r.Source).HasMaxLength(100).IsRequired();

        // Primary read pattern is "readings for a site, most recent first", so index on
        // (site, observed time). This is the query the dashboard and anomaly scan will run.
        builder.HasIndex(r => new { r.ReefSiteId, r.ObservedAt });
    }
}
