using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ReefPulse.Domain;

namespace ReefPulse.Infrastructure.Configurations;

internal sealed class ReefSiteConfiguration : IEntityTypeConfiguration<ReefSite>
{
    public void Configure(EntityTypeBuilder<ReefSite> builder)
    {
        builder.ToTable("reef_sites");
        builder.HasKey(s => s.Id);

        builder.Property(s => s.Name).HasMaxLength(200).IsRequired();
        builder.Property(s => s.Region).HasMaxLength(200);
        builder.HasIndex(s => s.Name).IsUnique();

        builder.HasMany(s => s.Readings)
            .WithOne(r => r.ReefSite!)
            .HasForeignKey(r => r.ReefSiteId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
