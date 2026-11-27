using System.Security.Cryptography;
using System.Text;
using ProfAlan.Features.Auth.Models;
using ProfAlan.Shared.Services;

namespace ProfAlan.Features.Auth.Services;

/// <summary>
/// Used when no Supabase project is configured, so the app stays fully usable offline.
/// Accounts live in this browser only.
/// </summary>
public sealed class DemoAuthGateway(BrowserStorage storage) : IAuthGateway
{
    private const string AccountsKey = "profalan.demo.accounts";

    private sealed record DemoAccount(string Id, string Email, string DisplayName, UserRole Role, string Salt, string PasswordHash);

    public bool IsDemo => true;

    public async Task<AuthOutcome> SignInAsync(string email, string password)
    {
        var accounts = await LoadAsync();
        var account = accounts.FirstOrDefault(a => a.Email.Equals(email.Trim(), StringComparison.OrdinalIgnoreCase));
        if (account is null || Hash(password, account.Salt) != account.PasswordHash)
        {
            return AuthOutcome.Fail("Invalid login credentials.");
        }
        return new AuthOutcome(ToSession(account));
    }

    public async Task<AuthOutcome> SignUpAsync(string email, string password, string displayName, UserRole role)
    {
        var accounts = await LoadAsync();
        if (accounts.Any(a => a.Email.Equals(email.Trim(), StringComparison.OrdinalIgnoreCase)))
        {
            return AuthOutcome.Fail("An account with this email already exists.");
        }

        var salt = Convert.ToHexString(RandomNumberGenerator.GetBytes(8));
        var account = new DemoAccount(Guid.NewGuid().ToString(), email.Trim(), displayName.Trim(), role, salt, Hash(password, salt));
        accounts.Add(account);
        await storage.SetJsonAsync(AccountsKey, accounts);
        return new AuthOutcome(ToSession(account));
    }

    public Task<UserSession?> RefreshAsync(UserSession session) =>
        Task.FromResult<UserSession?>(session with { ExpiresAt = DateTimeOffset.UtcNow.AddDays(30) });

    public Task SignOutAsync(UserSession session) => Task.CompletedTask;

    private async Task<List<DemoAccount>> LoadAsync() =>
        await storage.GetJsonAsync<List<DemoAccount>>(AccountsKey) ?? [];

    private static UserSession ToSession(DemoAccount a) =>
        new(a.Id, a.Email, a.DisplayName, a.Role, "demo", "demo", DateTimeOffset.UtcNow.AddDays(30));

    private static string Hash(string password, string salt) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(salt + password)));
}
