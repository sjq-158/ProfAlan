using ProfAlan.Domain.Automata;

namespace ProfAlan.Domain.Grammars;

/// <summary>
/// The construction from Lewis &amp; Papadimitriou, Lemma 3.4.1: a two-state PDA that guesses a
/// leftmost derivation on its stack. Δ contains ((p, e, e), (q, S)), ((q, e, A), (q, x)) for every
/// rule A → x, and ((q, a, a), (q, e)) for every terminal a.
/// </summary>
public static class GrammarToPda
{
    public static Automaton Build(ContextFreeGrammar grammar)
    {
        var machine = new Automaton
        {
            Kind = MachineKind.Pda,
            Name = "PDA from grammar",
            Description = "Accepts L(G) by simulating leftmost derivations on the stack.",
            Alphabet = grammar.Terminals.OrderBy(c => c).ToList(),
            StackAlphabet = grammar.Variables.Concat(grammar.Terminals).Distinct().OrderBy(c => c).ToList(),
            States =
            [
                new StateNode { Name = "p", X = 180, Y = 260, IsInitial = true },
                new StateNode { Name = "q", X = 520, Y = 260, IsFinal = true }
            ]
        };

        machine.Transitions.Add(new TransitionRule { From = "p", To = "q", Push = grammar.Start.ToString() });
        foreach (var rule in grammar.Rules)
        {
            machine.Transitions.Add(new TransitionRule { From = "q", To = "q", Pop = rule.Variable.ToString(), Push = rule.Body });
        }
        foreach (var a in grammar.Terminals.OrderBy(c => c))
        {
            machine.Transitions.Add(new TransitionRule { From = "q", To = "q", Read = a.ToString(), Pop = a.ToString() });
        }
        return machine;
    }
}
