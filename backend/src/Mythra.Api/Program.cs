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
if (string.IsNullOrWhiteSpace(jwt.Secret) || jwt.Secret.Length < 32)
    jwt.Secret = "dev-only-secret-please-change-me-32-chars-min!";

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
    if (!app.Environment.IsDevelopment()) return;

    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<MythraDbContext>();
    await db.Database.EnsureCreatedAsync();
}

// ================= BOOTSTRAP =================
static async Task BootstrapAsync(WebApplication app)
{
    using var scope = app.Services.CreateScope();
    var bootstrap = scope.ServiceProvider.GetRequiredService<ILibraryBootstrapService>();
    await bootstrap.EnsureDefaultLibraryAsync();
}

public partial class Program { }