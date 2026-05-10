using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Mythra.Domain.Playlists;

namespace Mythra.Infrastructure.Persistence.Configurations;

public sealed class PlaylistConfiguration : IEntityTypeConfiguration<Playlist>
{
    public void Configure(EntityTypeBuilder<Playlist> b)
    {
        b.ToTable("playlists");
        b.HasKey(p => p.Id);
        b.Property(p => p.Name).HasMaxLength(256).IsRequired();
        b.Property(p => p.Description).HasMaxLength(2048);
        b.Property(p => p.CoverImagePath).HasMaxLength(512);
        b.HasIndex(p => p.ProfileId);
        b.Ignore(p => p.DomainEvents);
        b.HasMany(p => p.Items)
         .WithOne()
         .HasForeignKey(i => i.PlaylistId)
         .OnDelete(DeleteBehavior.Cascade);
        b.Navigation(p => p.Items)
         .HasField("_items")
         .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

public sealed class PlaylistItemConfiguration : IEntityTypeConfiguration<PlaylistItem>
{
    public void Configure(EntityTypeBuilder<PlaylistItem> b)
    {
        b.ToTable("playlist_items");
        b.HasKey(i => i.Id);
        b.HasIndex(i => new { i.PlaylistId, i.Order });
        b.HasIndex(i => new { i.PlaylistId, i.MediaItemId }).IsUnique();
    }
}
