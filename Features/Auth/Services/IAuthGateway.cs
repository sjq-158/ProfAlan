using ProfAlan.Features.Auth.Models;

namespace ProfAlan.Features.Auth.Services;

public interface IAuthGateway
{
    bool IsDemo { get; }
    Task<AuthOutcome> SignInAsync(string email, string password);
    Task<AuthOutcome> SignUpAsync(string email, string password, string displayName, UserRole role);
    Task<UserSession?> RefreshAsync(UserSession session);
    Task SignOutAsync(UserSession session);

    /// <summary>Builds a session from the tokens Supabase appends to an email confirmation redirect.</summary>
    Task<UserSession?> SessionFromRedirectAsync(string accessToken, string refreshToken, int expiresIn);
}
