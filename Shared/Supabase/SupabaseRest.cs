using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace ProfAlan.Shared.Supabase;

public interface IAccessTokenSource
{
    Task<string?> GetAccessTokenAsync();
}

public sealed class SupabaseException(string message) : Exception(message);

/// <summary>Minimal PostgREST client: every call carries the anon key plus the signed-in user's JWT.</summary>
public sealed class SupabaseRest(HttpClient http, SupabaseOptions options, IAccessTokenSource tokens)
{
    public static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public async Task<List<T>> SelectAsync<T>(string pathAndQuery)
    {
        using var response = await SendAsync(HttpMethod.Get, pathAndQuery);
        return await response.Content.ReadFromJsonAsync<List<T>>(Json) ?? [];
    }

    public async Task<T> UpsertAsync<T>(string table, object row)
    {
        using var response = await SendAsync(HttpMethod.Post, table, row, "resolution=merge-duplicates,return=representation");
        var rows = await response.Content.ReadFromJsonAsync<List<T>>(Json);
        return rows is { Count: > 0 } ? rows[0] : throw new SupabaseException("The server did not return the saved row.");
    }

    public async Task InsertManyAsync(string table, object rows)
    {
        using var _ = await SendAsync(HttpMethod.Post, table, rows, "return=minimal");
    }

    public async Task DeleteAsync(string tableAndFilter)
    {
        using var _ = await SendAsync(HttpMethod.Delete, tableAndFilter);
    }

    public async Task<List<T>> RpcAsync<T>(string function, object args)
    {
        using var response = await SendAsync(HttpMethod.Post, $"rpc/{function}", args);
        return await response.Content.ReadFromJsonAsync<List<T>>(Json) ?? [];
    }

    private async Task<HttpResponseMessage> SendAsync(HttpMethod method, string path, object? body = null, string? prefer = null)
    {
        using var request = new HttpRequestMessage(method, $"{options.Url.TrimEnd('/')}/rest/v1/{path}");
        request.Headers.Add("apikey", options.AnonKey);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", await tokens.GetAccessTokenAsync() ?? options.AnonKey);
        if (prefer is not null) request.Headers.Add("Prefer", prefer);
        if (body is not null) request.Content = JsonContent.Create(body, options: Json);

        HttpResponseMessage response;
        try
        {
            response = await http.SendAsync(request);
        }
        catch (HttpRequestException)
        {
            throw new SupabaseException("Could not reach the database. Check your connection and try again.");
        }

        if (response.IsSuccessStatusCode) return response;

        var detail = await response.Content.ReadAsStringAsync();
        response.Dispose();
        throw new SupabaseException(ExtractMessage(detail) ?? $"Request failed ({(int)response.StatusCode}).");
    }

    internal static string? ExtractMessage(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            foreach (var key in new[] { "message", "msg", "error_description", "error" })
            {
                if (doc.RootElement.TryGetProperty(key, out var value) && value.ValueKind == JsonValueKind.String)
                {
                    return value.GetString();
                }
            }
        }
        catch (JsonException)
        {
        }
        return null;
    }
}
