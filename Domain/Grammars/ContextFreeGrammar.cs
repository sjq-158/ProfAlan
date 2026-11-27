namespace ProfAlan.Domain.Grammars;

/// <summary>A rule A → u. An empty right-hand side is the rule A → e.</summary>
public sealed record GrammarRule(char Variable, string Body)
{
    public override string ToString() => $"{Variable} → {(Body.Length == 0 ? "e" : Body)}";
}

public sealed class GrammarSyntaxException(string message, int line) : Exception(message)
{
    public int Line { get; } = line;
}

/// <summary>
/// G = (V, Σ, R, S). Variables are uppercase letters; every other symbol is a terminal.
/// The start symbol is the left-hand side of the first rule.
/// </summary>
public sealed class ContextFreeGrammar
{
    public IReadOnlyList<GrammarRule> Rules { get; }
    public char Start { get; }
    public IReadOnlySet<char> Variables { get; }
    public IReadOnlySet<char> Terminals { get; }

    private readonly HashSet<char> nullable;

    public ContextFreeGrammar(IReadOnlyList<GrammarRule> rules)
    {
        if (rules.Count == 0) throw new GrammarSyntaxException("Write at least one rule, for example S → aSb | e.", 1);

        Rules = rules;
        Start = rules[0].Variable;
        Variables = rules.Select(r => r.Variable).Concat(rules.SelectMany(r => r.Body.Where(IsVariable))).ToHashSet();
        Terminals = rules.SelectMany(r => r.Body.Where(c => !IsVariable(c))).ToHashSet();
        nullable = ComputeNullable();
    }

    public static bool IsVariable(char c) => c is >= 'A' and <= 'Z';

    public bool IsNullable(char variable) => nullable.Contains(variable);

    public IEnumerable<GrammarRule> RulesFor(char variable) => Rules.Where(r => r.Variable == variable);

    /// <summary>Variables used on a right-hand side that never appear on a left-hand side.</summary>
    public IEnumerable<char> UndefinedVariables() =>
        Variables.Where(v => Rules.All(r => r.Variable != v)).OrderBy(v => v);

    /// <summary>Parses lines like "S -> aSb | e". Accepts ->, → and ::= as the arrow.</summary>
    public static ContextFreeGrammar Parse(string text)
    {
        var rules = new List<GrammarRule>();
        var lines = text.Replace("\r", "").Split('\n');

        for (var i = 0; i < lines.Length; i++)
        {
            var line = lines[i].Trim();
            if (line.Length == 0 || line.StartsWith("//")) continue;

            var parts = line.Split(["->", "→", "::="], 2, StringSplitOptions.None);
            if (parts.Length != 2)
            {
                throw new GrammarSyntaxException($"Line {i + 1}: expected a rule like S → aSb | e.", i + 1);
            }

            var head = parts[0].Trim();
            if (head.Length != 1 || !IsVariable(head[0]))
            {
                throw new GrammarSyntaxException($"Line {i + 1}: the left-hand side must be a single variable (A–Z), found \"{head}\".", i + 1);
            }

            foreach (var alternative in parts[1].Split('|'))
            {
                var body = new string(alternative.Where(c => !char.IsWhiteSpace(c) && c is not ('e' or 'ε')).ToArray());
                var rule = new GrammarRule(head[0], body);
                if (!rules.Contains(rule)) rules.Add(rule);
            }
        }

        return new ContextFreeGrammar(rules);
    }

    private HashSet<char> ComputeNullable()
    {
        var result = new HashSet<char>();
        bool changed;
        do
        {
            changed = false;
            foreach (var rule in Rules)
            {
                if (!result.Contains(rule.Variable) && rule.Body.All(result.Contains))
                {
                    changed |= result.Add(rule.Variable);
                }
            }
        } while (changed);
        return result;
    }

    public string FormalDefinition()
    {
        var v = string.Join(", ", Variables.OrderBy(c => c != Start).ThenBy(c => c));
        var sigma = string.Join(", ", Terminals.OrderBy(c => c));
        return $"G = (V, Σ, R, {Start}), V = {{{v}}}, Σ = {{{sigma}}}";
    }
}
