using System.Net.WebSockets;
using System.Text;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Mythra.Api.Auth;
using Mythra.Api.Middleware;
using Mythra.Api.WebSockets;
using Mythra.Application;
using Mythra.Application.Abstractions.Auth;
using Mythra.Application.Services.Libraries;
using Mythra.Application.Services.SyncPlay;
using Mythra.Infrastructure;
using Mythra.Infrastructure.Auth;
using Mythra.Infrastructure.Persistence;
using Prometheus;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// ================= LOGGING =================
builder.Host.UseSerilog((context, services, cfg) => cfg
    .ReadFrom.Configuration(context.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext()
    .Enrich.WithEnvironmentName()
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {CorrelationId} {Message:lj}{NewLine}{Exception}")
    .WriteTo.File("logs/mythra-.log", rollingInterval: RollingInterval.Day, retainedFileCountLimit: 14));

// ================= SERVICES =================
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddApplication();

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, HttpCurrentUser>();
builder.Services.AddSingleton<SyncPlayHub>();

builder.Services
    .AddControllers()
    .AddJsonOptions(opts =>
    {
        opts.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
        opts.JsonSerializerOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
        opts.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
    });

builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddFluentValidationClientsideAdapters();

// ================= CORS =================
builder.Services.AddCors(opt =>
{
    opt.AddDefaultPolicy(p => p
        .WithOrigins(builder.Configuration.GetSection("Cors:Origins").Get<string[]>() ?? ["http://localhost:3000"])
        .AllowCredentials()
        .AllowAnyHeader()
        .AllowAnyMethod());
});

// ================= JWT =================
var jwt = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions();
const string DevFallbackJwtSecret = "mythra-development-only-secret-do-not-use-in-production-0000000000";
if (string.IsNullOrWhiteSpace(jwt.Secret))
{
    if (builder.Environment.IsProduction())
    {
        throw new InvalidOperationException(
            "Jwt:Secret is not configured. Set the 'Jwt__Secret' environment variable (or 'Jwt:Secret' in configuration) to a value of at least 32 characters before starting in Production.");
    }
    jwt.Secret = DevFallbackJwtSecret;
    Console.WriteLine("[Mythra][WARN] Jwt:Secret missing — using deterministic development fallback. Configure 'Jwt__Secret' for non-dev runs.");
}
if (jwt.Secret.Length < 32)
{
    throw new InvalidOperationException(
        $"Jwt:Secret is too short ({jwt.Secret.Length} chars). It must be at least 32 characters. Set the 'Jwt__Secret' environment variable.");
}
// Push the resolved secret back into configuration so downstream services (token issuer) see it.
builder.Configuration["Jwt:Secret"] = jwt.Secret;

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opts =>
    {
        opts.RequireHttpsMetadata = false;
        opts.SaveToken = true;
        opts.TokenValidationParameters = new TokenValidationParameters
        {
            ValidIssuer = jwt.Issuer,
            ValidAudience = jwt.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Secret)),
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ClockSkew = TimeSpan.FromSeconds(15),
        };
    });

builder.Services.AddAuthorization();

// ================= SWAGGER =================
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Mythra API",
        Version = "v1",
        Description = "Cinematic personal media universe — REST + WebSocket APIs.",
    });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT bearer token. Format: \"Bearer {token}\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
    });

    c.OperationFilter<Mythra.Api.Common.SwaggerBearerOperationFilter>();
});

// ================= HEALTHCHECK =================
builder.Services.AddHealthChecks()
    .AddCheck("db", () =>
    {
        // Lightweight startup check; full DB validation is deferred to middleware
        return Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy("Database configured");
    });

var app = builder.Build();

// ================= DATABASE INIT =================
await EnsureDatabaseAsync(app);

// ================= BOOTSTRAP =================
await BootstrapAsync(app);

// ================= MIDDLEWARE =================
app.UseSerilogRequestLogging();
app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<ErrorHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Mythra v1");
        c.RoutePrefix = "swagger";
    });
}

app.UseCors();

app.UseWebSockets(new WebSocketOptions
{
    KeepAliveInterval = TimeSpan.FromSeconds(15)
});

app.UseAuthentication();
app.UseAuthorization();

// ================= METRICS =================
app.UseHttpMetrics();
app.MapMetrics("/metrics");

// ================= HEALTH ENDPOINT =================
app.MapHealthChecks("/health");

// ================= CONTROLLERS =================
app.MapControllers();

// ================= WEBSOCKET =================
app.Map("/ws/sync/{code}", async (HttpContext ctx, string code, SyncPlayHub hub, ISyncPlayService sync, ICurrentUser cu) =>
{
    if (!ctx.WebSockets.IsWebSocketRequest)
    {
        ctx.Response.StatusCode = StatusCodes.Status400BadRequest;
        return;
    }

    if (!cu.IsAuthenticated || cu.UserId is null)
    {
        ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
        return;
    }

    using var ws = await ctx.WebSockets.AcceptWebSocketAsync();
    await hub.HandleAsync(ws, code, cu.UserId.Value, sync, ctx.RequestAborted);

}).RequireAuthorization();

app.Run();

// ================= DB INIT =================
static async Task EnsureDatabaseAsync(WebApplication app)
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<MythraDbContext>();

    if (!app.Environment.IsProduction())
    {
        // Create schema for new databases; for existing ones this is a no-op.
        await db.Database.EnsureCreatedAsync();
        // Idempotently apply incremental changes that EnsureCreated misses on existing DBs.
        await ApplySchemaDeltasAsync(db);
    }
    else
    {
        await db.Database.MigrateAsync();
    }
}

static async Task ApplySchemaDeltasAsync(MythraDbContext db)
{
    // ShowAdultContent on profiles — safe to ignore if already present.
    try
    {
        await db.Database.ExecuteSqlRawAsync(
            "ALTER TABLE profiles ADD COLUMN ShowAdultContent INTEGER NOT NULL DEFAULT 0");
    }
    catch { /* column already exists */ }

    // favorite_items table and indexes — IF NOT EXISTS makes these idempotent.
    await db.Database.ExecuteSqlRawAsync(@"
        CREATE TABLE IF NOT EXISTS favorite_items (
            Id TEXT NOT NULL,
            ProfileId TEXT NOT NULL,
            MediaItemId TEXT NOT NULL,
            AddedAt TEXT NOT NULL,
            CreatedAt INTEGER NOT NULL,
            UpdatedAt INTEGER NULL,
            CONSTRAINT PK_favorite_items PRIMARY KEY (Id)
        )");

    await db.Database.ExecuteSqlRawAsync(@"
        CREATE UNIQUE INDEX IF NOT EXISTS IX_favorite_items_ProfileId_MediaItemId
        ON favorite_items (ProfileId, MediaItemId)");

    await db.Database.ExecuteSqlRawAsync(@"
        CREATE INDEX IF NOT EXISTS IX_favorite_items_ProfileId
        ON favorite_items (ProfileId)");

    // playlists / playlist_items — created here so existing DBs predating the feature get the tables.
    await db.Database.ExecuteSqlRawAsync(@"
        CREATE TABLE IF NOT EXISTS playlists (
            Id TEXT NOT NULL CONSTRAINT PK_playlists PRIMARY KEY,
            ProfileId TEXT NOT NULL,
            Name TEXT NOT NULL,
            Description TEXT NULL,
            IsPublic INTEGER NOT NULL DEFAULT 0,
            CoverImagePath TEXT NULL,
            CreatedAt INTEGER NOT NULL,
            UpdatedAt INTEGER NULL
        )");

    await db.Database.ExecuteSqlRawAsync(@"
        CREATE INDEX IF NOT EXISTS IX_playlists_ProfileId ON playlists (ProfileId)");

    await db.Database.ExecuteSqlRawAsync(@"
        CREATE TABLE IF NOT EXISTS playlist_items (
            Id TEXT NOT NULL CONSTRAINT PK_playlist_items PRIMARY KEY,
            PlaylistId TEXT NOT NULL,
            MediaItemId TEXT NOT NULL,
            ""Order"" INTEGER NOT NULL,
            AddedAt INTEGER NOT NULL,
            CreatedAt INTEGER NOT NULL,
            UpdatedAt INTEGER NULL,
            CONSTRAINT FK_playlist_items_playlists_PlaylistId
                FOREIGN KEY (PlaylistId) REFERENCES playlists (Id) ON DELETE CASCADE
        )");

    await db.Database.ExecuteSqlRawAsync(@"
        CREATE INDEX IF NOT EXISTS IX_playlist_items_PlaylistId_Order
        ON playlist_items (PlaylistId, ""Order"")");

    await db.Database.ExecuteSqlRawAsync(@"
        CREATE UNIQUE INDEX IF NOT EXISTS IX_playlist_items_PlaylistId_MediaItemId
        ON playlist_items (PlaylistId, MediaItemId)");
}

// ================= BOOTSTRAP =================
static async Task BootstrapAsync(WebApplication app)
{
    using var scope = app.Services.CreateScope();
    var bootstrap = scope.ServiceProvider.GetRequiredService<ILibraryBootstrapService>();
    await bootstrap.EnsureDefaultLibraryAsync();
}

public partial class Program { }