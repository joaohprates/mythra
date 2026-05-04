using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Mythra.Domain.Media;
using Mythra.Domain.Media.Books;
using Mythra.Domain.Media.Manga;
using Mythra.Domain.Media.Video;

namespace Mythra.Infrastructure.Persistence.Configurations;

public sealed class MediaItemConfiguration : IEntityTypeConfiguration<MediaItem>
{
    public void Configure(EntityTypeBuilder<MediaItem> b)
    {
        b.ToTable("media_items");
        b.HasKey(m => m.Id);
        b.Property(m => m.Title).HasMaxLength(512).IsRequired();
        b.Property(m => m.OriginalTitle).HasMaxLength(512);
        b.Property(m => m.SortTitle).HasMaxLength(512);
        b.Property(m => m.Overview).HasMaxLength(8000);
        b.Property(m => m.Tagline).HasMaxLength(512);
        b.Property(m => m.PosterPath).HasMaxLength(1024);
        b.Property(m => m.BackdropPath).HasMaxLength(1024);
        b.Property(m => m.ThumbPath).HasMaxLength(1024);
        b.Property(m => m.Language).HasMaxLength(10);
        b.Property(m => m.CountryCode).HasMaxLength(8);
        b.Property(m => m.ProviderTmdbId).HasMaxLength(64);
        b.Property(m => m.ProviderImdbId).HasMaxLength(64);
        b.Property(m => m.ProviderAnilistId).HasMaxLength(64);
        b.Property(m => m.ProviderMalId).HasMaxLength(64);
        b.Property(m => m.ProviderMusicbrainzId).HasMaxLength(64);
        b.Property(m => m.ProviderGoogleBooksId).HasMaxLength(64);

        b.Ignore(m => m.Year);
        b.Ignore(m => m.Kind);
        b.Ignore(m => m.DomainEvents);

        b.HasIndex(m => m.LibraryId);
        b.HasIndex(m => m.Title);
        b.HasIndex(m => m.ProviderTmdbId);

        b.HasMany(m => m.Genres).WithMany().UsingEntity("media_item_genres");
        b.HasMany(m => m.Tags).WithMany().UsingEntity("media_item_tags");
        b.HasMany(m => m.People).WithOne().HasForeignKey(p => p.MediaItemId).OnDelete(DeleteBehavior.Cascade);

        b.HasDiscriminator<int>("MediaKind")
            .HasValue<VideoItem>((int)MediaKind.Video)
            .HasValue<MangaItem>((int)MediaKind.Manga)
            .HasValue<BookItem>((int)MediaKind.Book);
    }
}

public sealed class GenreConfiguration : IEntityTypeConfiguration<Genre>
{
    public void Configure(EntityTypeBuilder<Genre> b)
    {
        b.ToTable("genres");
        b.HasKey(g => g.Id);
        b.Property(g => g.Name).HasMaxLength(64).IsRequired();
        b.Property(g => g.Slug).HasMaxLength(64).IsRequired();
        b.Property(g => g.Kind).HasConversion<int?>();
        b.HasIndex(g => g.Slug).IsUnique();
    }
}

public sealed class TagConfiguration : IEntityTypeConfiguration<Tag>
{
    public void Configure(EntityTypeBuilder<Tag> b)
    {
        b.ToTable("tags");
        b.HasKey(t => t.Id);
        b.Property(t => t.Name).HasMaxLength(64).IsRequired();
        b.Property(t => t.Slug).HasMaxLength(64).IsRequired();
        b.HasIndex(t => t.Slug).IsUnique();
    }
}

public sealed class PersonConfiguration : IEntityTypeConfiguration<Person>
{
    public void Configure(EntityTypeBuilder<Person> b)
    {
        b.ToTable("people");
        b.HasKey(p => p.Id);
        b.Property(p => p.Name).HasMaxLength(256).IsRequired();
        b.Property(p => p.Biography).HasMaxLength(8000);
        b.Property(p => p.PhotoPath).HasMaxLength(1024);
        b.Property(p => p.ProviderTmdbId).HasMaxLength(64);
        b.HasIndex(p => p.Name);
    }
}

public sealed class MediaPersonRoleConfiguration : IEntityTypeConfiguration<MediaPersonRole>
{
    public void Configure(EntityTypeBuilder<MediaPersonRole> b)
    {
        b.ToTable("media_person_roles");
        b.HasKey(r => r.Id);
        b.Property(r => r.Role).HasConversion<int>();
        b.Property(r => r.Character).HasMaxLength(256);
        b.HasOne(r => r.Person).WithMany().HasForeignKey(r => r.PersonId);
        b.HasIndex(r => new { r.MediaItemId, r.PersonId, r.Role }).IsUnique();
    }
}
