using ProfAlan.Domain.Automata;
using ProfAlan.Shared.Services;

namespace ProfAlan.Features.Automata.Services;

/// <summary>
/// Keeps one work-in-progress machine per kind, plus the grammar and regex text, in localStorage
/// so a refresh or a detour to another page never loses work.
/// </summary>
public sealed class SandboxDrafts(BrowserStorage storage)
{
    private const string Key = "profalan.sandbox";

    public sealed class Snapshot
    {
        public Dictionary<MachineKind, Automaton> Machines { get; set; } = [];
        public Dictionary<MachineKind, Guid> SavedIds { get; set; } = [];
        public string Grammar { get; set; } = "S → aSb | e";
        public string Regex { get; set; } = "a(a ∪ b)*bb";
    }

    private Snapshot? current;

    public async Task<Snapshot> LoadAsync()
    {
        if (current is not null) return current;
        current = await storage.GetJsonAsync<Snapshot>(Key) ?? new Snapshot();
        foreach (var kind in Enum.GetValues<MachineKind>())
        {
            if (!current.Machines.ContainsKey(kind))
            {
                current.Machines[kind] = SampleLibrary.For(kind).FirstOrDefault()?.Build() ?? Automaton.Blank(kind);
            }
        }
        return current;
    }

    public async Task SaveAsync()
    {
        if (current is not null) await storage.SetJsonAsync(Key, current);
    }

    public IReadOnlyList<Automaton> FiniteMachines =>
        current is null ? [] : current.Machines.Where(p => p.Key is MachineKind.Dfa or MachineKind.Nfa).Select(p => p.Value).ToList();
}
