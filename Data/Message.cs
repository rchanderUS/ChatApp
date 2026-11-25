namespace ChatApp.Data;

public sealed class Message
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    public required string Text { get; set; }
    public DateTimeOffset SentAtUtc { get; set; }
}
