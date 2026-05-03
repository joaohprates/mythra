export type MediaKind = "Video" | "Manga" | "Book" | "Audio";
export type LibraryKind = "Video" | "Anime" | "Manga" | "Book" | "Audiobook" | "Music" | "General" | "Image";
export type VideoKind = "Movie" | "Series" | "Season" | "Episode" | "Anime" | "AnimeMovie" | "Special" | "Trailer" | "Other";
export type BookFormat = "Epub" | "Pdf" | "Mobi" | "Azw3" | "Cbz";
export type AudioKind = "Audiobook" | "Podcast" | "Music" | "Soundtrack";
export type MangaReadingDirection = "LeftToRight" | "RightToLeft" | "Vertical";

export interface Profile {
  id: string;
  name: string;
  avatarPath?: string | null;
  isKidFriendly: boolean;
  theme: string;
  showAdultContent?: boolean;
}

export interface User {
  id: string;
  email: string;
  username: string;
  role: "User" | "Manager" | "Admin";
  avatarPath?: string | null;
  preferredLanguage: string;
  profiles: Profile[];
}

export interface AuthResponse {
  accessToken: string;
  accessExpiresAt: string;
  refreshToken: string;
  refreshExpiresAt: string;
  user: User;
}

export interface MediaItem {
  id: string;
  kind: MediaKind;
  libraryId: string;
  title: string;
  originalTitle?: string | null;
  overview?: string | null;
  tagline?: string | null;
  posterPath?: string | null;
  backdropPath?: string | null;
  thumbPath?: string | null;
  releaseDate?: string | null;
  year?: number | null;
  rating?: number | null;
  language?: string | null;
  genres: string[];
  tags: string[];
  createdAt: string;
  isAdult?: boolean;
}

export interface VideoItemDetail extends MediaItem {
  videoKind: VideoKind;
  isAnime: boolean;
  duration?: string | null;
  width?: number | null;
  height?: number | null;
  resolutionLabel: string;
  seasonNumber?: number | null;
  episodeNumber?: number | null;
  videoCodec?: string | null;
  audioCodec?: string | null;
  bitrate?: number | null;
  subtitles: Subtitle[];
  audioTracks: AudioTrack[];
  chapterMarkers: ChapterMarker[];
  /** True when a local file exists on disk; false for virtual/external-only items. */
  hasFile: boolean;
  /** IMDB title ID, e.g. "tt1234567". Present when metadata was fetched from IMDB/TMDB. */
  imdbId?: string | null;
  /** Parent series ID for episodes. */
  parentId?: string | null;
}

export interface Subtitle {
  id: string;
  languageCode: string;
  displayName?: string | null;
  format: string;
  kind: string;
  isDefault: boolean;
  isForced: boolean;
}

export interface AudioTrack {
  id: string;
  languageCode: string;
  displayName?: string | null;
  streamIndex: number;
  codec: string;
  channels: number;
  channelLayout: string;
  isDefault: boolean;
  isCommentary: boolean;
}

export interface ChapterMarker {
  id: string;
  kind: string;
  label?: string | null;
  start: string;
  end?: string | null;
  thumbPath?: string | null;
}

export interface MangaItemDetail extends MediaItem {
  author?: string | null;
  artist?: string | null;
  status?: string | null;
  readingDirection: MangaReadingDirection;
  totalChapters?: number | null;
  totalVolumes?: number | null;
  chapters: MangaChapter[];
  /** True when the item has no local files — metadata-only from an external provider. */
  isExternal: boolean;
  /** True when this item contains adult/explicit content. */
  isAdult: boolean;
  /** Link to the AniList page for this manga. */
  anilistUrl?: string | null;
  /** Link to the MangaDex page for this manga. */
  mangaDexUrl?: string | null;
}

export interface MangaChapter {
  id: string;
  volumeNumber?: number | null;
  chapterNumber: number;
  title?: string | null;
  pageCount: number;
  coverPath?: string | null;
  releaseDate?: string | null;
}

export interface BookItemDetail extends MediaItem {
  author?: string | null;
  publisher?: string | null;
  isbn?: string | null;
  series?: string | null;
  seriesIndex?: number | null;
  format: BookFormat;
  pageCount?: number | null;
  wordCount?: number | null;
  chapters: BookChapter[];
  isExternal: boolean;
}

export interface BookChapter {
  id: string;
  order: number;
  title: string;
  anchor?: string | null;
  startPage?: number | null;
  endPage?: number | null;
}

export interface AudioItemDetail extends MediaItem {
  author?: string | null;
  narrator?: string | null;
  series?: string | null;
  seriesIndex?: number | null;
  audioKind: AudioKind;
  duration?: string | null;
  coverPath?: string | null;
  chapters: AudioChapter[];
  isExternal: boolean;
}

export interface AudioChapter {
  id: string;
  order: number;
  title: string;
  start: string;
  duration: string;
}

export interface LibraryFolder {
  id: string;
  path: string;
  isActive: boolean;
}

export interface Library {
  id: string;
  name: string;
  kind: LibraryKind;
  description?: string | null;
  isEnabled: boolean;
  isSystem: boolean;
  autoRefreshMetadata: boolean;
  lastScannedAt?: string | null;
  folderCount: number;
  itemCount?: number | null;
  effectiveExtensions?: string[];
  folders?: LibraryFolder[];
}

// ── Notifications ────────────────────────────────────────────────────────────
export type NotificationKind =
  | "MediaAdded" | "ScanCompleted" | "ScanFailed"
  | "Recommendation" | "ImportCompleted" | "ProviderUnhealthy" | "System";

export interface Notification {
  id: string;
  kind: NotificationKind;
  title: string;
  body?: string | null;
  actionUrl?: string | null;
  imageUrl?: string | null;
  isRead: boolean;
  createdAt: string;
}

export interface NotificationListDto {
  items: Notification[];
  unreadCount: number;
}

// ── Discover ─────────────────────────────────────────────────────────────────
export interface DiscoverItem {
  externalId: string;
  providerKind: string;
  title: string;
  originalTitle?: string | null;
  year?: number | null;
  rating?: number | null;
  overview?: string | null;
  posterPath?: string | null;
  backdropPath?: string | null;
  genres: string[];
  alreadyImported: boolean;
  existingItemId?: string | null;
  isAdult: boolean;
}

export interface DiscoverResult {
  items: DiscoverItem[];
  total: number;
  skip: number;
  take: number;
}

export interface ImportResultDto {
  id: string;
  title: string;
  mediaKind: string;
  hasFile: boolean;
  fileStatus: string;
  posterPath?: string | null;
  libraryId: string;
  watchUrl: string;
}

export interface PagedResult<T> {
  items: T[];
  total: number;
  skip: number;
  take: number;
}

export interface PlaybackProgress {
  mediaItemId: string;
  position: string;
  duration?: string | null;
  isCompleted: boolean;
  lastWatchedAt: string;
  percentComplete: number;
  playbackSpeed: number;
}

export interface ReadingProgressDto {
  mediaItemId: string;
  currentChapterId?: string | null;
  currentPage?: number | null;
  totalPages?: number | null;
  cfiLocator?: string | null;
  percentComplete: number;
  isCompleted: boolean;
  lastReadAt: string;
}

export interface StreamSession {
  sessionId: string;
  sessionToken: string;
  mode: "DirectPlay" | "Remux" | "Transcode";
  state: string;
  playlistUrl: string;
  width?: number | null;
  height?: number | null;
  bitrate?: number | null;
}

export interface SearchHit {
  id: string;
  kind: MediaKind;
  title: string;
  subtitle?: string | null;
  posterPath?: string | null;
  year?: number | null;
  rating?: number | null;
  relevance: number;
}

export interface SearchResult {
  hits: SearchHit[];
  total: number;
  elapsedMs: number;
}

// ── Playlists ────────────────────────────────────────────────────────────────

export interface PlaylistItem {
  id: string;
  mediaItemId: string;
  title: string;
  kind: MediaKind;
  posterPath?: string | null;
  rating?: number | null;
  year?: number | null;
  order: number;
  addedAt: string;
}

export interface Playlist {
  id: string;
  profileId: string;
  name: string;
  description?: string | null;
  isPublic: boolean;
  coverImagePath?: string | null;
  itemCount: number;
  createdAt: string;
  updatedAt?: string | null;
}

export interface PlaylistDetail extends Playlist {
  items: PlaylistItem[];
}

// ── Favorites ────────────────────────────────────────────────────────────────

export interface FavoriteItem {
  id: string;
  profileId: string;
  mediaItemId: string;
  addedAt: string;
  mediaItem?: MediaItem;
}

export interface FavoriteStatus {
  isFavorite: boolean;
}

// ── Statistics ───────────────────────────────────────────────────────────────

export interface GenreStat {
  genre: string;
  count: number;
  percentage: number;
}

export interface WeeklyActivity {
  week: string;
  itemsWatched: number;
  watchTime: string; // ISO 8601 duration
}

export interface MediaKindBreakdown {
  kind: MediaKind;
  count: number;
  percentage: number;
}

export interface ProfileStatistics {
  profileId: string;
  totalWatchTime: string;
  totalReadTime: string;
  totalItemsWatched: number;
  totalItemsRead: number;
  totalItemsCompleted: number;
  topGenres: GenreStat[];
  weeklyActivity: WeeklyActivity[];
  kindBreakdown: MediaKindBreakdown[];
  generatedAt: string;
}
