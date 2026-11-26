using Microsoft.JSInterop;

namespace ProfAlan.Services;

public sealed class ThemeService(IJSRuntime js)
{
    public bool IsDark { get; private set; }
    public event Action? Changed;

    public async Task InitAsync()
    {
        IsDark = await js.InvokeAsync<bool>("profAlanTheme.isDark");
        Changed?.Invoke();
    }

    public async Task ToggleAsync()
    {
        IsDark = await js.InvokeAsync<bool>("profAlanTheme.toggle");
        Changed?.Invoke();
    }
}