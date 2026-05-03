using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Mythra.Domain.Media;
using Mythra.Domain.Users;

namespace Mythra.Infrastructure.Persistence.Configurations;

public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> b)
    {
        b.ToTable("users");
        b.HasKey(u => u.Id);
        b.Property(u => u.Email).HasMaxLength(254).IsRequired();
        b.HasIndex(u => u.Email).IsUnique();
        b.Property(u => u.Username).HasMaxLength(64).IsRequired();
        b.HasIndex(u => u.Username).IsUnique();
        b.Property(u => u.PasswordHash).HasMaxLength(120).IsRequired();
        b.Property(u => u.Role).HasConversion<int>();
        b.Property(u => u.PreferredLanguage).HasMaxLength(10);
        b.Property(u => u.AvatarPath).HasMaxLength(512);
        b.HasMany(u => u.Profiles).WithOne().HasForeignKey(p => p.UserId).OnDelete(DeleteBehavior.Cascade);
        b.Ignore(u => u.DomainEvents);
    }
}

public sealed class ProfileConfiguration : IEntityTypeConfiguration<Profile>
{
    public void Configure(EntityTypeBuilder<Profile> b)
    {
        b.ToTable("profiles");
        b.HasKey(p => p.Id);
        b.Property(p => p.Name).HasMaxLength(64).IsRequired();
        b.Property(p => p.AvatarPath).HasMaxLength(512);
        b.Property(p => p.Theme).HasMaxLength(48);
        b.Property(p => p.EnabledMediaKinds).HasConversion(
            v => string.Join(',', v.Select(k => (int)k)),
            v => v.Split(',', StringSplitOptions.RemoveEmptyEntries)
                  .Select(s => (MediaKind)int.Parse(s))
                  .ToList(),
            new ValueComparer<List<MediaKind>>(
                (a, b) => a != null && b != null && a.SequenceEqual(b),
                c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
                c => c.ToList()));
        b.Property(p => p.PreferredContentLanguage).HasMaxLength(10);
        b.Property(p => p.PreferredSubtitleLanguage).HasMaxLength(10);
        b.Property(p => p.PreferredAudioLanguage).HasMaxLength(10);
        b.Property(p => p.ShowOriginalTitle).HasDefaultValue(false);
        b.Property(p => p.ShowAdultContent).HasDefaultValue(false);
        b.HasIndex(p => new { p.UserId, p.Name }).IsUnique();
    }
}

public sealed class SessionConfiguration : IEntityTypeConfiguration<Session>
{
    public void Configure(EntityTypeBuilder<Session> b)
    {
        b.ToTable("sessions");
        b.HasKey(s => s.Id);
        b.Property(s => s.RefreshTokenHash).HasMaxLength(128).IsRequired();
        b.HasIndex(s => s.RefreshTokenHash).IsUnique();
        b.HasIndex(s => s.UserId);
        b.Property(s => s.UserAgent).HasMaxLength(512);
        b.Property(s => s.IpAddress).HasMaxLength(64);
    }
}
