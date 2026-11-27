using ProfAlan.Features.Automata.Models;

namespace ProfAlan.Features.Automata.Services;

/// <summary>The signed-in user's saved machines and their recent simulation runs.</summary>
public interface IAutomatonLibrary
{
    Task<IReadOnlyList<SavedAutomaton>> ListAsync();
    Task<SavedAutomaton?> GetAsync(Guid id);
    Task<SavedAutomaton> SaveAsync(SavedAutomaton machine);
    Task DeleteAsync(Guid id);
    Task RecordRunAsync(SimulationRun run);
    Task<IReadOnlyList<SimulationRun>> RecentRunsAsync(int count = 8);
}
