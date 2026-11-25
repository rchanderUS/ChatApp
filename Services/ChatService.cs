using ChatApp.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace ChatApp.Services;

public sealed class ChatService(ChatDbContext database)
{
    public const int MaxDisplayNameLength = 50;
    public const int MaxMessageLength = 2_000;

    private const int PageSize = 50;
    private const int SqliteUniqueConstraint = 2067;

    public async Task<User> JoinAsync(string? name, CancellationToken cancellationToken = default)
    {
        var displayName = name?.Trim();
        if (string.IsNullOrEmpty(displayName) || displayName.Length > MaxDisplayNameLength)
        {
            throw new ArgumentException($"Enter a name between 1 and {MaxDisplayNameLength} characters.", nameof(name));
        }

        var normalizedName = displayName.ToLowerInvariant();
        var existingUser = await database.Users.SingleOrDefaultAsync(
            user => user.NormalizedName == normalizedName, cancellationToken);
        if (existingUser is not null)
        {
            return existingUser;
        }

        var newUser = new User
        {
            DisplayName = displayName,
            NormalizedName = normalizedName,
            CreatedAtUtc = DateTimeOffset.UtcNow
        };
        database.Users.Add(newUser);

        try
        {
            await database.SaveChangesAsync(cancellationToken);
            return newUser;
        }
        catch (DbUpdateException exception) when (
            exception.InnerException is SqliteException { SqliteExtendedErrorCode: SqliteUniqueConstraint })
        {
            // Another join saved this name after our lookup.
            database.Entry(newUser).State = EntityState.Detached;
            return await database.Users.SingleAsync(
                user => user.NormalizedName == normalizedName, cancellationToken);
        }
    }

    public async Task<ChatMessage> SaveMessageAsync(
        int userId, string? text, CancellationToken cancellationToken = default)
    {
        var trimmedText = text?.Trim();
        if (string.IsNullOrEmpty(trimmedText) || trimmedText.Length > MaxMessageLength)
        {
            throw new ArgumentException($"Enter a message between 1 and {MaxMessageLength:N0} characters.", nameof(text));
        }

        var author = await database.Users.SingleOrDefaultAsync(user => user.Id == userId, cancellationToken)
            ?? throw new ArgumentException("Unknown user.");
        var message = new Message { User = author, Text = trimmedText, SentAtUtc = DateTimeOffset.UtcNow };
        database.Messages.Add(message);
        await database.SaveChangesAsync(cancellationToken);

        return new ChatMessage(message.Id, author.Id, author.DisplayName, message.Text, message.SentAtUtc);
    }

    public async Task<HistoryPage> GetHistoryAsync(
        int? beforeId = null, int? afterId = null, CancellationToken cancellationToken = default)
    {
        if (beforeId.HasValue && afterId.HasValue)
        {
            throw new ArgumentException("Use one history cursor at a time.");
        }

        var messages = database.Messages.AsNoTracking();
        if (beforeId is int before)
        {
            messages = messages.Where(message => message.Id < before);
        }

        if (afterId is int after)
        {
            messages = messages.Where(message => message.Id > after);
        }

        var ordered = afterId.HasValue
            ? messages.OrderBy(message => message.Id)
            : messages.OrderByDescending(message => message.Id);
        var page = await ordered.Take(PageSize + 1)
            .Select(message => new ChatMessage(
                message.Id, message.UserId, message.User.DisplayName, message.Text, message.SentAtUtc))
            .ToListAsync(cancellationToken);

        var hasMore = page.Count > PageSize;
        if (hasMore)
        {
            page.RemoveAt(PageSize);
        }

        if (!afterId.HasValue)
        {
            page.Reverse();
        }

        return new HistoryPage(page, hasMore);
    }
}
