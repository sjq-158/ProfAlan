using ProfAlan.Shared.Services;

namespace ProfAlan.Features.StudyArea.Services;

/// <summary>
/// Holds the user's own Gemini API key in the browser only. By default it lives in sessionStorage
/// and disappears with the tab; "remember" moves it to localStorage. It is never sent to Supabase.
/// </summary>
public sealed class ApiKeyVault(BrowserStorage storage)
{
    private const string Key = "profalan.gemini.key";

    public async Task<(string? Key, bool Remembered)> LoadAsync()
    {
        var session = await storage.GetAsync(Key, session: true);
        if (!string.IsNullOrEmpty(session)) return (session, false);
        var local = await storage.GetAsync(Key);
        return (string.IsNullOrEmpty(local) ? null : local, !string.IsNullOrEmpty(local));
    }

    public async Task SaveAsync(string key, bool remember)
    {
        await ClearAsync();
        await storage.SetAsync(Key, key.Trim(), session: !remember);
    }

    public async Task ClearAsync()
    {
        await storage.RemoveAsync(Key, session: true);
        await storage.RemoveAsync(Key);
    }
}
