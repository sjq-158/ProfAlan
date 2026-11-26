namespace ProfAlan.Models.Machines;

public interface IMachine
{
    MachineKind Kind { get; }
    IReadOnlyList<State> States { get; }
    IReadOnlyList<Transition> Transitions { get; }
    SimulationResult Run(string input);
}