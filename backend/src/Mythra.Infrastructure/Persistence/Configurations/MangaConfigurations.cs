using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Mythra.Domain.Media.Manga;

namespace Mythra.Infrastructure.Persistence.Configurations;

public sealed class MangaItemConfiguration : IEntityTypeConfiguration<MangaItem>
{
    public void Configure(EntityTypeBuilder<MangaItem> b)
    {
        b.Property(m => m.Author).HasMaxLength(256);
        b.Property(m => m.Artist).HasMaxLength(256);
        b.Property(m => m.Status).HasMaxLength(32);
        b.Property(m => m.RootPath).HasMaxLength(1024);
        b.Property(m => m.ReadingDirection).HasConversion<int>();
        b.HasMany(m => m.Chapters).WithOne().HasForeignKey(c => c.MangaItemId).OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class MangaChapterConfiguration : IEntityTypeConfiguration<MangaChapter>
{
    public void Configure(EntityTypeBuilder<MangaChapter> b)
    {
        b.ToTable("manga_chapters");
        b.HasKey(c => c.Id);
        b.Property(c => c.Title).HasMaxLength(256);
        b.Property(c => c.ArchivePath).HasMaxLength(1024).IsRequired();
        b.Property(c => c.ArchiveFormat).HasMaxLength(8);
        b.Property(c => c.CoverPath).HasMaxLength(1024);
        b.HasIndex(c => new { c.MangaItemId, c.VolumeNumber, c.ChapterNumber });
    }
}
