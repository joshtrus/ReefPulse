using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ReefPulse.Domain;

namespace ReefPulse.Infrastructure.Configurations;

internal sealed class AlertConfiguration : IEntityTypeConfiguration<Alert>
{
    public void Configure(EntityTypeBuilder<Alert> builder)
    {
        builder.ToTable("alerts");
        builder.HasKey(a => a.Id);

        builder.Property(a => a.Metric)
            .HasConversion<string>()
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(a => a.Status)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.HasOne(a => a.ReefSite)
            .WithMany()
            .HasForeignKey(a => a.ReefSiteId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(a => new { a.ReefSiteId, a.Status });
    }
}
