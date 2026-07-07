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
        
        builder.Property(r => r.Metric)
            .HasConversion<string>()
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(r => r.Source).HasMaxLength(100).IsRequired();
        
        builder.HasIndex(r => new { r.ReefSiteId, r.ObservedAt });
    }
}
