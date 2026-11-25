using ChatApp.Services;
using Microsoft.AspNetCore.SignalR;

namespace ChatApp.Hubs;

public sealed class ChatHub(ChatService chat, PresenceTracker presence) : Hub
{
    private const string JoinedGroup = "joined";
    private static readonly object DisplayNameKey = new();

    public async Task<RosterSnapshot> GetRoster()
    {
        // Capture the revision before the query so events for newly saved users can supersede this snapshot.
        var snapshot = presence.GetSnapshot();
        var users = await chat.GetUsersAsync(Context.ConnectionAborted);
        return new RosterSnapshot(snapshot.Revision, users.Select(user => new RosterUser(
            user.UserId, user.DisplayName, snapshot.OnlineUserIds.Contains(user.UserId))).ToArray());
    }

    public async Task<KnownUser> Join(string? name)
    {
        var connectionId = Context.ConnectionId;
        var user = await WithClientErrors(() => chat.JoinAsync(
            name, Context.ConnectionAborted, boundUserId: presence.GetUserId(connectionId)));

        await Groups.AddToGroupAsync(connectionId, JoinedGroup, Context.ConnectionAborted);
        Context.Items[DisplayNameKey] = user.DisplayName;
        var transition = presence.AddConnection(connectionId, user.Id);
        if (transition is not null)
        {
            await BroadcastPresence(transition, user.DisplayName);
        }

        return new KnownUser(user.Id, user.DisplayName);
    }

    public Task<HistoryPage> GetHistory(int? beforeId, int? afterId)
    {
        RequireJoinedUser();
        return WithClientErrors(() => chat.GetHistoryAsync(beforeId, afterId, Context.ConnectionAborted));
    }

    public async Task SendMessage(string? text)
    {
        var userId = RequireJoinedUser();
        var saved = await WithClientErrors(() => chat.SaveMessageAsync(userId, text, Context.ConnectionAborted));
        await Clients.Group(JoinedGroup).SendAsync("MessageReceived", saved);
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var transition = presence.RemoveConnection(Context.ConnectionId);
        if (transition is not null && Context.Items[DisplayNameKey] is string displayName)
        {
            await BroadcastPresence(transition, displayName);
        }

        await base.OnDisconnectedAsync(exception);
    }

    private int RequireJoinedUser() => presence.GetUserId(Context.ConnectionId)
        ?? throw new HubException("Join the chat first.");

    private Task BroadcastPresence(PresenceTransition transition, string displayName) =>
        Clients.All.SendAsync("PresenceChanged", new PresenceUpdate(
            transition.UserId, displayName, transition.IsOnline, transition.Revision));

    private static async Task<T> WithClientErrors<T>(Func<Task<T>> operation)
    {
        try
        {
            return await operation();
        }
        catch (ArgumentException exception)
        {
            throw new HubException(exception.Message);
        }
    }
}

public sealed record RosterSnapshot(long Revision, IReadOnlyList<RosterUser> Users);

public sealed record RosterUser(int UserId, string DisplayName, bool IsOnline);

public sealed record PresenceUpdate(int UserId, string DisplayName, bool IsOnline, long Revision);
