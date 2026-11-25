using ChatApp.Services;

namespace ChatApp.Tests;

public sealed class PresenceTrackerTests
{
    private const int UserId = 1;
    private const string FirstConnection = "first";
    private const string SecondConnection = "second";
    private readonly PresenceTracker tracker = new();

    [Fact]
    public void FirstConnectionReturnsOnlineTransition()
    {
        var transition = tracker.AddConnection(FirstConnection, UserId);

        Assert.Equal(new PresenceTransition(UserId, IsOnline: true, Revision: 1), transition);
        Assert.Contains(UserId, tracker.GetSnapshot().OnlineUserIds);
    }

    [Fact]
    public void SecondConnectionForSameUserReturnsNull()
    {
        tracker.AddConnection(FirstConnection, UserId);

        Assert.Null(tracker.AddConnection(SecondConnection, UserId));
        Assert.Equal(UserId, tracker.GetUserId(SecondConnection));
    }

    [Fact]
    public void RemovingOneOfTwoConnectionsKeepsUserOnline()
    {
        tracker.AddConnection(FirstConnection, UserId);
        tracker.AddConnection(SecondConnection, UserId);

        Assert.Null(tracker.RemoveConnection(FirstConnection));
        Assert.Contains(UserId, tracker.GetSnapshot().OnlineUserIds);
        Assert.Null(tracker.GetUserId(FirstConnection));
    }

    [Fact]
    public void RemovingLastConnectionReturnsOfflineTransition()
    {
        tracker.AddConnection(FirstConnection, UserId);

        var transition = tracker.RemoveConnection(FirstConnection);

        Assert.Equal(new PresenceTransition(UserId, IsOnline: false, Revision: 2), transition);
        Assert.DoesNotContain(UserId, tracker.GetSnapshot().OnlineUserIds);
    }

    [Fact]
    public void RemovingUnknownConnectionReturnsNull()
    {
        Assert.Null(tracker.RemoveConnection("unknown"));
    }

    [Fact]
    public void AddingSameConnectionForSameUserIsIdempotent()
    {
        tracker.AddConnection(FirstConnection, UserId);

        Assert.Null(tracker.AddConnection(FirstConnection, UserId));
        Assert.Equal(new PresenceTransition(UserId, IsOnline: false, Revision: 2),
            tracker.RemoveConnection(FirstConnection));
    }

    [Fact]
    public void AddingSameConnectionForDifferentUserThrows()
    {
        tracker.AddConnection(FirstConnection, UserId);

        Assert.Throws<InvalidOperationException>(() => tracker.AddConnection(FirstConnection, UserId + 1));
        Assert.Equal(UserId, tracker.GetUserId(FirstConnection));
        Assert.DoesNotContain(UserId + 1, tracker.GetSnapshot().OnlineUserIds);
    }

    [Fact]
    public void SnapshotRevisionIncreasesOnlyOnTransitions()
    {
        Assert.Equal(0L, tracker.GetSnapshot().Revision);

        tracker.AddConnection(FirstConnection, UserId);
        Assert.Equal(1L, tracker.GetSnapshot().Revision);

        tracker.AddConnection(FirstConnection, UserId);
        Assert.Equal(1L, tracker.GetSnapshot().Revision);
        tracker.AddConnection(SecondConnection, UserId);
        Assert.Equal(1L, tracker.GetSnapshot().Revision);
        tracker.RemoveConnection("unknown");
        Assert.Equal(1L, tracker.GetSnapshot().Revision);
        tracker.RemoveConnection(FirstConnection);
        Assert.Equal(1L, tracker.GetSnapshot().Revision);

        tracker.RemoveConnection(SecondConnection);
        Assert.Equal(2L, tracker.GetSnapshot().Revision);
    }
}
