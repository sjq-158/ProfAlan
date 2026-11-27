namespace ProfAlan.Domain.Automata;

/// <summary>Checks shared by every machine: K, Σ and s must be well formed.</summary>
public static class DefinitionChecks
{
    public static List<DefinitionIssue> Common(Automaton m)
    {
        var issues = new List<DefinitionIssue>();

        if (m.States.Count == 0)
        {
            issues.Add(DefinitionIssue.Error("K is empty. Add at least one state."));
            return issues;
        }

        var initialCount = m.States.Count(s => s.IsInitial);
        if (initialCount == 0) issues.Add(DefinitionIssue.Error("No initial state s is marked."));
        if (initialCount > 1) issues.Add(DefinitionIssue.Error("More than one state is marked initial; s must be unique."));

        foreach (var dup in m.States.GroupBy(s => s.Name).Where(g => g.Count() > 1))
        {
            issues.Add(DefinitionIssue.Error($"State name \"{dup.Key}\" is used more than once."));
        }

        if (m.Alphabet.Count == 0) issues.Add(DefinitionIssue.Error("Σ is empty. Declare at least one input symbol."));

        foreach (var reserved in m.Alphabet.Where(Symbols.IsReserved))
        {
            issues.Add(DefinitionIssue.Error($"'{reserved}' is reserved and cannot be an input symbol."));
        }

        foreach (var rule in m.Transitions)
        {
            if (m.FindState(rule.From) is null || m.FindState(rule.To) is null)
            {
                issues.Add(DefinitionIssue.Error($"{rule.Formal(m.Kind)} refers to a state that does not exist."));
            }
        }

        return issues;
    }

    public static string? CheckInput(Automaton m, string input)
    {
        var stray = input.FirstOrDefault(c => !m.Alphabet.Contains(c));
        return stray == default ? null : $"'{stray}' is not in Σ = {{{string.Join(", ", m.Alphabet)}}}.";
    }

    public static bool HasErrors(this IEnumerable<DefinitionIssue> issues) =>
        issues.Any(i => i.Severity == IssueSeverity.Error);
}
