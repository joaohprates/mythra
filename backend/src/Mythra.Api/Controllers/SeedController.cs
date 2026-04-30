using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Mythra.Application.Abstractions.Persistence;
using Mythra.Domain.Libraries;
using Mythra.Domain.Media.Video;

namespace Mythra.Api.Controllers;

/// <summary>
/// Populates the database with a curated set of demo media items so the UI
/// is not empty on first launch.  All demo items have IMDB IDs so they can
/// be streamed via Vidsrc without any local media files.
/// </summary>
[ApiController]
[Route("api/v1/seed")]
[Authorize(Roles = "Admin,Manager")]
public sealed class SeedController(
    ILibraryRepository libraries,
    IMediaItemRepository mediaItems,
    IVideoRepository videos,
    IUnitOfWork uow) : ControllerBase
{
    /// <summary>
    /// Creates a "Demo" library and fills it with popular movies and TV episodes.
    /// Safe to call multiple times — returns 409 if the demo library already exists.
    /// </summary>
    [HttpPost("demo")]
    public async Task<IActionResult> SeedDemo(CancellationToken ct)
    {
        const string LibraryName = "Demo";

        if (await libraries.GetByNameAsync(LibraryName, ct) is not null)
            return Conflict(new { detail = "Demo library already exists." });

        var lib = new Library(LibraryName, LibraryKind.Video)
        {
            Description = "Curated demo content — stream instantly via external providers.",
        };
        await libraries.AddAsync(lib, ct);

        foreach (var m in DemoMovies)
        {
            var item = new VideoItem
            {
                LibraryId      = lib.Id,
                Title          = m.Title,
                Overview       = m.Overview,
                ReleaseDate    = new DateOnly(m.Year, 1, 1),
                Rating         = m.Rating,
                PosterPath     = m.PosterPath,
                BackdropPath   = m.BackdropPath,
                VideoKind      = VideoKind.Movie,
                ProviderImdbId = m.ImdbId,
                ProviderTmdbId = m.TmdbId,
                // FilePath intentionally null — streamed via external providers
            };
            foreach (var g in m.Genres)
                item.Genres.Add(new Domain.Media.Genre(g));
            await videos.AddAsync(item, ct);
        }

        foreach (var ep in DemoEpisodes)
        {
            var item = new VideoItem
            {
                LibraryId      = lib.Id,
                Title          = ep.Title,
                Overview       = ep.Overview,
                ReleaseDate    = new DateOnly(ep.Year, 1, 1),
                Rating         = ep.Rating,
                PosterPath     = ep.PosterPath,
                VideoKind      = VideoKind.Episode,
                SeasonNumber   = ep.Season,
                EpisodeNumber  = ep.Episode,
                ProviderImdbId = ep.ImdbId,
                ProviderTmdbId = ep.TmdbId,
            };
            foreach (var g in ep.Genres)
                item.Genres.Add(new Domain.Media.Genre(g));
            await videos.AddAsync(item, ct);
        }

        await uow.SaveChangesAsync(ct);

        return Ok(new
        {
            libraryId    = lib.Id,
            moviesSeeded = DemoMovies.Length,
            episodesSeeded = DemoEpisodes.Length,
            message      = "Demo library created successfully. Items can be streamed via Vidsrc.",
        });
    }

    // ── Demo data ─────────────────────────────────────────────────────────────

    private sealed record DemoMovie(
        string   Title,
        int      Year,
        double   Rating,
        string   ImdbId,
        string   TmdbId,
        string?  PosterPath,
        string?  BackdropPath,
        string   Overview,
        string[] Genres);

    private sealed record DemoEpisodeEntry(
        string   Title,
        int      Year,
        double   Rating,
        string   ImdbId,
        string   TmdbId,
        int      Season,
        int      Episode,
        string?  PosterPath,
        string   Overview,
        string[] Genres);

    private static readonly DemoMovie[] DemoMovies =
    [
        new(
            Title:       "The Dark Knight",
            Year:        2008,
            Rating:      9.0,
            ImdbId:      "tt0468569",
            TmdbId:      "155",
            PosterPath:  "https://image.tmdb.org/t/p/w500/qJ2tW6WMUDux911r6m7haRef0WH.jpg",
            BackdropPath:"https://image.tmdb.org/t/p/original/hkBaDkMWbLaf8B1lsWsKX7Ew3Xq.jpg",
            Overview:    "When the menace known as the Joker wreaks havoc and chaos on the people of Gotham, Batman must accept one of the greatest psychological and physical tests of his ability to fight injustice.",
            Genres:      ["Action", "Crime", "Drama", "Thriller"]
        ),
        new(
            Title:       "Inception",
            Year:        2010,
            Rating:      8.8,
            ImdbId:      "tt1375666",
            TmdbId:      "27205",
            PosterPath:  "https://image.tmdb.org/t/p/w500/oYuLEt3zVCKq57qu2F8dT7NIa6f.jpg",
            BackdropPath:"https://image.tmdb.org/t/p/original/s3TBrRGB1iav7gFOCNx3H31MoES.jpg",
            Overview:    "A thief who steals corporate secrets through the use of dream-sharing technology is given the inverse task of planting an idea into the mind of a C.E.O.",
            Genres:      ["Action", "Science Fiction", "Adventure"]
        ),
        new(
            Title:       "Interstellar",
            Year:        2014,
            Rating:      8.7,
            ImdbId:      "tt0816692",
            TmdbId:      "157336",
            PosterPath:  "https://image.tmdb.org/t/p/w500/gEU2QniE6E77NI6lCU6MxlNBvIx.jpg",
            BackdropPath:"https://image.tmdb.org/t/p/original/xJHokMbljvjADYdit5fK5VQsXEG.jpg",
            Overview:    "The adventures of a group of explorers who make use of a newly discovered wormhole to surpass the limitations on human space travel and conquer the vast distances involved in an interstellar voyage.",
            Genres:      ["Adventure", "Drama", "Science Fiction"]
        ),
        new(
            Title:       "The Shawshank Redemption",
            Year:        1994,
            Rating:      9.3,
            ImdbId:      "tt0111161",
            TmdbId:      "278",
            PosterPath:  "https://image.tmdb.org/t/p/w500/q6y0Go1tsGEsmtFryDOJo3dEmqu.jpg",
            BackdropPath:"https://image.tmdb.org/t/p/original/kXfqcdQKsToO0OUXHcrrNCHDBzO.jpg",
            Overview:    "Framed in the 1940s for the double murder of his wife and her lover, upstanding banker Andy Dufresne begins a new life at the Shawshank State Penitentiary.",
            Genres:      ["Drama", "Crime"]
        ),
        new(
            Title:       "Pulp Fiction",
            Year:        1994,
            Rating:      8.9,
            ImdbId:      "tt0110912",
            TmdbId:      "680",
            PosterPath:  "https://image.tmdb.org/t/p/w500/d5iIlFn5s0ImszYzBPb8JPIfbXD.jpg",
            BackdropPath:"https://image.tmdb.org/t/p/original/suaEOtk1N1sgg2MTM7oZd2cfVp3.jpg",
            Overview:    "A burger-loving hit man, his philosophical partner, a drug-addled gangster's moll and a washed-up boxer converge in this sprawling, comedic crime caper.",
            Genres:      ["Thriller", "Crime"]
        ),
        new(
            Title:       "The Matrix",
            Year:        1999,
            Rating:      8.7,
            ImdbId:      "tt0133093",
            TmdbId:      "603",
            PosterPath:  "https://image.tmdb.org/t/p/w500/f89U3ADr1oiB1s9GkdPOEpXUk5H.jpg",
            BackdropPath:"https://image.tmdb.org/t/p/original/fNG7i7RqMErkcqhohV2a6cV1Ehy.jpg",
            Overview:    "Set in the 22nd century, The Matrix tells the story of a computer hacker who joins a group of underground insurgents fighting the vast and powerful computers who now rule the earth.",
            Genres:      ["Action", "Science Fiction"]
        ),
        new(
            Title:       "Fight Club",
            Year:        1999,
            Rating:      8.8,
            ImdbId:      "tt0137523",
            TmdbId:      "550",
            PosterPath:  "https://image.tmdb.org/t/p/w500/pB8BM7pdSp6B6Ih7QZ4DrQ3PmJK.jpg",
            BackdropPath:"https://image.tmdb.org/t/p/original/hZkgoQYus5vegHoetLkCJzb17zJ.jpg",
            Overview:    "A ticking-time-bomb insomniac and a slippery soap salesman channel primal male aggression into a shocking new form of therapy.",
            Genres:      ["Drama", "Thriller", "Crime"]
        ),
        new(
            Title:       "Forrest Gump",
            Year:        1994,
            Rating:      8.8,
            ImdbId:      "tt0109830",
            TmdbId:      "13",
            PosterPath:  "https://image.tmdb.org/t/p/w500/arw2vcBveWOVZr6pxd9XTd1TdQa.jpg",
            BackdropPath:"https://image.tmdb.org/t/p/original/qdIMHd4sEfJSckfVJfKQvisL02a.jpg",
            Overview:    "A man with a low IQ has accomplished great things in his life and been present during significant historic events — in each case, far exceeding what anyone imagined he could do.",
            Genres:      ["Comedy", "Drama", "Romance"]
        ),
        new(
            Title:       "The Godfather",
            Year:        1972,
            Rating:      9.2,
            ImdbId:      "tt0068646",
            TmdbId:      "238",
            PosterPath:  "https://image.tmdb.org/t/p/w500/3bhkrj58Vtu7enYsLLeHjTniEDO.jpg",
            BackdropPath:"https://image.tmdb.org/t/p/original/tmU7GeKVybMWFbH9KHiaN3gFCGP.jpg",
            Overview:    "Spanning the years 1945 to 1955, a chronicle of the fictional Italian-American Corleone crime family. When organized crime family patriarch Vito Corleone barely survives an attempt on his life, his youngest son Michael steps in to take care of the would-be killers.",
            Genres:      ["Drama", "Crime"]
        ),
        new(
            Title:       "Parasite",
            Year:        2019,
            Rating:      8.5,
            ImdbId:      "tt6751668",
            TmdbId:      "496243",
            PosterPath:  "https://image.tmdb.org/t/p/w500/7IiTTgloJzvGI1TAYymCfbfl3vT.jpg",
            BackdropPath:"https://image.tmdb.org/t/p/original/TU9NIjwzjoKPwQHoHshkFcQUCG.jpg",
            Overview:    "All unemployed, Ki-taek's family takes peculiar interest in the wealthy and well-educated Park family. Soon, they find themselves entangled in an unexpected incident.",
            Genres:      ["Comedy", "Thriller", "Drama"]
        ),
        new(
            Title:       "Se7en",
            Year:        1995,
            Rating:      8.6,
            ImdbId:      "tt0114369",
            TmdbId:      "807",
            PosterPath:  "https://image.tmdb.org/t/p/w500/69Sns8WoET6CfaYlIkHbla4l7nC.jpg",
            BackdropPath:"https://image.tmdb.org/t/p/original/6bbZ6XyvgfjhQwbplnUh1LSj1ky.jpg",
            Overview:    "Two homicide detectives are on a desperate hunt for a serial killer whose crimes are based on the \"seven deadly sins\" in this dark and haunting film.",
            Genres:      ["Crime", "Mystery", "Thriller"]
        ),
        new(
            Title:       "Goodfellas",
            Year:        1990,
            Rating:      8.7,
            ImdbId:      "tt0099685",
            TmdbId:      "769",
            PosterPath:  "https://image.tmdb.org/t/p/w500/6QMSLvU5ziIL2T6VrkaKzN2YkMz.jpg",
            BackdropPath:"https://image.tmdb.org/t/p/original/sw7mordbZxgITU877yTpZCud90M.jpg",
            Overview:    "The story of Henry Hill and his life in the mob, covering his relationship with his wife Karen Hill and his mob partners Jimmy Conway and Tommy DeVito.",
            Genres:      ["Drama", "Crime"]
        ),
    ];

    private static readonly DemoEpisodeEntry[] DemoEpisodes =
    [
        new(
            Title:     "Breaking Bad – Pilot",
            Year:      2008,
            Rating:    9.5,
            ImdbId:    "tt0903747",
            TmdbId:    "1396",
            Season:    1,
            Episode:   1,
            PosterPath:"https://image.tmdb.org/t/p/w500/ggFHVNu6YYI5L9pCfOacjizRGt.jpg",
            Overview:  "Walter White, a chemistry teacher diagnosed with inoperable lung cancer, turns to manufacturing and selling methamphetamine with his former student Jesse Pinkman.",
            Genres:    ["Drama", "Crime", "Thriller"]
        ),
        new(
            Title:     "Stranger Things – Chapter One: The Vanishing of Will Byers",
            Year:      2016,
            Rating:    9.0,
            ImdbId:    "tt4574334",
            TmdbId:    "66732",
            Season:    1,
            Episode:   1,
            PosterPath:"https://image.tmdb.org/t/p/w500/49WJfeN0moxb9IPfGn8AIqMGskD.jpg",
            Overview:  "On his way home from a friend's house, young Will sees something terrifying. Nearby, a sinister secret lurks in the depths of a government lab.",
            Genres:    ["Drama", "Science Fiction", "Mystery"]
        ),
    ];
}
