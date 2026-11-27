using System.Text.Json.Serialization;
using ProfAlan.Domain.Automata;
using ProfAlan.Features.Automata.Models;
using ProfAlan.Shared.Supabase;

namespace ProfAlan.Features.Automata.Services;

public sealed class SupabaseAutomatonLibrary(SupabaseRest db) : IAutomatonLibrary
{
    private sealed record AutomatonRow(
        [property: JsonPropertyName("id")] Guid Id,
        [property: JsonPropertyName("name")] string Name,
        [property: JsonPropertyName("kind")] string Kind,
        [property: JsonPropertyName("definition")] Automaton Definition,
        [property: JsonPropertyName("updated_at")] DateTimeOffset UpdatedAt);

    private sealed record RunRow(
        [property: JsonPropertyName("id")] Guid Id,
        [property: JsonPropertyName("machine_name")] string MachineName,
        [property: JsonPropertyName("kind")] string Kind,
        [property: JsonPropertyName("input")] string Input,
        [property: JsonPropertyName("verdict")] string Verdict,
        [property: JsonPropertyName("steps")] int Steps,
        [property: JsonPropertyName("created_at")] DateTimeOffset CreatedAt);

    public async Task<IReadOnlyList<SavedAutomaton>> ListAsync()
    {
        var rows = await db.SelectAsync<AutomatonRow>("automata?select=id,name,kind,definition,updated_at&order=updated_at.desc");
        return rows.Select(ToModel).ToList();
    }

    public async Task<SavedAutomaton?> GetAsync(Guid id)
    {
        var rows = await db.SelectAsync<AutomatonRow>($"automata?select=id,name,kind,definition,updated_at&id=eq.{id}");
        return rows.Count == 0 ? null : ToModel(rows[0]);
    }

    public async Task<SavedAutomaton> SaveAsync(SavedAutomaton machine)
    {
        var row = await db.UpsertAsync<AutomatonRow>("automata", new AutomatonRow(
            machine.Id, machine.Name, machine.Kind.Slug(), machine.Definition, DateTimeOffset.UtcNow));
        return ToModel(row);
    }

    public Task DeleteAsync(Guid id) => db.DeleteAsync($"automata?id=eq.{id}");

    public Task RecordRunAsync(SimulationRun run) =>
        db.InsertManyAsync("simulation_runs", new[]
        {
            new RunRow(run.Id, run.MachineName, run.Kind.Slug(), run.Input, run.Verdict.ToString(), run.Steps, run.CreatedAt)
        });

    public async Task<IReadOnlyList<SimulationRun>> RecentRunsAsync(int count = 8)
    {
        var rows = await db.SelectAsync<RunRow>($"simulation_runs?select=*&order=created_at.desc&limit={count}");
        return rows.Select(r => new SimulationRun(
            r.Id, r.MachineName, MachineKindNames.FromSlug(r.Kind) ?? MachineKind.Dfa, r.Input,
            Enum.TryParse<Verdict>(r.Verdict, out var v) ? v : Verdict.Rejected, r.Steps, r.CreatedAt)).ToList();
    }

    private static SavedAutomaton ToModel(AutomatonRow r) =>
        new(r.Id, r.Name, MachineKindNames.FromSlug(r.Kind) ?? r.Definition.Kind, r.Definition, r.UpdatedAt);
}
