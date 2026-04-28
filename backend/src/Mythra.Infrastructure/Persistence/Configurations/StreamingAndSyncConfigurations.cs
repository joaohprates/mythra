using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Mythra.Domain.Streaming;
using Mythra.Domain.SyncPlay;

namespace Mythra.Infrastructure.Persistence.Configurations;

public sealed class StreamSessionConfiguration : IEntityTypeConfiguration<StreamSession>
{
    public void Configure(EntityTypeBuilder<StreamSession> b)
    {
        b.ToTable("stream_sessions");
        b.HasKey(s => s.Id);
        b.Property(s => s.SessionToken).HasMaxLength(64).IsRequired();
        b.HasIndex(s => s.SessionToken).IsUnique();
        b.Property(s => s.Mode).HasConversion<int>();
        b.Property(s => s.State).HasConversion<int>();
        b.Property(s => s.PlaylistPath).HasMaxLength(1024);
        b.Property(s => s.VideoCodec).HasMaxLength(32);
        b.Property(s => s.AudioCodec).HasMaxLength(32);
        b.Property(s => s.UserAgent).HasMaxLength(512);
        b.Property(s => s.IpAddress).HasMaxLength(64);
        b.Property(s => s.FailureReason).HasMaxLength(2000);
        b.Ignore(s => s.DomainEvents);
    }
}

public sealed class SyncRoomConfiguration : IEntityTypeConfiguration<SyncRoom>
{
    public void Configure(EntityTypeBuilder<SyncRoom> b)
    {
        b.ToTable("sync_rooms");
        b.HasKey(r => r.Id);
        b.Property(r => r.Code).HasMaxLength(8).IsRequired();
        b.HasIndex(r => r.Code).IsUnique();
        b.Property(r => r.Name).HasMaxLength(80).IsRequired();
        b.HasMany(r => r.Members).WithOne().HasForeignKey(m => m.SyncRoomId).OnDelete(DeleteBehavior.Cascade);
        b.Ignore(r => r.DomainEvents);
    }
}

public sealed class SyncMemberConfiguration : IEntityTypeConfiguration<SyncMember>
{
    public void Configure(EntityTypeBuilder<SyncMember> b)
    {
        b.ToTable("sync_members");
        b.HasKey(m => m.Id);
        b.Property(m => m.DisplayName).HasMaxLength(64);
        b.HasIndex(m => new { m.SyncRoomId, m.UserId }).IsUnique();
    }
}
