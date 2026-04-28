using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Mythra.Domain.Progress;

namespace Mythra.Infrastructure.Persistence.Configurations;

public sealed class PlaybackProgressConfiguration : IEntityTypeConfiguration<PlaybackProgress>
{
    public void Configure(EntityTypeBuilder<PlaybackProgress> b)
    {
        b.ToTable("playback_progress");
        b.HasKey(p => p.Id);
        b.HasIndex(p => new { p.ProfileId, p.MediaItemId }).IsUnique();
        b.HasIndex(p => p.LastWatchedAt);
        b.Ignore(p => p.PercentComplete);
    }
}

public sealed class ReadingProgressConfiguration : IEntityTypeConfiguration<ReadingProgress>
{
    public void Configure(EntityTypeBuilder<ReadingProgress> b)
    {
        b.ToTable("reading_progress");
        b.HasKey(r => r.Id);
        b.HasIndex(r => new { r.ProfileId, r.MediaItemId }).IsUnique();
        b.HasIndex(r => r.LastReadAt);
        b.Property(r => r.CfiLocator).HasMaxLength(512);
    }
}

public sealed class BookmarkConfiguration : IEntityTypeConfiguration<Bookmark>
{
    public void Configure(EntityTypeBuilder<Bookmark> b)
    {
        b.ToTable("bookmarks");
        b.HasKey(x => x.Id);
        b.Property(x => x.Label).HasMaxLength(128);
        b.Property(x => x.Note).HasMaxLength(2000);
        b.Property(x => x.CfiLocator).HasMaxLength(512);
        b.HasIndex(x => new { x.ProfileId, x.MediaItemId });
    }
}

public sealed class HighlightConfiguration : IEntityTypeConfiguration<Highlight>
{
    public void Configure(EntityTypeBuilder<Highlight> b)
    {
        b.ToTable("highlights");
        b.HasKey(x => x.Id);
        b.Property(x => x.Text).HasMaxLength(4000).IsRequired();
        b.Property(x => x.Note).HasMaxLength(2000);
        b.Property(x => x.Color).HasMaxLength(16);
        b.Property(x => x.CfiStart).HasMaxLength(512);
        b.Property(x => x.CfiEnd).HasMaxLength(512);
        b.HasIndex(x => new { x.ProfileId, x.MediaItemId });
    }
}
