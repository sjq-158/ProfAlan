using ProfAlan.Domain.Automata;

namespace ProfAlan.Features.Automata.Models;

public sealed record SavedAutomaton(Guid Id, string Name, MachineKind Kind, Automaton Definition, DateTimeOffset UpdatedAt);

public sealed record SimulationRun(Guid Id, string MachineName, MachineKind Kind, string Input, Verdict Verdict, int Steps, DateTimeOffset CreatedAt);
