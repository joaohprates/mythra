using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using Mythra.Application.Dtos.SyncPlay;
using Mythra.Application.Services.SyncPlay;
using Mythra.Domain.SyncPlay;

namespace Mythra.Api.WebSockets;

public sealed class SyncPlayHub
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<Guid, WebSocket>> _rooms = new();
    private readonly ILogger<SyncPlayHub> _log;

    public SyncPlayHub(ILogger<SyncPlayHub> log) => _log = log;

    public async Task HandleAsync(WebSocket socket, string roomCode, Guid userId, ISyncPlayService syncService, CancellationToken ct)
    {
        var members = _rooms.GetOrAdd(roomCode, _ => new ConcurrentDictionary<Guid, WebSocket>());
        members[userId] = socket;

        var buffer = new byte[8192];
        try
        {
            while (socket.State == WebSocketState.Open && !ct.IsCancellationRequested)
            {
                var receive = await socket.ReceiveAsync(buffer, ct);
                if (receive.MessageType == WebSocketMessageType.Close)
                {
                    await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "client closed", ct);
                    break;
                }
                var json = Encoding.UTF8.GetString(buffer, 0, receive.Count);
                var msg = JsonSerializer.Deserialize<InboundMessage>(json, Json);
                if (msg is null) continue;
                await ProcessAsync(msg, roomCode, userId, members, syncService, ct);
            }
        }
        catch (WebSocketException ex)
        {
            _log.LogDebug(ex, "WebSocket closed for {Code}/{User}", roomCode, userId);
        }
        finally
        {
            members.TryRemove(userId, out _);
            if (members.IsEmpty) _rooms.TryRemove(roomCode, out _);
        }
    }

    private async Task ProcessAsync(
        InboundMessage msg,
        string roomCode,
        Guid userId,
        ConcurrentDictionary<Guid, WebSocket> members,
        ISyncPlayService syncService,
        CancellationToken ct)
    {
        switch (msg.Type)
        {
            case "command":
                if (msg.Command is not null)
                {
                    var cmd = new SyncCommandDto(msg.Command.Kind, msg.Command.Position, msg.Command.MediaItemId);
                    await syncService.ApplyCommandAsync(userId, roomCode, cmd, ct);
                    await BroadcastAsync(members, new OutboundMessage("command", null, msg.Command), ct);
                }
                break;

            case "ping":
                if (msg.Ping is not null)
                {
                    await syncService.PingAsync(userId, roomCode, msg.Ping.LatencyMs, msg.Ping.Position, ct);
                    await SendAsync(members[userId], new OutboundMessage("pong", null, null) with { Pong = DateTimeOffset.UtcNow }, ct);
                }
                break;

            case "presence":
                await BroadcastAsync(members, new OutboundMessage("presence", new PresencePayload(userId, msg.Presence?.Status ?? "active"), null), ct);
                break;
        }
    }

    private static async Task BroadcastAsync(ConcurrentDictionary<Guid, WebSocket> members, OutboundMessage msg, CancellationToken ct)
    {
        var bytes = JsonSerializer.SerializeToUtf8Bytes(msg, Json);
        foreach (var ws in members.Values)
        {
            if (ws.State == WebSocketState.Open)
                await ws.SendAsync(bytes, WebSocketMessageType.Text, endOfMessage: true, ct);
        }
    }

    private static async Task SendAsync(WebSocket socket, OutboundMessage msg, CancellationToken ct)
    {
        var bytes = JsonSerializer.SerializeToUtf8Bytes(msg, Json);
        if (socket.State == WebSocketState.Open)
            await socket.SendAsync(bytes, WebSocketMessageType.Text, endOfMessage: true, ct);
    }

    private sealed record InboundMessage(string Type, CommandPayload? Command, PingPayload? Ping, PresencePayload? Presence);
    private sealed record CommandPayload(SyncCommandKind Kind, TimeSpan Position, Guid? MediaItemId);
    private sealed record PingPayload(double LatencyMs, TimeSpan Position);
    private sealed record PresencePayload(Guid UserId, string Status);
    private sealed record OutboundMessage(string Type, PresencePayload? Presence, CommandPayload? Command)
    {
        public DateTimeOffset? Pong { get; init; }
    }
}
