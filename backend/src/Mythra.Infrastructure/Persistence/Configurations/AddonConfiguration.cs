using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Mythra.Domain.Addons;

namespace Mythra.Infrastructure.Persistence.Configurations;

public sealed class AddonConfiguration : IEntityTypeConfiguration<Addon>
{
    public void Configure(EntityTypeBuilder<Addon> b)
    {
        b.ToTable("addons");
        b.HasKey(a => a.Id);
        b.Property(a => a.Name).HasMaxLength(128).IsRequired();
        b.Property(a => a.Description).HasMaxLength(1024);
        b.Property(a => a.IconUrl).HasMaxLength(512);
        b.Property(a => a.TargetMediaKind).HasMaxLength(32).IsRequired();
        b.Property(a => a.ProviderType).HasMaxLength(64).IsRequired();
        b.Property(a => a.ProviderConfigJson).IsRequired();
        b.Property(a => a.SecretsJson);
        b.Property(a => a.Kind).HasConversion<int>();
        b.Property(a => a.Status).HasConversion<int>();
        b.Property(a => a.SourceChecksum).HasMaxLength(128).IsRequired();
        b.Property(a => a.ImportedFrom).HasMaxLength(256);
        b.HasIndex(a => a.UserId);
    }
}
