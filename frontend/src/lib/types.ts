export type MediaKind = "Video" | "Manga" | "Book" | "Audio";
export type LibraryKind = "Video" | "Anime" | "Manga" | "Book" | "Audiobook" | "Music";
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
}

export interface AudioChapter {
  id: string;
  order: number;
  title: string;
  start: string;
  duration: string;
}

export interface Library {
  id: string;
  name: string;
  kind: LibraryKind;
  description?: string | null;
  isEnabled: boolean;
  autoRefreshMetadata: boolean;
  lastScannedAt?: string | null;
  folderCount: number;
  itemCount?: number | null;
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
