namespace ProfAlan.Shared.Supabase;

/// <summary>
/// Project URL and anon key from wwwroot/appsettings.json. The anon key is public by design;
/// row level security in the database is what protects the data.
/// </summary>
public sealed record SupabaseOptions(string Url, string AnonKey)
{
    public bool IsConfigured => Url.StartsWith("https://", StringComparison.OrdinalIgnoreCase) && AnonKey.Length > 20;
}
