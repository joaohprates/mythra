using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Mythra.Domain.Favorites;

namespace Mythra.Infrastructure.Persistence.Configurations;

public sealed class FavoriteItemConfiguration : IEntityTypeConfiguration<FavoriteItem>
{
    public void Configure(EntityTypeBuilder<FavoriteItem> b)
    {
        b.ToTable("favorite_items");
        b.HasKey(f => f.Id);
        b.HasIndex(f => new { f.ProfileId, f.MediaItemId }).IsUnique();
        b.HasIndex(f => f.ProfileId);
    }
}
