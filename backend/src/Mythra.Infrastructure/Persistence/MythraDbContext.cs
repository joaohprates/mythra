using Microsoft.EntityFrameworkCore;
using Mythra.Application.Abstractions.Persistence;
using Mythra.Domain.Common;
using Mythra.Domain.Libraries;
using Mythra.Domain.Media;
using Mythra.Domain.Media.Audio;
using Mythra.Domain.Media.Books;
using Mythra.Domain.Media.Manga;
using Mythra.Domain.Media.Video;
using Mythra.Domain.Progress;
using Mythra.Domain.Streaming;
using Mythra.Domain.SyncPlay;
using Mythra.Domain.Users;

namespace Mythra.Infrastructure.Persistence;

public sealed class MythraDbContext(DbContextOptions<MythraDbContext> options) : DbContext(options), IUnitOfWork
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Profile> Profiles => Set<Profile>();
    public DbSet<Session> Sessions => Set<Session>();

    public DbSet<Library> Libraries => Set<Library>();
    public DbSet<LibraryFolder> LibraryFolders => Set<LibraryFolder>();

    public DbSet<MediaItem> MediaItems => Set<MediaItem>();
    public DbSet<VideoItem> Videos => Set<VideoItem>();
    public DbSet<MangaItem> Mangas => Set<MangaItem>();
    public DbSet<BookItem> Books => Set<BookItem>();
    public DbSet<AudioItem> Audios => Set<AudioItem>();
    public DbSet<Subtitle> Subtitles => Set<Subtitle>();
    public DbSet<AudioTrack> AudioTracks => Set<AudioTrack>();
    public DbSet<ChapterMarker> ChapterMarkers => Set<ChapterMarker>();
    public DbSet<MangaChapter> MangaChapters => Set<MangaChapter>();
    public DbSet<BookChapter> BookChapters => Set<BookChapter>();
    public DbSet<AudioChapter> AudioChapters => Set<AudioChapter>();

    public DbSet<Genre> Genres => Set<Genre>();
    public DbSet<Tag> Tags => Set<Tag>();
    public DbSet<Person> People => Set<Person>();
    public DbSet<MediaPersonRole> MediaPersonRoles => Set<MediaPersonRole>();

    public DbSet<PlaybackProgress> Playbacks => Set<PlaybackProgress>();
    public DbSet<ReadingProgress> Readings => Set<ReadingProgress>();
    public DbSet<Bookmark> Bookmarks => Set<Bookmark>();
    public DbSet<Highlight> Highlights => Set<Highlight>();

    public DbSet<StreamSession> StreamSessions => Set<StreamSession>();
    public DbSet<SyncRoom> SyncRooms => Set<SyncRoom>();
    public DbSet<SyncMember> SyncMembers => Set<SyncMember>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.ApplyConfigurationsFromAssembly(typeof(MythraDbContext).Assembly);
        base.OnModelCreating(builder);
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder builder)
    {
        // SQLite cannot ORDER BY a DateTimeOffset; persist as long ticks.
        builder.Properties<DateTimeOffset>()
            .HaveConversion<Microsoft.EntityFrameworkCore.Storage.ValueConversion.DateTimeOffsetToBinaryConverter>();
        builder.Properties<DateTimeOffset?>()
            .HaveConversion<Microsoft.EntityFrameworkCore.Storage.ValueConversion.DateTimeOffsetToBinaryConverter>();
        base.ConfigureConventions(builder);
    }

    public override int SaveChanges()
    {
        TouchTimestamps();
        return base.SaveChanges();
    }

    public override Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        TouchTimestamps();
        return base.SaveChangesAsync(ct);
    }

    public async Task<TResult> ExecuteInTransactionAsync<TResult>(Func<CancellationToken, Task<TResult>> work, CancellationToken ct = default)
    {
        await using var trx = await Database.BeginTransactionAsync(ct);
        try
        {
            var result = await work(ct);
            await trx.CommitAsync(ct);
            return result;
        }
        catch
        {
            await trx.RollbackAsync(ct);
            throw;
        }
    }

    private void TouchTimestamps()
    {
        foreach (var entry in ChangeTracker.Entries<Entity>())
        {
            if (entry.State == EntityState.Modified) entry.Entity.Touch();
        }
    }
}
