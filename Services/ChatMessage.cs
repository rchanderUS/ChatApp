namespace ChatApp.Services;

public sealed record ChatMessage(int Id, int UserId, string Author, string Text, DateTimeOffset SentAtUtc);
