using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using ProfAlan.Features.Auth.Models;
using ProfAlan.Shared.Services;
using ProfAlan.Shared.Supabase;

namespace ProfAlan.Features.Auth.Services;

/// <summary>Owns the signed-in user, persists it across reloads and refreshes the access token.</summary>
public sealed class AuthSession(IAuthGateway gateway, BrowserStorage storage)
    : AuthenticationStateProvider, IAccessTokenSource
{
    private const string SessionKey = "profalan.session";

    private UserSession? current;
    private bool restored;

    public UserSession? Current => current;
    public bool IsDemo => gateway.IsDemo;

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        await RestoreAsync();
        return BuildState(current);
    }

    public async Task<AuthOutcome> SignInAsync(string email, string password)
    {
        var outcome = await gateway.SignInAsync(email, password);
        if (outcome.Session is not null) await SetAsync(outcome.Session);
        return outcome;
    }

    public async Task<AuthOutcome> SignUpAsync(string email, string password, string displayName, UserRole role)
    {
        var outcome = await gateway.SignUpAsync(email, password, displayName, role);
        if (outcome.Session is not null) await SetAsync(outcome.Session);
        return outcome;
    }

    public async Task<bool> CompleteRedirectAsync(string accessToken, string refreshToken, int expiresIn)
    {
        var session = await gateway.SessionFromRedirectAsync(accessToken, refreshToken, expiresIn);
        if (session is null) return false;
        await SetAsync(session);
        return true;
    }

    public async Task SignOutAsync()
    {
        if (current is not null) await gateway.SignOutAsync(current);
        current = null;
        await storage.RemoveAsync(SessionKey);
        NotifyAuthenticationStateChanged(Task.FromResult(BuildState(null)));
    }

    public async Task<string?> GetAccessTokenAsync()
    {
        await RestoreAsync();
        if (current is null || gateway.IsDemo) return null;
        if (current.IsExpiring)
        {
            var refreshed = await gateway.RefreshAsync(current);
            if (refreshed is null)
            {
                await SignOutAsync();
                return null;
            }
            await SetAsync(refreshed);
        }
        return current.AccessToken;
    }

    private async Task RestoreAsync()
    {
        if (restored) return;
        restored = true;
        current = await storage.GetJsonAsync<UserSession>(SessionKey);
    }

    private async Task SetAsync(UserSession session)
    {
        current = session;
        restored = true;
        await storage.SetJsonAsync(SessionKey, session);
        NotifyAuthenticationStateChanged(Task.FromResult(BuildState(session)));
    }

    private static AuthenticationState BuildState(UserSession? session)
    {
        if (session is null) return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));

        var identity = new ClaimsIdentity(
        [
            new Claim(ClaimTypes.NameIdentifier, session.UserId),
            new Claim(ClaimTypes.Name, session.DisplayName),
            new Claim(ClaimTypes.Email, session.Email),
            new Claim(ClaimTypes.Role, session.Role.ToString())
        ], "profalan");
        return new AuthenticationState(new ClaimsPrincipal(identity));
    }
}
