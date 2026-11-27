using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using ProfAlan;
using ProfAlan.Features.Auth.Services;
using ProfAlan.Features.Automata.Services;
using ProfAlan.Features.Quizzes.Services;
using ProfAlan.Features.StudyArea.Services;
using ProfAlan.Shared.Services;
using ProfAlan.Shared.Supabase;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

var supabase = new SupabaseOptions(
    builder.Configuration["Supabase:Url"] ?? "",
    builder.Configuration["Supabase:AnonKey"] ?? "");

builder.Services.AddSingleton(supabase);
builder.Services.AddScoped(_ => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
builder.Services.AddScoped<BrowserStorage>();
builder.Services.AddScoped<PaletteService>();
builder.Services.AddScoped<Notifier>();

builder.Services.AddAuthorizationCore();
builder.Services.AddScoped<AuthSession>();
builder.Services.AddScoped<AuthenticationStateProvider>(sp => sp.GetRequiredService<AuthSession>());
builder.Services.AddScoped<IAccessTokenSource>(sp => sp.GetRequiredService<AuthSession>());
builder.Services.AddScoped<SandboxDrafts>();
builder.Services.AddScoped<PendingQuizDraft>();
builder.Services.AddScoped<ApiKeyVault>();
builder.Services.AddScoped<GeminiClient>();

if (supabase.IsConfigured)
{
    builder.Services.AddScoped<SupabaseRest>();
    builder.Services.AddScoped<IAuthGateway, SupabaseAuthGateway>();
    builder.Services.AddScoped<IAutomatonLibrary, SupabaseAutomatonLibrary>();
    builder.Services.AddScoped<IQuizRepository, SupabaseQuizRepository>();
}
else
{
    builder.Services.AddScoped<IAuthGateway, DemoAuthGateway>();
    builder.Services.AddScoped<IAutomatonLibrary, DemoAutomatonLibrary>();
    builder.Services.AddScoped<IQuizRepository, DemoQuizRepository>();
}

await builder.Build().RunAsync();
