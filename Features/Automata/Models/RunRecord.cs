using ProfAlan.Domain.Automata;

namespace ProfAlan.Features.Automata.Models;

/// <summary>A finished run from the visualizer, handed up so it can be recorded in history.</summary>
public sealed record RunRecord(string Input, ComputationResult Result);
