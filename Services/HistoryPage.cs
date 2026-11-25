namespace ChatApp.Services;

public sealed record HistoryPage(IReadOnlyList<ChatMessage> Messages, bool HasMore);
