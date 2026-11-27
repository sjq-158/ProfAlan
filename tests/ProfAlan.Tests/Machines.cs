namespace ProfAlan.Tests;

/// <summary>Small builder so test machines read like their formal definitions.</summary>
internal static class Machines
{
    public static Automaton Build(MachineKind kind, string sigma, string initial, string[] final, params (string From, string Read, string To)[] rules)
    {
        var m = new Automaton { Kind = kind, Alphabet = sigma.ToList() };
        foreach (var name in rules.SelectMany(r => new[] { r.From, r.To }).Prepend(initial).Concat(final).Distinct())
        {
            m.States.Add(new StateNode { Name = name, IsInitial = name == initial, IsFinal = final.Contains(name) });
        }
        foreach (var (from, read, to) in rules)
        {
            m.Transitions.Add(new TransitionRule { From = from, Read = read, To = to });
        }
        return m;
    }

    /// <summary>Strings over {a,b} that start with a and end with bb: a(a ∪ b)*bb.</summary>
    public static Automaton StartsWithAEndsWithBb() => Build(MachineKind.Dfa, "ab", "q0", ["q3"],
        ("q0", "a", "q1"), ("q0", "b", "dead"),
        ("q1", "a", "q1"), ("q1", "b", "q2"),
        ("q2", "a", "q1"), ("q2", "b", "q3"),
        ("q3", "a", "q1"), ("q3", "b", "q3"),
        ("dead", "a", "dead"), ("dead", "b", "dead"));

    /// <summary>{ aⁿbⁿ : n ≥ 0 }, pushing an a for every a and popping one for every b.</summary>
    public static Automaton AnBn()
    {
        var m = new Automaton
        {
            Kind = MachineKind.Pda,
            Alphabet = ['a', 'b'],
            StackAlphabet = ['a'],
            States =
            [
                new StateNode { Name = "s", IsInitial = true, IsFinal = true },
                new StateNode { Name = "f", IsFinal = true }
            ],
            Transitions =
            [
                new TransitionRule { From = "s", Read = "a", Pop = "", To = "s", Push = "a" },
                new TransitionRule { From = "s", Read = "b", Pop = "a", To = "f", Push = "" },
                new TransitionRule { From = "f", Read = "b", Pop = "a", To = "f", Push = "" }
            ]
        };
        return m;
    }

    /// <summary>Decides strings over {a} of even length using halting states y and n.</summary>
    public static Automaton EvenLengthDecider()
    {
        var m = new Automaton
        {
            Kind = MachineKind.Turing,
            Alphabet = ['a'],
            States =
            [
                new StateNode { Name = "s", IsInitial = true },
                new StateNode { Name = "even" },
                new StateNode { Name = "odd" },
                new StateNode { Name = "y", IsHalting = true, IsFinal = true },
                new StateNode { Name = "n", IsHalting = true }
            ]
        };
        void Rule(string from, char read, string to, string action) =>
            m.Transitions.Add(new TransitionRule { From = from, Read = read.ToString(), To = to, Action = action });

        Rule("s", Symbols.Blank, "even", Symbols.MoveRight);
        Rule("s", Symbols.LeftEnd, "s", Symbols.MoveRight);
        Rule("even", 'a', "odd", Symbols.MoveRight);
        Rule("odd", 'a', "even", Symbols.MoveRight);
        Rule("even", Symbols.Blank, "y", Symbols.Blank.ToString());
        Rule("odd", Symbols.Blank, "n", Symbols.Blank.ToString());
        return m;
    }
}
