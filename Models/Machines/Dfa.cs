namespace ProfAlan.Models.Machines;

public sealed class Dfa : IMachine
{
    public MachineKind Kind => MachineKind.Dfa;
    public IReadOnlyList<State> States { get; }
    public IReadOnlyList<Transition> Transitions { get; }

    public Dfa(IEnumerable<State> states, IEnumerable<Transition> transitions)
    {
        States = states.ToList();
        Transitions = transitions.ToList();
    }

    public SimulationResult Run(string input)
    {
        var current = States.First(s => s.IsStart).Id;
        var steps = new List<SimulationStep> { new([current], 0, "start") };

        for (var i = 0; i < input.Length; i++)
        {
            var edge = Transitions.FirstOrDefault(t => t.From == current && t.Read == input[i]);
            if (edge is null)
            {
                steps.Add(new SimulationStep([], i + 1, $"no transition on '{input[i]}', halt"));
                return new SimulationResult(false, steps);
            }

            current = edge.To;
            steps.Add(new SimulationStep([current], i + 1, $"read '{input[i]}'"));
        }

        return new SimulationResult(States.First(s => s.Id == current).IsAccepting, steps);
    }
}