namespace ChatApp.Services;

public sealed class PresenceTracker
{
    private readonly object gate = new();
    private readonly Dictionary<string, int> usersByConnection = new(StringComparer.Ordinal);
    private readonly Dictionary<int, HashSet<string>> connectionsByUser = new();
    private long revision;

    public PresenceTransition? AddConnection(string connectionId, int userId)
    {
        lock (gate)
        {
            if (usersByConnection.TryGetValue(connectionId, out var existingUserId))
            {
                if (existingUserId != userId)
                {
                    throw new InvalidOperationException("This connection is already joined as another user.");
                }

                return null;
            }

            usersByConnection.Add(connectionId, userId);
            if (connectionsByUser.TryGetValue(userId, out var connections))
            {
                connections.Add(connectionId);
                return null;
            }

            connectionsByUser.Add(userId, new HashSet<string>(StringComparer.Ordinal) { connectionId });
            return new PresenceTransition(userId, IsOnline: true, Revision: ++revision);
        }
    }

    public PresenceTransition? RemoveConnection(string connectionId)
    {
        lock (gate)
        {
            if (!usersByConnection.Remove(connectionId, out var userId))
            {
                return null;
            }

            var connections = connectionsByUser[userId];
            connections.Remove(connectionId);
            if (connections.Count > 0)
            {
                return null;
            }

            connectionsByUser.Remove(userId);
            return new PresenceTransition(userId, IsOnline: false, Revision: ++revision);
        }
    }

    public int? GetUserId(string connectionId)
    {
        lock (gate)
        {
            return usersByConnection.TryGetValue(connectionId, out var userId) ? userId : null;
        }
    }

    public PresenceSnapshot GetSnapshot()
    {
        lock (gate)
        {
            return new PresenceSnapshot(revision, connectionsByUser.Keys.ToHashSet());
        }
    }
}

public sealed record PresenceTransition(int UserId, bool IsOnline, long Revision);

public sealed record PresenceSnapshot(long Revision, IReadOnlySet<int> OnlineUserIds);
