namespace ProfAlan.Domain.Automata;

/// <summary>
/// A snapshot of a machine mid-computation. Finite automata use (q, w); PDAs add the stack
/// (top written first); Turing machines carry the whole tape and the head position instead.
/// </summary>
public sealed record Configuration(
    string State,
    string Input = "",
    int Position = 0,
    string Stack = "",
    string Tape = "",
    int Head = 0,
    string? RuleId = null)
{
    public string Remaining => Position >= Input.Length ? "" : Input[Position..];
    public string Consumed => Input[..Math.Min(Position, Input.Length)];
}

public enum Verdict
{
    Accepted,
    Rejected,
    DidNotHalt,
    InvalidInput
}

public sealed record ComputationResult(
    Verdict Verdict,
    IReadOnlyList<Configuration> Path,
    string Explanation,
    int ConfigurationsExplored = 0)
{
    public bool Accepted => Verdict == Verdict.Accepted;

    public static ComputationResult Invalid(string reason) => new(Verdict.InvalidInput, [], reason);
}

public enum IssueSeverity
{
    Error,
    Warning
}

public enum IssueFix
{
    None,
    AddTrapState
}

public sealed record DefinitionIssue(IssueSeverity Severity, string Message, IssueFix Fix = IssueFix.None)
{
    public static DefinitionIssue Error(string message, IssueFix fix = IssueFix.None) => new(IssueSeverity.Error, message, fix);
    public static DefinitionIssue Warning(string message) => new(IssueSeverity.Warning, message);
}

public static class ConfigurationNotation
{
    /// <summary>Formats a configuration as it is written in the textbook, e.g. (q, aab) or (q, ▷⊔a[b]).</summary>
    public static string Format(MachineKind kind, Configuration c) => kind switch
    {
        MachineKind.Pda => $"({c.State}, {Symbols.Show(c.Remaining)}, {Symbols.Show(c.Stack)})",
        MachineKind.Turing => $"({c.State}, {TapeWithHead(c)})",
        _ => $"({c.State}, {Symbols.Show(c.Remaining)})"
    };

    private static string TapeWithHead(Configuration c)
    {
        var tape = c.Tape.TrimEnd(Symbols.Blank);
        if (c.Head >= tape.Length) tape = tape.PadRight(c.Head + 1, Symbols.Blank);
        return $"{tape[..c.Head]}[{tape[c.Head]}]{tape[(c.Head + 1)..]}";
    }
}
