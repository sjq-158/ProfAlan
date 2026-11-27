namespace ProfAlan.Domain.Automata;

public enum MachineKind
{
    Dfa,
    Nfa,
    Pda,
    Turing
}

public static class MachineKindNames
{
    public static string ShortName(this MachineKind kind) => kind switch
    {
        MachineKind.Dfa => "DFA",
        MachineKind.Nfa => "NFA",
        MachineKind.Pda => "PDA",
        MachineKind.Turing => "TM",
        _ => kind.ToString()
    };

    public static string FullName(this MachineKind kind) => kind switch
    {
        MachineKind.Dfa => "Deterministic Finite Automaton",
        MachineKind.Nfa => "Nondeterministic Finite Automaton",
        MachineKind.Pda => "Pushdown Automaton",
        MachineKind.Turing => "Turing Machine",
        _ => kind.ToString()
    };

    public static string Slug(this MachineKind kind) => kind.ShortName().ToLowerInvariant();

    public static MachineKind? FromSlug(string? slug) => slug?.ToLowerInvariant() switch
    {
        "dfa" => MachineKind.Dfa,
        "nfa" => MachineKind.Nfa,
        "pda" => MachineKind.Pda,
        "tm" or "turing" => MachineKind.Turing,
        _ => null
    };

    /// <summary>The name Lewis &amp; Papadimitriou give the transition component of the tuple.</summary>
    public static string TransitionSymbol(this MachineKind kind) =>
        kind is MachineKind.Dfa or MachineKind.Turing ? "δ" : "Δ";
}
