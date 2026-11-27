namespace ProfAlan.Domain.Automata;

public interface IMachineEngine
{
    IReadOnlyList<DefinitionIssue> Validate();
    ComputationResult Run(string input);
}

public static class MachineEngines
{
    public static IMachineEngine For(Automaton machine) => machine.Kind switch
    {
        MachineKind.Dfa => new DfaEngine(machine),
        MachineKind.Nfa => new NfaEngine(machine),
        MachineKind.Pda => new PdaEngine(machine),
        MachineKind.Turing => new TuringEngine(machine),
        _ => throw new NotSupportedException(machine.Kind.ToString())
    };
}
