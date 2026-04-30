using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Mythra.Domain.Notifications;

namespace Mythra.Infrastructure.Persistence.Configurations;

public sealed class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> b)
    {
        b.ToTable("notifications");
        b.HasKey(n => n.Id);
        b.Property(n => n.Kind).HasConversion<int>();
        b.Property(n => n.Title).HasMaxLength(256).IsRequired();
        b.Property(n => n.Body).HasMaxLength(1024);
        b.Property(n => n.ActionUrl).HasMaxLength(512);
        b.Property(n => n.ImageUrl).HasMaxLength(512);
        b.Property(n => n.Payload).HasMaxLength(4096);
        b.Property(n => n.IsRead).HasDefaultValue(false);
        b.HasIndex(n => new { n.UserId, n.IsRead });
        b.HasIndex(n => n.CreatedAt);
    }
}
