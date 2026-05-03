using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Mythra.Domain.Libraries;

namespace Mythra.Infrastructure.Persistence.Configurations;

public sealed class LibraryConfiguration : IEntityTypeConfiguration<Library>
{
    public void Configure(EntityTypeBuilder<Library> b)
    {
        b.ToTable("libraries");
        b.HasKey(l => l.Id);
        b.Property(l => l.Name).HasMaxLength(80).IsRequired();
        b.HasIndex(l => l.Name).IsUnique();
        b.Property(l => l.Description).HasMaxLength(1024);
        b.Property(l => l.PreferredLanguage).HasMaxLength(10);
        b.Property(l => l.PreferredMetadataProvider).HasMaxLength(64);
        b.Property(l => l.Kind).HasConversion<int>();
        b.Property(l => l.IsSystem).HasDefaultValue(false);
        b.HasMany(l => l.Folders).WithOne().HasForeignKey(f => f.LibraryId).OnDelete(DeleteBehavior.Cascade);
        b.Ignore(l => l.DomainEvents);
    }
}

public sealed class LibraryFolderConfiguration : IEntityTypeConfiguration<LibraryFolder>
{
    public void Configure(EntityTypeBuilder<LibraryFolder> b)
    {
        b.ToTable("library_folders");
        b.HasKey(f => f.Id);
        b.Property(f => f.Path).HasMaxLength(1024).IsRequired();
        b.HasIndex(f => new { f.LibraryId, f.Path }).IsUnique();
    }
}
