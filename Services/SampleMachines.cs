using ProfAlan.Models.Machines;

namespace ProfAlan.Services;

public static class SampleMachines
{
    /// <summary>Accepts strings over {a,b} with an even number of a's.</summary>
    public static Dfa EvenAs() => new(
        [
            new State("q0", IsStart: true, IsAccepting: true, X: 180, Y: 200),
            new State("q1", X: 420, Y: 200)
        ],
        [
            new Transition("q0", "q1", 'a'),
            new Transition("q1", "q0", 'a'),
            new Transition("q0", "q0", 'b'),
            new Transition("q1", "q1", 'b')
        ]);
}