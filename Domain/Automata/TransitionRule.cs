namespace ProfAlan.Domain.Automata;

/// <summary>
/// One element of δ or Δ. Which fields matter depends on the machine:
/// DFA (q, a) → p; NFA (q, u, p) with u ∈ Σ*; PDA ((q, a, β), (p, γ)); TM δ(q, a) = (p, b) with b a symbol or a move.
/// </summary>
public sealed class TransitionRule
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N")[..8];
    public string From { get; set; } = "";
    public string To { get; set; } = "";

    /// <summary>Input symbol (DFA, PDA, TM) or input string (NFA). Empty means e.</summary>
    public string Read { get; set; } = "";

    /// <summary>PDA only: β, the string popped from the top of the stack.</summary>
    public string Pop { get; set; } = "";

    /// <summary>PDA only: γ, the string pushed onto the stack.</summary>
    public string Push { get; set; } = "";

    /// <summary>TM only: the symbol written, or ← / →.</summary>
    public string Action { get; set; } = "";

    public string Label(MachineKind kind) => kind switch
    {
        MachineKind.Pda => $"{Symbols.Show(Read)}, {Symbols.Show(Pop)}/{Symbols.Show(Push)}",
        MachineKind.Turing => $"{Symbols.Show(Read)}/{Symbols.Show(Action)}",
        _ => Symbols.Show(Read)
    };

    public string Formal(MachineKind kind) => kind switch
    {
        MachineKind.Dfa => $"δ({From}, {Symbols.Show(Read)}) = {To}",
        MachineKind.Nfa => $"({From}, {Symbols.Show(Read)}, {To})",
        MachineKind.Pda => $"(({From}, {Symbols.Show(Read)}, {Symbols.Show(Pop)}), ({To}, {Symbols.Show(Push)}))",
        MachineKind.Turing => $"δ({From}, {Symbols.Show(Read)}) = ({To}, {Symbols.Show(Action)})",
        _ => ""
    };

    public TransitionRule Copy() => new()
    {
        Id = Id, From = From, To = To, Read = Read, Pop = Pop, Push = Push, Action = Action
    };
}
