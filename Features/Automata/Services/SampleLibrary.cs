using ProfAlan.Domain.Automata;

namespace ProfAlan.Features.Automata.Services;

public sealed record SampleMachine(string Id, string Title, string Language, Func<Automaton> Build)
{
    public MachineKind Kind => Build().Kind;
}

/// <summary>Worked examples, several taken from Lewis &amp; Papadimitriou.</summary>
public static class SampleLibrary
{
    public static readonly IReadOnlyList<SampleMachine> All =
    [
        new("dfa-a-bb", "Starts with a, ends with bb", "L = { w ∈ {a,b}* | w starts with a and ends with bb }", StartsWithAEndsWithBb),
        new("dfa-b-mod-3", "Number of b's divisible by 3", "L = { w ∈ {a,b}* | the number of b's in w is divisible by 3 }", BsDivisibleByThree),
        new("dfa-no-aba", "Does not contain aba", "L = { w ∈ {a,b}* | w does not contain aba as a substring }", NoAba),
        new("nfa-bba", "Contains bba", "L = { w ∈ {a,b}* | w contains bba as a substring }", ContainsBba),
        new("nfa-bab-or-aba", "Starts with bab or ends with aba", "L = { w ∈ {a,b}* | w starts with bab or ends with aba }", StartsBabOrEndsAba),
        new("pda-anbn", "aⁿbⁿ", "L = { aⁿbⁿ : n ≥ 0 }", AnBn),
        new("pda-wcwr", "wcwᴿ", "L = { wcwᴿ : w ∈ {a,b}* } (Example 3.3.1)", WcwReverse),
        new("tm-anbn", "Decide aⁿbⁿ", "L = { aⁿbⁿ : n ≥ 0 }, decided with halting states y and n", TuringAnBn),
        new("tm-a-to-b", "Rewrite every a as b", "Computes f(w) = w with each a replaced by b", RewriteAsToBs)
    ];

    public static SampleMachine? Find(string id) => All.FirstOrDefault(s => s.Id == id);

    public static IEnumerable<SampleMachine> For(MachineKind kind) => All.Where(s => s.Kind == kind);

    private static Automaton Make(MachineKind kind, string name, string description, string sigma,
        (string Name, double X, double Y)[] states, string initial, string[] final)
    {
        var m = new Automaton { Kind = kind, Name = name, Description = description, Alphabet = sigma.ToList() };
        foreach (var (stateName, x, y) in states)
        {
            m.States.Add(new StateNode { Name = stateName, X = x, Y = y, IsInitial = stateName == initial, IsFinal = final.Contains(stateName) });
        }
        return m;
    }

    private static void Rule(Automaton m, string from, string read, string to) =>
        m.Transitions.Add(new TransitionRule { From = from, Read = read, To = to });

    private static void Pda(Automaton m, string from, string read, string pop, string to, string push) =>
        m.Transitions.Add(new TransitionRule { From = from, Read = read, Pop = pop, To = to, Push = push });

    private static void Tm(Automaton m, string from, char read, string to, string action) =>
        m.Transitions.Add(new TransitionRule { From = from, Read = read.ToString(), To = to, Action = action });

    private static Automaton StartsWithAEndsWithBb()
    {
        var m = Make(MachineKind.Dfa, "Starts with a, ends with bb", "Regular expression a(a ∪ b)*bb.", "ab",
            [("q0", 120, 250), ("q1", 300, 250), ("q2", 480, 250), ("q3", 660, 250), ("q4", 120, 430)], "q0", ["q3"]);
        Rule(m, "q0", "a", "q1"); Rule(m, "q0", "b", "q4");
        Rule(m, "q1", "a", "q1"); Rule(m, "q1", "b", "q2");
        Rule(m, "q2", "a", "q1"); Rule(m, "q2", "b", "q3");
        Rule(m, "q3", "a", "q1"); Rule(m, "q3", "b", "q3");
        Rule(m, "q4", "a", "q4"); Rule(m, "q4", "b", "q4");
        return m;
    }

    private static Automaton BsDivisibleByThree()
    {
        var m = Make(MachineKind.Dfa, "Number of b's divisible by 3", "Each state remembers the count of b's mod 3.", "ab",
            [("q0", 160, 260), ("q1", 420, 260), ("q2", 680, 260)], "q0", ["q0"]);
        Rule(m, "q0", "a", "q0"); Rule(m, "q0", "b", "q1");
        Rule(m, "q1", "a", "q1"); Rule(m, "q1", "b", "q2");
        Rule(m, "q2", "a", "q2"); Rule(m, "q2", "b", "q0");
        return m;
    }

    private static Automaton NoAba()
    {
        var m = Make(MachineKind.Dfa, "Does not contain aba", "States track the longest suffix that is a prefix of aba.", "ab",
            [("q0", 140, 260), ("q1", 340, 260), ("q2", 540, 260), ("q3", 740, 260)], "q0", ["q0", "q1", "q2"]);
        Rule(m, "q0", "a", "q1"); Rule(m, "q0", "b", "q0");
        Rule(m, "q1", "a", "q1"); Rule(m, "q1", "b", "q2");
        Rule(m, "q2", "a", "q3"); Rule(m, "q2", "b", "q0");
        Rule(m, "q3", "a", "q3"); Rule(m, "q3", "b", "q3");
        return m;
    }

    private static Automaton ContainsBba()
    {
        var m = Make(MachineKind.Nfa, "Contains bba", "Labels may be whole strings: Δ ⊆ K × Σ* × K.", "ab",
            [("q0", 200, 280), ("q1", 600, 280)], "q0", ["q1"]);
        Rule(m, "q0", "a", "q0"); Rule(m, "q0", "b", "q0");
        Rule(m, "q0", "bba", "q1");
        Rule(m, "q1", "a", "q1"); Rule(m, "q1", "b", "q1");
        return m;
    }

    private static Automaton StartsBabOrEndsAba()
    {
        var m = Make(MachineKind.Nfa, "Starts with bab or ends with aba", "An e-move guesses which half of the union to check.", "ab",
            [("s", 110, 270), ("p0", 300, 160), ("p1", 560, 160), ("r0", 300, 400), ("r1", 560, 400)], "s", ["p1", "r1"]);
        Rule(m, "s", "", "p0"); Rule(m, "s", "", "r0");
        Rule(m, "p0", "bab", "p1");
        Rule(m, "p1", "a", "p1"); Rule(m, "p1", "b", "p1");
        Rule(m, "r0", "a", "r0"); Rule(m, "r0", "b", "r0");
        Rule(m, "r0", "aba", "r1");
        return m;
    }

    private static Automaton AnBn()
    {
        var m = Make(MachineKind.Pda, "aⁿbⁿ", "Push an a for every a, pop one for every b.", "ab",
            [("s", 260, 280), ("f", 620, 280)], "s", ["s", "f"]);
        m.StackAlphabet = ['a'];
        Pda(m, "s", "a", "", "s", "a");
        Pda(m, "s", "b", "a", "f", "");
        Pda(m, "f", "b", "a", "f", "");
        return m;
    }

    private static Automaton WcwReverse()
    {
        var m = Make(MachineKind.Pda, "wcwᴿ", "Copy w onto the stack, switch on c, then match wᴿ against the stack.", "abc",
            [("s", 260, 280), ("f", 620, 280)], "s", ["f"]);
        m.StackAlphabet = ['a', 'b'];
        Pda(m, "s", "a", "", "s", "a");
        Pda(m, "s", "b", "", "s", "b");
        Pda(m, "s", "c", "", "f", "");
        Pda(m, "f", "a", "a", "f", "");
        Pda(m, "f", "b", "b", "f", "");
        return m;
    }

    private static Automaton TuringAnBn()
    {
        var m = Make(MachineKind.Turing, "Decide aⁿbⁿ", "Mark the leftmost a as X and the matching b as Y until none remain.", "ab",
            [("s", 90, 150), ("q1", 230, 300), ("c", 430, 150), ("y", 800, 150), ("l", 430, 300), ("n", 800, 330), ("m", 230, 450), ("r", 430, 450), ("mb", 640, 450)],
            "s", ["y"]);
        foreach (var halt in new[] { "y", "n" }) m.FindState(halt)!.IsHalting = true;

        Tm(m, "s", Symbols.LeftEnd, "s", Symbols.MoveRight);
        Tm(m, "s", Symbols.Blank, "q1", Symbols.MoveRight);
        Tm(m, "q1", 'a', "m", "X");
        Tm(m, "q1", 'Y', "c", Symbols.MoveRight);
        Tm(m, "q1", Symbols.Blank, "y", Symbols.Blank.ToString());
        Tm(m, "q1", 'b', "n", "b");
        Tm(m, "m", 'X', "r", Symbols.MoveRight);
        Tm(m, "r", 'a', "r", Symbols.MoveRight);
        Tm(m, "r", 'Y', "r", Symbols.MoveRight);
        Tm(m, "r", 'b', "mb", "Y");
        Tm(m, "r", Symbols.Blank, "n", Symbols.Blank.ToString());
        Tm(m, "mb", 'Y', "l", Symbols.MoveLeft);
        Tm(m, "l", 'a', "l", Symbols.MoveLeft);
        Tm(m, "l", 'Y', "l", Symbols.MoveLeft);
        Tm(m, "l", 'X', "q1", Symbols.MoveRight);
        Tm(m, "c", 'Y', "c", Symbols.MoveRight);
        Tm(m, "c", Symbols.Blank, "y", Symbols.Blank.ToString());
        Tm(m, "c", 'a', "n", "a");
        Tm(m, "c", 'b', "n", "b");
        return m;
    }

    private static Automaton RewriteAsToBs()
    {
        var m = Make(MachineKind.Turing, "Rewrite every a as b", "Scan right, writing b over each a, and halt on the first blank.", "ab",
            [("s", 160, 280), ("q", 440, 280), ("h", 720, 280)], "s", []);
        m.FindState("h")!.IsHalting = true;
        Tm(m, "s", Symbols.LeftEnd, "s", Symbols.MoveRight);
        Tm(m, "s", Symbols.Blank, "q", Symbols.MoveRight);
        Tm(m, "q", 'a', "q", "b");
        Tm(m, "q", 'b', "q", Symbols.MoveRight);
        Tm(m, "q", Symbols.Blank, "h", Symbols.Blank.ToString());
        return m;
    }
}
