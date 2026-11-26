namespace ProfAlan.Models;

public enum MachineKind
{
    Dfa,
    Nfa,
    Pda,
    TuringMachine,
    Cfg
}

public static class MachineKindExtensions
{
    public static string DisplayName(this MachineKind kind) => kind switch
    {
        MachineKind.Dfa => "Deterministic Finite Automaton",
        MachineKind.Nfa => "Nondeterministic Finite Automaton",
        MachineKind.Pda => "Pushdown Automaton",
        MachineKind.TuringMachine => "Turing Machine",
        MachineKind.Cfg => "Context-Free Grammar",
        _ => kind.ToString()
    };
}