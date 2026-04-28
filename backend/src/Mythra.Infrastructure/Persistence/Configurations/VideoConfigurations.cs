using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Mythra.Domain.Media.Video;

namespace Mythra.Infrastructure.Persistence.Configurations;

public sealed class VideoItemConfiguration : IEntityTypeConfiguration<VideoItem>
{
    public void Configure(EntityTypeBuilder<VideoItem> b)
    {
        b.Property(v => v.VideoKind).HasConversion<int>();
        b.Property(v => v.FilePath).HasMaxLength(1024);
        b.Property(v => v.Container).HasMaxLength(32);
        b.Property(v => v.VideoCodec).HasMaxLength(32);
        b.Property(v => v.AudioCodec).HasMaxLength(32);
        b.Ignore(v => v.ResolutionLabel);

        b.HasMany(v => v.Subtitles).WithOne().HasForeignKey(s => s.VideoItemId).OnDelete(DeleteBehavior.Cascade);
        b.HasMany(v => v.AudioTracks).WithOne().HasForeignKey(a => a.VideoItemId).OnDelete(DeleteBehavior.Cascade);
        b.HasMany(v => v.ChapterMarkers).WithOne().HasForeignKey(c => c.VideoItemId).OnDelete(DeleteBehavior.Cascade);

        b.HasIndex(v => v.FilePath);
        b.HasIndex(v => new { v.ParentId, v.SeasonNumber, v.EpisodeNumber });
    }
}

public sealed class SubtitleConfiguration : IEntityTypeConfiguration<Subtitle>
{
    public void Configure(EntityTypeBuilder<Subtitle> b)
    {
        b.ToTable("subtitles");
        b.HasKey(s => s.Id);
        b.Property(s => s.LanguageCode).HasMaxLength(10).IsRequired();
        b.Property(s => s.DisplayName).HasMaxLength(128);
        b.Property(s => s.FilePath).HasMaxLength(1024);
        b.Property(s => s.Format).HasMaxLength(16);
        b.Property(s => s.Kind).HasConversion<int>();
    }
}

public sealed class AudioTrackConfiguration : IEntityTypeConfiguration<AudioTrack>
{
    public void Configure(EntityTypeBuilder<AudioTrack> b)
    {
        b.ToTable("audio_tracks");
        b.HasKey(a => a.Id);
        b.Property(a => a.LanguageCode).HasMaxLength(10).IsRequired();
        b.Property(a => a.DisplayName).HasMaxLength(128);
        b.Property(a => a.Codec).HasMaxLength(32).IsRequired();
        b.Property(a => a.ChannelLayout).HasMaxLength(32);
    }
}

public sealed class ChapterMarkerConfiguration : IEntityTypeConfiguration<ChapterMarker>
{
    public void Configure(EntityTypeBuilder<ChapterMarker> b)
    {
        b.ToTable("chapter_markers");
        b.HasKey(c => c.Id);
        b.Property(c => c.Kind).HasConversion<int>();
        b.Property(c => c.Label).HasMaxLength(128);
        b.Property(c => c.ThumbPath).HasMaxLength(1024);
    }
}
