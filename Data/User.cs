namespace ChatApp.Data;

public sealed class User
{
    public int Id { get; set; }
    public required string DisplayName { get; set; }

    // Use a trimmed, lowercase lookup key so equivalent names share one user.
    public required string NormalizedName { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
}
