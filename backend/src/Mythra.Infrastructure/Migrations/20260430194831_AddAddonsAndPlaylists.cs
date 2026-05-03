using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Mythra.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAddonsAndPlaylists : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "addons",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 1024, nullable: true),
                    IconUrl = table.Column<string>(type: "TEXT", maxLength: 512, nullable: true),
                    Kind = table.Column<int>(type: "INTEGER", nullable: false),
                    TargetMediaKind = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    ProviderType = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    ProviderConfigJson = table.Column<string>(type: "TEXT", nullable: false),
                    SecretsJson = table.Column<string>(type: "TEXT", nullable: true),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    SourceChecksum = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    ImportedFrom = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    CreatedAt = table.Column<long>(type: "INTEGER", nullable: false),
                    UpdatedAt = table.Column<long>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_addons", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "bookmarks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ProfileId = table.Column<Guid>(type: "TEXT", nullable: false),
                    MediaItemId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Label = table.Column<string>(type: "TEXT", maxLength: 128, nullable: true),
                    Note = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    Position = table.Column<TimeSpan>(type: "TEXT", nullable: true),
                    Page = table.Column<int>(type: "INTEGER", nullable: true),
                    CfiLocator = table.Column<string>(type: "TEXT", maxLength: 512, nullable: true),
                    CreatedAt = table.Column<long>(type: "INTEGER", nullable: false),
                    UpdatedAt = table.Column<long>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_bookmarks", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "genres",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    Slug = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    Kind = table.Column<int>(type: "INTEGER", nullable: true),
                    CreatedAt = table.Column<long>(type: "INTEGER", nullable: false),
                    UpdatedAt = table.Column<long>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_genres", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "highlights",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ProfileId = table.Column<Guid>(type: "TEXT", nullable: false),
                    MediaItemId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Text = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: false),
                    Note = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    Color = table.Column<string>(type: "TEXT", maxLength: 16, nullable: false),
                    CfiStart = table.Column<string>(type: "TEXT", maxLength: 512, nullable: true),
                    CfiEnd = table.Column<string>(type: "TEXT", maxLength: 512, nullable: true),
                    Page = table.Column<int>(type: "INTEGER", nullable: true),
                    CreatedAt = table.Column<long>(type: "INTEGER", nullable: false),
                    UpdatedAt = table.Column<long>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_highlights", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "libraries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                    Kind = table.Column<int>(type: "INTEGER", nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 1024, nullable: true),
                    PreferredLanguage = table.Column<string>(type: "TEXT", maxLength: 10, nullable: true),
                    PreferredMetadataProvider = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true),
                    AutoRefreshMetadata = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsSystem = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: false),
                    LastScannedAt = table.Column<long>(type: "INTEGER", nullable: true),
                    AllowedExtensions = table.Column<string>(type: "TEXT", nullable: false, defaultValue: ""),
                    CreatedAt = table.Column<long>(type: "INTEGER", nullable: false),
                    UpdatedAt = table.Column<long>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_libraries", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "media_items",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    LibraryId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 512, nullable: false),
                    OriginalTitle = table.Column<string>(type: "TEXT", maxLength: 512, nullable: true),
                    SortTitle = table.Column<string>(type: "TEXT", maxLength: 512, nullable: true),
                    Overview = table.Column<string>(type: "TEXT", maxLength: 8000, nullable: true),
                    Tagline = table.Column<string>(type: "TEXT", maxLength: 512, nullable: true),
                    PosterPath = table.Column<string>(type: "TEXT", maxLength: 1024, nullable: true),
                    BackdropPath = table.Column<string>(type: "TEXT", maxLength: 1024, nullable: true),
                    ThumbPath = table.Column<string>(type: "TEXT", maxLength: 1024, nullable: true),
                    ReleaseDate = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    Rating = table.Column<double>(type: "REAL", nullable: true),
                    RatingCount = table.Column<int>(type: "INTEGER", nullable: true),
                    Language = table.Column<string>(type: "TEXT", maxLength: 10, nullable: true),
                    CountryCode = table.Column<string>(type: "TEXT", maxLength: 8, nullable: true),
                    ProviderTmdbId = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true),
                    ProviderImdbId = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true),
                    ProviderAnilistId = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true),
                    ProviderMalId = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true),
                    ProviderMusicbrainzId = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true),
                    ProviderGoogleBooksId = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true),
                    ProviderGutenbergId = table.Column<string>(type: "TEXT", nullable: true),
                    ProviderLibriVoxId = table.Column<string>(type: "TEXT", nullable: true),
                    ProviderMangaDexId = table.Column<string>(type: "TEXT", nullable: true),
                    ProviderArchiveOrgId = table.Column<string>(type: "TEXT", nullable: true),
                    FileStatus = table.Column<string>(type: "TEXT", nullable: false),
                    LastScannedAt = table.Column<long>(type: "INTEGER", nullable: true),
                    LastMetadataRefreshAt = table.Column<long>(type: "INTEGER", nullable: true),
                    MediaKind = table.Column<int>(type: "INTEGER", nullable: false),
                    AudioKind = table.Column<int>(type: "INTEGER", nullable: true),
                    AudioItem_Author = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    Narrator = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    AudioItem_Series = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    AudioItem_SeriesIndex = table.Column<int>(type: "INTEGER", nullable: true),
                    Duration = table.Column<TimeSpan>(type: "TEXT", nullable: true),
                    AudioItem_RootPath = table.Column<string>(type: "TEXT", maxLength: 1024, nullable: true),
                    CoverPath = table.Column<string>(type: "TEXT", maxLength: 1024, nullable: true),
                    BookItem_Author = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    Publisher = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    Isbn = table.Column<string>(type: "TEXT", maxLength: 32, nullable: true),
                    Series = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    SeriesIndex = table.Column<int>(type: "INTEGER", nullable: true),
                    Format = table.Column<int>(type: "INTEGER", nullable: true),
                    FilePath = table.Column<string>(type: "TEXT", maxLength: 1024, nullable: true),
                    FileSizeBytes = table.Column<long>(type: "INTEGER", nullable: true),
                    PageCount = table.Column<int>(type: "INTEGER", nullable: true),
                    WordCount = table.Column<int>(type: "INTEGER", nullable: true),
                    Author = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    Artist = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    Status = table.Column<string>(type: "TEXT", maxLength: 32, nullable: true),
                    ReadingDirection = table.Column<int>(type: "INTEGER", nullable: true),
                    TotalChapters = table.Column<int>(type: "INTEGER", nullable: true),
                    TotalVolumes = table.Column<int>(type: "INTEGER", nullable: true),
                    RootPath = table.Column<string>(type: "TEXT", maxLength: 1024, nullable: true),
                    VideoKind = table.Column<int>(type: "INTEGER", nullable: true),
                    IsAnime = table.Column<bool>(type: "INTEGER", nullable: true),
                    VideoItem_Duration = table.Column<TimeSpan>(type: "TEXT", nullable: true),
                    VideoItem_FilePath = table.Column<string>(type: "TEXT", maxLength: 1024, nullable: true),
                    VideoItem_FileSizeBytes = table.Column<long>(type: "INTEGER", nullable: true),
                    Container = table.Column<string>(type: "TEXT", maxLength: 32, nullable: true),
                    VideoCodec = table.Column<string>(type: "TEXT", maxLength: 32, nullable: true),
                    AudioCodec = table.Column<string>(type: "TEXT", maxLength: 32, nullable: true),
                    Width = table.Column<int>(type: "INTEGER", nullable: true),
                    Height = table.Column<int>(type: "INTEGER", nullable: true),
                    FrameRate = table.Column<double>(type: "REAL", nullable: true),
                    Bitrate = table.Column<long>(type: "INTEGER", nullable: true),
                    ParentId = table.Column<Guid>(type: "TEXT", nullable: true),
                    SeasonNumber = table.Column<int>(type: "INTEGER", nullable: true),
                    EpisodeNumber = table.Column<int>(type: "INTEGER", nullable: true),
                    AbsoluteEpisodeNumber = table.Column<int>(type: "INTEGER", nullable: true),
                    CreatedAt = table.Column<long>(type: "INTEGER", nullable: false),
                    UpdatedAt = table.Column<long>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_media_items", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "notifications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: true),
                    ProfileId = table.Column<Guid>(type: "TEXT", nullable: true),
                    Kind = table.Column<int>(type: "INTEGER", nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    Body = table.Column<string>(type: "TEXT", maxLength: 1024, nullable: true),
                    ActionUrl = table.Column<string>(type: "TEXT", maxLength: 512, nullable: true),
                    ImageUrl = table.Column<string>(type: "TEXT", maxLength: 512, nullable: true),
                    IsRead = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: false),
                    Payload = table.Column<string>(type: "TEXT", maxLength: 4096, nullable: true),
                    CreatedAt = table.Column<long>(type: "INTEGER", nullable: false),
                    UpdatedAt = table.Column<long>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_notifications", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "people",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    PhotoPath = table.Column<string>(type: "TEXT", maxLength: 1024, nullable: true),
                    Biography = table.Column<string>(type: "TEXT", maxLength: 8000, nullable: true),
                    Birthday = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    ProviderTmdbId = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true),
                    CreatedAt = table.Column<long>(type: "INTEGER", nullable: false),
                    UpdatedAt = table.Column<long>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_people", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "playback_progress",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ProfileId = table.Column<Guid>(type: "TEXT", nullable: false),
                    MediaItemId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Position = table.Column<TimeSpan>(type: "TEXT", nullable: false),
                    Duration = table.Column<TimeSpan>(type: "TEXT", nullable: true),
                    IsCompleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    LastWatchedAt = table.Column<long>(type: "INTEGER", nullable: false),
                    PreferredAudioStreamIndex = table.Column<int>(type: "INTEGER", nullable: true),
                    PreferredSubtitleStreamIndex = table.Column<int>(type: "INTEGER", nullable: true),
                    PlaybackSpeed = table.Column<double>(type: "REAL", nullable: false),
                    CreatedAt = table.Column<long>(type: "INTEGER", nullable: false),
                    UpdatedAt = table.Column<long>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_playback_progress", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "playlists",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ProfileId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 2048, nullable: true),
                    IsPublic = table.Column<bool>(type: "INTEGER", nullable: false),
                    CoverImagePath = table.Column<string>(type: "TEXT", maxLength: 512, nullable: true),
                    CreatedAt = table.Column<long>(type: "INTEGER", nullable: false),
                    UpdatedAt = table.Column<long>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_playlists", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "reading_progress",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ProfileId = table.Column<Guid>(type: "TEXT", nullable: false),
                    MediaItemId = table.Column<Guid>(type: "TEXT", nullable: false),
                    CurrentChapterId = table.Column<Guid>(type: "TEXT", nullable: true),
                    CurrentPage = table.Column<int>(type: "INTEGER", nullable: true),
                    TotalPages = table.Column<int>(type: "INTEGER", nullable: true),
                    CfiLocator = table.Column<string>(type: "TEXT", maxLength: 512, nullable: true),
                    PercentComplete = table.Column<double>(type: "REAL", nullable: false),
                    IsCompleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    LastReadAt = table.Column<long>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<long>(type: "INTEGER", nullable: false),
                    UpdatedAt = table.Column<long>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_reading_progress", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "sessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    RefreshTokenHash = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    ExpiresAt = table.Column<long>(type: "INTEGER", nullable: false),
                    RevokedAt = table.Column<long>(type: "INTEGER", nullable: true),
                    UserAgent = table.Column<string>(type: "TEXT", maxLength: 512, nullable: true),
                    IpAddress = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true),
                    CreatedAt = table.Column<long>(type: "INTEGER", nullable: false),
                    UpdatedAt = table.Column<long>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sessions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "stream_sessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ProfileId = table.Column<Guid>(type: "TEXT", nullable: false),
                    VideoItemId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Mode = table.Column<int>(type: "INTEGER", nullable: false),
                    State = table.Column<int>(type: "INTEGER", nullable: false),
                    SessionToken = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    PlaylistPath = table.Column<string>(type: "TEXT", maxLength: 1024, nullable: true),
                    VideoCodec = table.Column<string>(type: "TEXT", maxLength: 32, nullable: true),
                    AudioCodec = table.Column<string>(type: "TEXT", maxLength: 32, nullable: true),
                    Width = table.Column<int>(type: "INTEGER", nullable: true),
                    Height = table.Column<int>(type: "INTEGER", nullable: true),
                    Bitrate = table.Column<long>(type: "INTEGER", nullable: true),
                    StartedAt = table.Column<long>(type: "INTEGER", nullable: false),
                    EndedAt = table.Column<long>(type: "INTEGER", nullable: true),
                    FailureReason = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    UserAgent = table.Column<string>(type: "TEXT", maxLength: 512, nullable: true),
                    IpAddress = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true),
                    CreatedAt = table.Column<long>(type: "INTEGER", nullable: false),
                    UpdatedAt = table.Column<long>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_stream_sessions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "sync_rooms",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Code = table.Column<string>(type: "TEXT", maxLength: 8, nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                    HostUserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    CurrentMediaItemId = table.Column<Guid>(type: "TEXT", nullable: true),
                    CurrentPosition = table.Column<TimeSpan>(type: "TEXT", nullable: false),
                    IsPlaying = table.Column<bool>(type: "INTEGER", nullable: false),
                    LastPositionUpdateAt = table.Column<long>(type: "INTEGER", nullable: false),
                    IsClosed = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<long>(type: "INTEGER", nullable: false),
                    UpdatedAt = table.Column<long>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sync_rooms", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "tags",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    Slug = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    CreatedAt = table.Column<long>(type: "INTEGER", nullable: false),
                    UpdatedAt = table.Column<long>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tags", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Email = table.Column<string>(type: "TEXT", maxLength: 254, nullable: false),
                    Username = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    PasswordHash = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false),
                    Role = table.Column<int>(type: "INTEGER", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    LastLoginAt = table.Column<long>(type: "INTEGER", nullable: true),
                    AvatarPath = table.Column<string>(type: "TEXT", maxLength: 512, nullable: true),
                    PreferredLanguage = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                    CreatedAt = table.Column<long>(type: "INTEGER", nullable: false),
                    UpdatedAt = table.Column<long>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "library_folders",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    LibraryId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Path = table.Column<string>(type: "TEXT", maxLength: 1024, nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    LastScannedAt = table.Column<long>(type: "INTEGER", nullable: true),
                    CreatedAt = table.Column<long>(type: "INTEGER", nullable: false),
                    UpdatedAt = table.Column<long>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_library_folders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_library_folders_libraries_LibraryId",
                        column: x => x.LibraryId,
                        principalTable: "libraries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "audio_chapters",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    AudioItemId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Order = table.Column<int>(type: "INTEGER", nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    FilePath = table.Column<string>(type: "TEXT", maxLength: 1024, nullable: false),
                    Start = table.Column<TimeSpan>(type: "TEXT", nullable: false),
                    Duration = table.Column<TimeSpan>(type: "TEXT", nullable: false),
                    Codec = table.Column<string>(type: "TEXT", maxLength: 32, nullable: true),
                    Bitrate = table.Column<long>(type: "INTEGER", nullable: true),
                    CreatedAt = table.Column<long>(type: "INTEGER", nullable: false),
                    UpdatedAt = table.Column<long>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_audio_chapters", x => x.Id);
                    table.ForeignKey(
                        name: "FK_audio_chapters_media_items_AudioItemId",
                        column: x => x.AudioItemId,
                        principalTable: "media_items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "audio_tracks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    VideoItemId = table.Column<Guid>(type: "TEXT", nullable: false),
                    LanguageCode = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                    DisplayName = table.Column<string>(type: "TEXT", maxLength: 128, nullable: true),
                    StreamIndex = table.Column<int>(type: "INTEGER", nullable: false),
                    Codec = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    Channels = table.Column<int>(type: "INTEGER", nullable: false),
                    ChannelLayout = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    SampleRate = table.Column<int>(type: "INTEGER", nullable: false),
                    Bitrate = table.Column<long>(type: "INTEGER", nullable: true),
                    IsDefault = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsCommentary = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<long>(type: "INTEGER", nullable: false),
                    UpdatedAt = table.Column<long>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_audio_tracks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_audio_tracks_media_items_VideoItemId",
                        column: x => x.VideoItemId,
                        principalTable: "media_items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "book_chapters",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    BookItemId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Order = table.Column<int>(type: "INTEGER", nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    Anchor = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    StartPage = table.Column<int>(type: "INTEGER", nullable: true),
                    EndPage = table.Column<int>(type: "INTEGER", nullable: true),
                    WordCount = table.Column<int>(type: "INTEGER", nullable: true),
                    CreatedAt = table.Column<long>(type: "INTEGER", nullable: false),
                    UpdatedAt = table.Column<long>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_book_chapters", x => x.Id);
                    table.ForeignKey(
                        name: "FK_book_chapters_media_items_BookItemId",
                        column: x => x.BookItemId,
                        principalTable: "media_items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "chapter_markers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    VideoItemId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Kind = table.Column<int>(type: "INTEGER", nullable: false),
                    Label = table.Column<string>(type: "TEXT", maxLength: 128, nullable: true),
                    Start = table.Column<TimeSpan>(type: "TEXT", nullable: false),
                    End = table.Column<TimeSpan>(type: "TEXT", nullable: true),
                    ThumbPath = table.Column<string>(type: "TEXT", maxLength: 1024, nullable: true),
                    CreatedAt = table.Column<long>(type: "INTEGER", nullable: false),
                    UpdatedAt = table.Column<long>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_chapter_markers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_chapter_markers_media_items_VideoItemId",
                        column: x => x.VideoItemId,
                        principalTable: "media_items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "manga_chapters",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    MangaItemId = table.Column<Guid>(type: "TEXT", nullable: false),
                    VolumeNumber = table.Column<int>(type: "INTEGER", nullable: true),
                    ChapterNumber = table.Column<double>(type: "REAL", nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    ArchivePath = table.Column<string>(type: "TEXT", maxLength: 1024, nullable: false),
                    ArchiveFormat = table.Column<string>(type: "TEXT", maxLength: 8, nullable: false),
                    PageCount = table.Column<int>(type: "INTEGER", nullable: false),
                    CoverPath = table.Column<string>(type: "TEXT", maxLength: 1024, nullable: true),
                    ReleaseDate = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<long>(type: "INTEGER", nullable: false),
                    UpdatedAt = table.Column<long>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_manga_chapters", x => x.Id);
                    table.ForeignKey(
                        name: "FK_manga_chapters_media_items_MangaItemId",
                        column: x => x.MangaItemId,
                        principalTable: "media_items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "media_item_genres",
                columns: table => new
                {
                    GenresId = table.Column<Guid>(type: "TEXT", nullable: false),
                    MediaItemId = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_media_item_genres", x => new { x.GenresId, x.MediaItemId });
                    table.ForeignKey(
                        name: "FK_media_item_genres_genres_GenresId",
                        column: x => x.GenresId,
                        principalTable: "genres",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_media_item_genres_media_items_MediaItemId",
                        column: x => x.MediaItemId,
                        principalTable: "media_items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "subtitles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    VideoItemId = table.Column<Guid>(type: "TEXT", nullable: false),
                    LanguageCode = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                    DisplayName = table.Column<string>(type: "TEXT", maxLength: 128, nullable: true),
                    FilePath = table.Column<string>(type: "TEXT", maxLength: 1024, nullable: true),
                    StreamIndex = table.Column<int>(type: "INTEGER", nullable: true),
                    Format = table.Column<string>(type: "TEXT", maxLength: 16, nullable: false),
                    Kind = table.Column<int>(type: "INTEGER", nullable: false),
                    IsDefault = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsForced = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<long>(type: "INTEGER", nullable: false),
                    UpdatedAt = table.Column<long>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_subtitles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_subtitles_media_items_VideoItemId",
                        column: x => x.VideoItemId,
                        principalTable: "media_items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "media_person_roles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    MediaItemId = table.Column<Guid>(type: "TEXT", nullable: false),
                    PersonId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Role = table.Column<int>(type: "INTEGER", nullable: false),
                    Character = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    Order = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<long>(type: "INTEGER", nullable: false),
                    UpdatedAt = table.Column<long>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_media_person_roles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_media_person_roles_media_items_MediaItemId",
                        column: x => x.MediaItemId,
                        principalTable: "media_items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_media_person_roles_people_PersonId",
                        column: x => x.PersonId,
                        principalTable: "people",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "playlist_items",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    PlaylistId = table.Column<Guid>(type: "TEXT", nullable: false),
                    MediaItemId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Order = table.Column<int>(type: "INTEGER", nullable: false),
                    AddedAt = table.Column<long>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<long>(type: "INTEGER", nullable: false),
                    UpdatedAt = table.Column<long>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_playlist_items", x => x.Id);
                    table.ForeignKey(
                        name: "FK_playlist_items_playlists_PlaylistId",
                        column: x => x.PlaylistId,
                        principalTable: "playlists",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "sync_members",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    SyncRoomId = table.Column<Guid>(type: "TEXT", nullable: false),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    DisplayName = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    IsHost = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsReady = table.Column<bool>(type: "INTEGER", nullable: false),
                    Position = table.Column<TimeSpan>(type: "TEXT", nullable: false),
                    Latency = table.Column<double>(type: "REAL", nullable: false),
                    JoinedAt = table.Column<long>(type: "INTEGER", nullable: false),
                    LastPingAt = table.Column<long>(type: "INTEGER", nullable: true),
                    CreatedAt = table.Column<long>(type: "INTEGER", nullable: false),
                    UpdatedAt = table.Column<long>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sync_members", x => x.Id);
                    table.ForeignKey(
                        name: "FK_sync_members_sync_rooms_SyncRoomId",
                        column: x => x.SyncRoomId,
                        principalTable: "sync_rooms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "media_item_tags",
                columns: table => new
                {
                    MediaItemId = table.Column<Guid>(type: "TEXT", nullable: false),
                    TagsId = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_media_item_tags", x => new { x.MediaItemId, x.TagsId });
                    table.ForeignKey(
                        name: "FK_media_item_tags_media_items_MediaItemId",
                        column: x => x.MediaItemId,
                        principalTable: "media_items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_media_item_tags_tags_TagsId",
                        column: x => x.TagsId,
                        principalTable: "tags",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "profiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    AvatarPath = table.Column<string>(type: "TEXT", maxLength: 512, nullable: true),
                    IsKidFriendly = table.Column<bool>(type: "INTEGER", nullable: false),
                    Theme = table.Column<string>(type: "TEXT", maxLength: 48, nullable: false),
                    EnabledMediaKinds = table.Column<string>(type: "TEXT", nullable: false),
                    PreferredContentLanguage = table.Column<string>(type: "TEXT", maxLength: 10, nullable: true),
                    PreferredSubtitleLanguage = table.Column<string>(type: "TEXT", maxLength: 10, nullable: true),
                    PreferredAudioLanguage = table.Column<string>(type: "TEXT", maxLength: 10, nullable: true),
                    ShowOriginalTitle = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: false),
                    CreatedAt = table.Column<long>(type: "INTEGER", nullable: false),
                    UpdatedAt = table.Column<long>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_profiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_profiles_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_addons_UserId",
                table: "addons",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_audio_chapters_AudioItemId",
                table: "audio_chapters",
                column: "AudioItemId");

            migrationBuilder.CreateIndex(
                name: "IX_audio_tracks_VideoItemId",
                table: "audio_tracks",
                column: "VideoItemId");

            migrationBuilder.CreateIndex(
                name: "IX_book_chapters_BookItemId",
                table: "book_chapters",
                column: "BookItemId");

            migrationBuilder.CreateIndex(
                name: "IX_bookmarks_ProfileId_MediaItemId",
                table: "bookmarks",
                columns: new[] { "ProfileId", "MediaItemId" });

            migrationBuilder.CreateIndex(
                name: "IX_chapter_markers_VideoItemId",
                table: "chapter_markers",
                column: "VideoItemId");

            migrationBuilder.CreateIndex(
                name: "IX_genres_Slug",
                table: "genres",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_highlights_ProfileId_MediaItemId",
                table: "highlights",
                columns: new[] { "ProfileId", "MediaItemId" });

            migrationBuilder.CreateIndex(
                name: "IX_libraries_Name",
                table: "libraries",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_library_folders_LibraryId_Path",
                table: "library_folders",
                columns: new[] { "LibraryId", "Path" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_manga_chapters_MangaItemId_VolumeNumber_ChapterNumber",
                table: "manga_chapters",
                columns: new[] { "MangaItemId", "VolumeNumber", "ChapterNumber" });

            migrationBuilder.CreateIndex(
                name: "IX_media_item_genres_MediaItemId",
                table: "media_item_genres",
                column: "MediaItemId");

            migrationBuilder.CreateIndex(
                name: "IX_media_item_tags_TagsId",
                table: "media_item_tags",
                column: "TagsId");

            migrationBuilder.CreateIndex(
                name: "IX_media_items_FilePath",
                table: "media_items",
                column: "FilePath");

            migrationBuilder.CreateIndex(
                name: "IX_media_items_LibraryId",
                table: "media_items",
                column: "LibraryId");

            migrationBuilder.CreateIndex(
                name: "IX_media_items_ParentId_SeasonNumber_EpisodeNumber",
                table: "media_items",
                columns: new[] { "ParentId", "SeasonNumber", "EpisodeNumber" });

            migrationBuilder.CreateIndex(
                name: "IX_media_items_ProviderTmdbId",
                table: "media_items",
                column: "ProviderTmdbId");

            migrationBuilder.CreateIndex(
                name: "IX_media_items_Title",
                table: "media_items",
                column: "Title");

            migrationBuilder.CreateIndex(
                name: "IX_media_items_VideoItem_FilePath",
                table: "media_items",
                column: "VideoItem_FilePath");

            migrationBuilder.CreateIndex(
                name: "IX_media_person_roles_MediaItemId_PersonId_Role",
                table: "media_person_roles",
                columns: new[] { "MediaItemId", "PersonId", "Role" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_media_person_roles_PersonId",
                table: "media_person_roles",
                column: "PersonId");

            migrationBuilder.CreateIndex(
                name: "IX_notifications_CreatedAt",
                table: "notifications",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_notifications_UserId_IsRead",
                table: "notifications",
                columns: new[] { "UserId", "IsRead" });

            migrationBuilder.CreateIndex(
                name: "IX_people_Name",
                table: "people",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_playback_progress_LastWatchedAt",
                table: "playback_progress",
                column: "LastWatchedAt");

            migrationBuilder.CreateIndex(
                name: "IX_playback_progress_ProfileId_MediaItemId",
                table: "playback_progress",
                columns: new[] { "ProfileId", "MediaItemId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_playlist_items_PlaylistId_MediaItemId",
                table: "playlist_items",
                columns: new[] { "PlaylistId", "MediaItemId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_playlist_items_PlaylistId_Order",
                table: "playlist_items",
                columns: new[] { "PlaylistId", "Order" });

            migrationBuilder.CreateIndex(
                name: "IX_playlists_ProfileId",
                table: "playlists",
                column: "ProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_profiles_UserId_Name",
                table: "profiles",
                columns: new[] { "UserId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_reading_progress_LastReadAt",
                table: "reading_progress",
                column: "LastReadAt");

            migrationBuilder.CreateIndex(
                name: "IX_reading_progress_ProfileId_MediaItemId",
                table: "reading_progress",
                columns: new[] { "ProfileId", "MediaItemId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_sessions_RefreshTokenHash",
                table: "sessions",
                column: "RefreshTokenHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_sessions_UserId",
                table: "sessions",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_stream_sessions_SessionToken",
                table: "stream_sessions",
                column: "SessionToken",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_subtitles_VideoItemId",
                table: "subtitles",
                column: "VideoItemId");

            migrationBuilder.CreateIndex(
                name: "IX_sync_members_SyncRoomId_UserId",
                table: "sync_members",
                columns: new[] { "SyncRoomId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_sync_rooms_Code",
                table: "sync_rooms",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_tags_Slug",
                table: "tags",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_users_Email",
                table: "users",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_users_Username",
                table: "users",
                column: "Username",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "addons");

            migrationBuilder.DropTable(
                name: "audio_chapters");

            migrationBuilder.DropTable(
                name: "audio_tracks");

            migrationBuilder.DropTable(
                name: "book_chapters");

            migrationBuilder.DropTable(
                name: "bookmarks");

            migrationBuilder.DropTable(
                name: "chapter_markers");

            migrationBuilder.DropTable(
                name: "highlights");

            migrationBuilder.DropTable(
                name: "library_folders");

            migrationBuilder.DropTable(
                name: "manga_chapters");

            migrationBuilder.DropTable(
                name: "media_item_genres");

            migrationBuilder.DropTable(
                name: "media_item_tags");

            migrationBuilder.DropTable(
                name: "media_person_roles");

            migrationBuilder.DropTable(
                name: "notifications");

            migrationBuilder.DropTable(
                name: "playback_progress");

            migrationBuilder.DropTable(
                name: "playlist_items");

            migrationBuilder.DropTable(
                name: "profiles");

            migrationBuilder.DropTable(
                name: "reading_progress");

            migrationBuilder.DropTable(
                name: "sessions");

            migrationBuilder.DropTable(
                name: "stream_sessions");

            migrationBuilder.DropTable(
                name: "subtitles");

            migrationBuilder.DropTable(
                name: "sync_members");

            migrationBuilder.DropTable(
                name: "libraries");

            migrationBuilder.DropTable(
                name: "genres");

            migrationBuilder.DropTable(
                name: "tags");

            migrationBuilder.DropTable(
                name: "people");

            migrationBuilder.DropTable(
                name: "playlists");

            migrationBuilder.DropTable(
                name: "users");

            migrationBuilder.DropTable(
                name: "media_items");

            migrationBuilder.DropTable(
                name: "sync_rooms");
        }
    }
}
