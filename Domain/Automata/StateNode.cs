namespace ProfAlan.Domain.Automata;

public sealed class StateNode
{
    public string Name { get; set; } = "";
    public double X { get; set; }
    public double Y { get; set; }
    public bool IsInitial { get; set; }

    /// <summary>F for DFA/NFA/PDA. For a TM this marks the accepting halting state y.</summary>
    public bool IsFinal { get; set; }

    /// <summary>Membership in H. Only meaningful for Turing machines.</summary>
    public bool IsHalting { get; set; }
}
