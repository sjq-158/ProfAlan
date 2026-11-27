using ProfAlan.Features.Auth.Services;
using ProfAlan.Features.Automata.Models;
using ProfAlan.Shared.Services;

namespace ProfAlan.Features.Automata.Services;

/// <summary>Browser-only storage used in demo mode, partitioned per demo account.</summary>
public sealed class DemoAutomatonLibrary(BrowserStorage storage, AuthSession auth) : IAutomatonLibrary
{
    private string Key(string table) => $"profalan.demo.{auth.Current?.UserId ?? "guest"}.{table}";

    public async Task<IReadOnlyList<SavedAutomaton>> ListAsync() =>
        (await LoadMachinesAsync()).OrderByDescending(m => m.UpdatedAt).ToList();

    public async Task<SavedAutomaton?> GetAsync(Guid id) =>
        (await LoadMachinesAsync()).FirstOrDefault(m => m.Id == id);

    public async Task<SavedAutomaton> SaveAsync(SavedAutomaton machine)
    {
        var all = await LoadMachinesAsync();
        var saved = machine with { UpdatedAt = DateTimeOffset.UtcNow };
        all.RemoveAll(m => m.Id == machine.Id);
        all.Add(saved);
        await storage.SetJsonAsync(Key("automata"), all);
        return saved;
    }

    public async Task DeleteAsync(Guid id)
    {
        var all = await LoadMachinesAsync();
        all.RemoveAll(m => m.Id == id);
        await storage.SetJsonAsync(Key("automata"), all);
    }

    public async Task RecordRunAsync(SimulationRun run)
    {
        var runs = await storage.GetJsonAsync<List<SimulationRun>>(Key("runs")) ?? [];
        runs.Insert(0, run);
        await storage.SetJsonAsync(Key("runs"), runs.Take(50).ToList());
    }

    public async Task<IReadOnlyList<SimulationRun>> RecentRunsAsync(int count = 8) =>
        (await storage.GetJsonAsync<List<SimulationRun>>(Key("runs")) ?? []).Take(count).ToList();

    private async Task<List<SavedAutomaton>> LoadMachinesAsync() =>
        await storage.GetJsonAsync<List<SavedAutomaton>>(Key("automata")) ?? [];
}
