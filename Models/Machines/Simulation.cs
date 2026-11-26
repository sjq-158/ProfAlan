namespace ProfAlan.Models.Machines;

public sealed record SimulationStep(
    IReadOnlyList<string> ActiveStates,
    int Position,
    string Note);

public sealed record SimulationResult(
    bool Accepted,
    IReadOnlyList<SimulationStep> Steps);