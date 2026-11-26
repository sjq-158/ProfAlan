namespace ProfAlan.Models.Machines;

public enum TapeMove { Left, Right, Stay }

/// <summary>
/// One edge shared by every state-based machine.
/// DFA/NFA : Read (null = epsilon)
/// PDA     : Read, Pop, Push
/// TM      : Read, Write, Move
/// </summary>
public sealed record Transition(
    string From,
    string To,
    char? Read,
    char? Write = null,
    TapeMove? Move = null,
    char? Pop = null,
    string? Push = null)
{
    public string Label
    {
        get
        {
            var read = Read?.ToString() ?? "ε";

            if (Move is TapeMove move)
            {
                return $"{read}→{Write?.ToString() ?? read}, {move.ToString()[0]}";
            }

            if (Pop is not null || Push is not null)
            {
                var pop = Pop?.ToString() ?? "ε";
                var push = string.IsNullOrEmpty(Push) ? "ε" : Push;
                return $"{read}, {pop}→{push}";
            }

            return read;
        }
    }
}