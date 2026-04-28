using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Mythra.Domain.Media.Books;

namespace Mythra.Infrastructure.Persistence.Configurations;

public sealed class BookItemConfiguration : IEntityTypeConfiguration<BookItem>
{
    public void Configure(EntityTypeBuilder<BookItem> b)
    {
        b.Property(x => x.Author).HasMaxLength(256);
        b.Property(x => x.Publisher).HasMaxLength(256);
        b.Property(x => x.Isbn).HasMaxLength(32);
        b.Property(x => x.Series).HasMaxLength(256);
        b.Property(x => x.Format).HasConversion<int>();
        b.Property(x => x.FilePath).HasMaxLength(1024).IsRequired();
        b.HasIndex(x => x.FilePath);
        b.HasMany(x => x.Chapters).WithOne().HasForeignKey(c => c.BookItemId).OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class BookChapterConfiguration : IEntityTypeConfiguration<BookChapter>
{
    public void Configure(EntityTypeBuilder<BookChapter> b)
    {
        b.ToTable("book_chapters");
        b.HasKey(c => c.Id);
        b.Property(c => c.Title).HasMaxLength(256).IsRequired();
        b.Property(c => c.Anchor).HasMaxLength(256);
    }
}
