using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Components;
using ProfAlan.Features.Auth.Models;
using ProfAlan.Shared.Supabase;

namespace ProfAlan.Features.Auth.Services;

/// <summary>Talks to Supabase Auth (GoTrue) over its REST endpoints.</summary>
public sealed class SupabaseAuthGateway(HttpClient http, SupabaseOptions options, NavigationManager navigation) : IAuthGateway
{
    public bool IsDemo => false;

    public Task<AuthOutcome> SignInAsync(string email, string password) =>
        PostAsync("token?grant_type=password", new { email, password });

    public async Task<AuthOutcome> SignUpAsync(string email, string password, string displayName, UserRole role)
    {
        // Confirmation links return to wherever this app is running, so no site URL is hardcoded.
        var redirect = Uri.EscapeDataString($"{navigation.BaseUri}login");
        var outcome = await PostAsync($"signup?redirect_to={redirect}", new
        {
            email,
            password,
            data = new { display_name = displayName, role = role.ToString().ToLowerInvariant() }
        });
        return outcome.Session is null && outcome.Error is null ? outcome with { NeedsConfirmation = true } : outcome;
    }

    public async Task<UserSession?> RefreshAsync(UserSession session)
    {
        var outcome = await PostAsync("token?grant_type=refresh_token", new { refresh_token = session.RefreshToken });
        return outcome.Session;
    }

    public async Task SignOutAsync(UserSession session)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, Endpoint("logout"));
        request.Headers.Add("apikey", options.AnonKey);
        request.Headers.Authorization = new("Bearer", session.AccessToken);
        try
        {
            using var _ = await http.SendAsync(request);
        }
        catch (HttpRequestException)
        {
            // Signing out locally still succeeds; the token simply expires on its own.
        }
    }

    public async Task<UserSession?> SessionFromRedirectAsync(string accessToken, string refreshToken, int expiresIn)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, Endpoint("user"));
        request.Headers.Add("apikey", options.AnonKey);
        request.Headers.Authorization = new("Bearer", accessToken);
        try
        {
            using var response = await http.SendAsync(request);
            if (!response.IsSuccessStatusCode) return null;
            using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            var json = $$"""{"access_token":{{JsonSerializer.Serialize(accessToken)}},"refresh_token":{{JsonSerializer.Serialize(refreshToken)}},"expires_in":{{expiresIn}},"user":{{doc.RootElement.GetRawText()}}}""";
            using var session = JsonDocument.Parse(json);
            return ReadSession(session.RootElement);
        }
        catch (HttpRequestException)
        {
            return null;
        }
    }

    private string Endpoint(string path) => $"{options.Url.TrimEnd('/')}/auth/v1/{path}";

    private async Task<AuthOutcome> PostAsync(string path, object body)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, Endpoint(path)) { Content = JsonContent.Create(body) };
        request.Headers.Add("apikey", options.AnonKey);

        HttpResponseMessage response;
        try
        {
            response = await http.SendAsync(request);
        }
        catch (HttpRequestException)
        {
            return AuthOutcome.Fail("Could not reach the authentication server.");
        }

        using (response)
        {
            var json = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode)
            {
                return AuthOutcome.Fail(SupabaseRest.ExtractMessage(json) ?? "Authentication failed.");
            }

            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            return root.TryGetProperty("access_token", out _)
                ? new AuthOutcome(ReadSession(root))
                : new AuthOutcome(null);
        }
    }

    private static UserSession ReadSession(JsonElement root)
    {
        var user = root.GetProperty("user");
        var email = user.GetProperty("email").GetString() ?? "";
        var meta = user.TryGetProperty("user_metadata", out var m) ? m : default;

        string? Meta(string key) =>
            meta.ValueKind == JsonValueKind.Object && meta.TryGetProperty(key, out var v) ? v.GetString() : null;

        return new UserSession(
            user.GetProperty("id").GetString()!,
            email,
            Meta("display_name") ?? email.Split('@')[0],
            Meta("role") == "instructor" ? UserRole.Instructor : UserRole.Student,
            root.GetProperty("access_token").GetString()!,
            root.GetProperty("refresh_token").GetString()!,
            DateTimeOffset.UtcNow.AddSeconds(root.GetProperty("expires_in").GetInt32()));
    }
}
