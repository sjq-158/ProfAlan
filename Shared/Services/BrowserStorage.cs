using System.Text.Json;
using Microsoft.JSInterop;

namespace ProfAlan.Shared.Services;

/// <summary>Thin wrapper over localStorage and sessionStorage.</summary>
public sealed class BrowserStorage(IJSRuntime js)
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public ValueTask<string?> GetAsync(string key, bool session = false) =>
        js.InvokeAsync<string?>("profAlan.storage.get", key, session);

    public ValueTask SetAsync(string key, string value, bool session = false) =>
        js.InvokeVoidAsync("profAlan.storage.set", key, value, session);

    public ValueTask RemoveAsync(string key, bool session = false) =>
        js.InvokeVoidAsync("profAlan.storage.remove", key, session);

    public async Task<T?> GetJsonAsync<T>(string key, bool session = false)
    {
        var raw = await GetAsync(key, session);
        if (string.IsNullOrEmpty(raw)) return default;
        try
        {
            return JsonSerializer.Deserialize<T>(raw, Json);
        }
        catch (JsonException)
        {
            return default;
        }
    }

    public ValueTask SetJsonAsync<T>(string key, T value, bool session = false) =>
        SetAsync(key, JsonSerializer.Serialize(value, Json), session);
}
