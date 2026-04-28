using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Mythra.Domain.Media.Audio;

namespace Mythra.Infrastructure.Persistence.Configurations;

public sealed class AudioItemConfiguration : IEntityTypeConfiguration<AudioItem>
{
    public void Configure(EntityTypeBuilder<AudioItem> b)
    {
        b.Property(a => a.Author).HasMaxLength(256);
        b.Property(a => a.Narrator).HasMaxLength(256);
        b.Property(a => a.Series).HasMaxLength(256);
        b.Property(a => a.AudioKind).HasConversion<int>();
        b.Property(a => a.RootPath).HasMaxLength(1024);
        b.Property(a => a.CoverPath).HasMaxLength(1024);
        b.HasMany(a => a.Chapters).WithOne().HasForeignKey(c => c.AudioItemId).OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class AudioChapterConfiguration : IEntityTypeConfiguration<AudioChapter>
{
    public void Configure(EntityTypeBuilder<AudioChapter> b)
    {
        b.ToTable("audio_chapters");
        b.HasKey(c => c.Id);
        b.Property(c => c.Title).HasMaxLength(256).IsRequired();
        b.Property(c => c.FilePath).HasMaxLength(1024).IsRequired();
        b.Property(c => c.Codec).HasMaxLength(32);
    }
}
