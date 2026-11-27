using Microsoft.JSInterop;

namespace ProfAlan.Shared.Services;

public sealed record Palette(string Id, string Name, string Surface, string Panel, string Ink);

public sealed class PaletteService(IJSRuntime js)
{
    public static readonly IReadOnlyList<Palette> All =
    [
        new("platinum", "Platinum / French Gray / Gunmetal", "#D8D5DB", "#ADACB5", "#2D3142"),
        new("butter", "Blue / Butter / Chocopie", "#C4DAE8", "#FEEFB8", "#432F2E"),
        new("milk", "Wine / Milk / Heavenly", "#FFF7E6", "#95BAEB", "#930400"),
        new("linkwater", "Link Water / Gull Grey / Blue Jay", "#DCE4EF", "#A0ADC2", "#394E6E")
    ];

    public string Current { get; private set; } = "platinum";

    public event Action? Changed;

    public async Task InitializeAsync()
    {
        Current = await js.InvokeAsync<string>("profAlan.palette.get");
        Changed?.Invoke();
    }

    public async Task ApplyAsync(string id)
    {
        await js.InvokeVoidAsync("profAlan.palette.set", id);
        Current = id;
        Changed?.Invoke();
    }
}
