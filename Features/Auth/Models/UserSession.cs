namespace ProfAlan.Features.Auth.Models;

public enum UserRole
{
    Student,
    Instructor
}

public sealed record UserSession(
    string UserId,
    string Email,
    string DisplayName,
    UserRole Role,
    string AccessToken,
    string RefreshToken,
    DateTimeOffset ExpiresAt)
{
    public bool IsExpiring => DateTimeOffset.UtcNow > ExpiresAt.AddSeconds(-60);

    public string Initials => string.Concat(DisplayName.Split(' ', StringSplitOptions.RemoveEmptyEntries).Take(2).Select(w => char.ToUpperInvariant(w[0])));
}

public sealed record AuthOutcome(UserSession? Session, string? Error = null, bool NeedsConfirmation = false)
{
    public static AuthOutcome Fail(string error) => new(null, error);
}
