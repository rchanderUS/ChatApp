using ChatApp.Data;
using ChatApp.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace ChatApp.Tests;

public sealed class ChatServiceTests : IAsyncLifetime
{
    private readonly SqliteConnection connection = new("Data Source=:memory:;Foreign Keys=True");

    public async Task InitializeAsync()
    {
        await connection.OpenAsync();
        await using var database = CreateContext();
        await database.Database.EnsureCreatedAsync();
    }

    public Task DisposeAsync() => connection.DisposeAsync().AsTask();

    [Fact]
    public async Task JoinNormalizesCaseAndWhitespaceAndPreservesFirstDisplayName()
    {
        await using var database = CreateContext();
        var service = new ChatService(database);

        var first = await service.JoinAsync("DJ");
        var lowercase = await service.JoinAsync("dj");
        var padded = await service.JoinAsync(" DJ ");

        Assert.Equal(first.Id, lowercase.Id);
        Assert.Equal(first.Id, padded.Id);
        Assert.Equal("DJ", lowercase.DisplayName);
        Assert.Equal("DJ", padded.DisplayName);
        await using var reader = CreateContext();
        var persisted = await reader.Users.SingleAsync();
        Assert.Equal(first.Id, persisted.Id);
        Assert.Equal("DJ", persisted.DisplayName);
    }

    [Fact]
    public async Task JoiningTwiceWithSameNameReturnsSameId()
    {
        await using var firstContext = CreateContext();
        var first = await new ChatService(firstContext).JoinAsync("DJ");
        await using var secondContext = CreateContext();
        var second = await new ChatService(secondContext).JoinAsync("DJ");

        Assert.Equal(first.Id, second.Id);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" \t ")]
    [InlineData("abcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrstuvwxy")]
    public async Task JoinRejectsInvalidNames(string name)
    {
        await using var database = CreateContext();
        var service = new ChatService(database);

        await Assert.ThrowsAsync<ArgumentException>(() => service.JoinAsync(name));
        Assert.Empty(await database.Users.ToListAsync());
    }

    [Fact]
    public async Task BoundJoinAcceptsSameIdentityAndRejectsSwitchWithoutCreatingUser()
    {
        await using var database = CreateContext();
        var service = new ChatService(database);
        var first = await service.JoinAsync("DJ");
        var other = await service.JoinAsync("Other");

        Assert.Equal(first.Id, (await service.JoinAsync(" dj ", boundUserId: first.Id)).Id);
        await Assert.ThrowsAsync<ArgumentException>(() => service.JoinAsync("Other", boundUserId: first.Id));
        await Assert.ThrowsAsync<ArgumentException>(() => service.JoinAsync("New", boundUserId: first.Id));

        Assert.Equal(new[] { new KnownUser(first.Id, "DJ"), new KnownUser(other.Id, "Other") },
            await service.GetUsersAsync());
    }

    [Fact]
    public async Task OverlappingJoinsResolveToOnePersistedUser()
    {
        var pause = new PauseBeforeSave();
        await using var firstContext = CreateContext(pause);
        await using var secondContext = CreateContext();
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));

        var firstJoin = new ChatService(firstContext).JoinAsync("DJ", timeout.Token);
        User second;
        try
        {
            await pause.Reached.Task.WaitAsync(timeout.Token);
            second = await new ChatService(secondContext).JoinAsync("DJ", timeout.Token);
        }
        finally
        {
            // Both lookups have seen an empty table before the paused insert resumes.
            pause.Release.TrySetResult();
            await firstJoin;
        }

        var first = await firstJoin;
        await using var reader = CreateContext();
        var persisted = await reader.Users.SingleAsync();
        Assert.Equal(persisted.Id, first.Id);
        Assert.Equal(persisted.Id, second.Id);
        Assert.False(firstContext.ChangeTracker.HasChanges());
    }

    [Fact]
    public async Task SaveMessageRejectsWhitespaceAndOverLimitAndAcceptsLimit()
    {
        await using var database = CreateContext();
        var service = new ChatService(database);
        var author = await service.JoinAsync("DJ");

        await Assert.ThrowsAsync<ArgumentException>(() => service.SaveMessageAsync(author.Id, ""));
        await Assert.ThrowsAsync<ArgumentException>(() => service.SaveMessageAsync(author.Id, " \t "));
        await Assert.ThrowsAsync<ArgumentException>(() => service.SaveMessageAsync(author.Id, new string('x', 2_001)));
        var text = new string('x', 2_000);
        var saved = await service.SaveMessageAsync(author.Id, " " + text + " ");

        Assert.Equal(text, saved.Text);
        Assert.Equal(text, (await database.Messages.AsNoTracking().SingleAsync()).Text);
    }

    [Fact]
    public async Task SaveMessagePersistsIdAuthorAndServerTimestamp()
    {
        await using var database = CreateContext();
        var service = new ChatService(database);
        var author = await service.JoinAsync("DJ");
        var beforeSave = DateTimeOffset.UtcNow;

        var saved = await service.SaveMessageAsync(author.Id, " hello ");
        var afterSave = DateTimeOffset.UtcNow;

        await using var reader = CreateContext();
        var persisted = await reader.Messages.Include(message => message.User).SingleAsync();
        Assert.True(persisted.Id > 0);
        Assert.Equal(persisted.Id, saved.Id);
        Assert.Equal(author.Id, persisted.UserId);
        Assert.Equal("DJ", persisted.User.DisplayName);
        Assert.Equal(persisted.User.DisplayName, saved.Author);
        Assert.Equal("hello", persisted.Text);
        Assert.Equal(persisted.SentAtUtc, saved.SentAtUtc);
        Assert.Equal(TimeSpan.Zero, persisted.SentAtUtc.Offset);
        Assert.InRange(persisted.SentAtUtc, beforeSave, afterSave);
    }

    [Fact]
    public async Task SaveMessageRejectsUnknownUser()
    {
        await using var database = CreateContext();
        var service = new ChatService(database);

        var error = await Assert.ThrowsAsync<ArgumentException>(() => service.SaveMessageAsync(123, "hello"));

        Assert.Equal("Unknown user.", error.Message);
        Assert.Empty(await database.Messages.ToListAsync());
    }

    [Fact]
    public async Task HistoryReturnsNewestFiftyInAscendingOrder()
    {
        var ids = await SeedHistoryAsync();
        await using var database = CreateContext();

        var page = await new ChatService(database).GetHistoryAsync();

        Assert.Equal(ids.Skip(5), page.Messages.Select(message => message.Id));
        Assert.True(page.HasMore);
    }

    [Fact]
    public async Task HistoryBeforeCursorReturnsOnlyOlderMessagesInAscendingOrder()
    {
        var ids = await SeedHistoryAsync();
        await using var database = CreateContext();

        var page = await new ChatService(database).GetHistoryAsync(beforeId: ids[5]);

        Assert.Equal(ids.Take(5), page.Messages.Select(message => message.Id));
        Assert.False(page.HasMore);
    }

    [Fact]
    public async Task HistoryAfterCursorReturnsOnlyNewerMessagesInAscendingOrder()
    {
        var ids = await SeedHistoryAsync();
        await using var database = CreateContext();

        var page = await new ChatService(database).GetHistoryAsync(afterId: ids[49]);

        Assert.Equal(ids.Skip(50), page.Messages.Select(message => message.Id));
        Assert.False(page.HasMore);
    }

    [Fact]
    public async Task HistoryRejectsBothCursors()
    {
        await using var database = CreateContext();
        var service = new ChatService(database);

        await Assert.ThrowsAsync<ArgumentException>(() => service.GetHistoryAsync(beforeId: 10, afterId: 1));
    }

    private ChatDbContext CreateContext(params IInterceptor[] interceptors)
    {
        var options = new DbContextOptionsBuilder<ChatDbContext>()
            .UseSqlite(connection)
            .AddInterceptors(interceptors)
            .Options;
        return new ChatDbContext(options);
    }

    private async Task<int[]> SeedHistoryAsync()
    {
        await using var database = CreateContext();
        var author = await new ChatService(database).JoinAsync("DJ");
        var start = DateTimeOffset.UtcNow.AddMinutes(-1);
        var messages = Enumerable.Range(0, 55).Select(index => new Message
        {
            UserId = author.Id,
            Text = $"Message {index + 1}",
            SentAtUtc = start.AddSeconds(index)
        }).ToArray();
        database.Messages.AddRange(messages);
        await database.SaveChangesAsync();
        return messages.Select(message => message.Id).Order().ToArray();
    }

    private sealed class PauseBeforeSave : SaveChangesInterceptor
    {
        public TaskCompletionSource Reached { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource Release { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            Reached.TrySetResult();
            await Release.Task.WaitAsync(cancellationToken);
            return result;
        }
    }
}
