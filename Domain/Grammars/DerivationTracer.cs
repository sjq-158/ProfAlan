namespace ProfAlan.Domain.Grammars;

/// <summary>One step S ⇒ ... of a leftmost derivation; Position is the variable that was rewritten.</summary>
public sealed record DerivationStep(string Form, GrammarRule? Rule, int Position);

public sealed class ParseTreeNode(char symbol)
{
    public char Symbol { get; } = symbol;
    public List<ParseTreeNode> Children { get; } = [];
    public bool IsLeaf => Children.Count == 0;
}

public sealed record Derivation(IReadOnlyList<DerivationStep> Steps, ParseTreeNode Tree);

public static class DerivationTracer
{
    public const int SearchBudget = 150_000;

    /// <summary>
    /// Breadth-first search over leftmost sentential forms, so the derivation found is a shortest one.
    /// Forms are pruned when their fixed terminal prefix disagrees with w or they already need more
    /// terminals than w has. Returns null when no derivation exists within the budget.
    /// </summary>
    public static Derivation? FindLeftmost(ContextFreeGrammar grammar, string target)
    {
        var start = grammar.Start.ToString();
        var parent = new Dictionary<string, (string Form, GrammarRule Rule, int Position)?> { [start] = null };
        var queue = new Queue<string>([start]);
        var lengthCap = target.Length + 2 * grammar.Variables.Count + 4;

        while (queue.Count > 0 && parent.Count < SearchBudget)
        {
            var form = queue.Dequeue();
            if (form == target) return Build(form, parent, grammar.Start);

            var position = form.AsSpan().IndexOfAnyInRange('A', 'Z');
            if (position < 0) continue;

            foreach (var rule in grammar.RulesFor(form[position]))
            {
                var next = string.Concat(form.AsSpan(0, position), rule.Body, form.AsSpan(position + 1));
                if (!IsPromising(grammar, next, target, lengthCap) || parent.ContainsKey(next)) continue;

                parent[next] = (form, rule, position);
                queue.Enqueue(next);
            }
        }
        return null;
    }

    private static bool IsPromising(ContextFreeGrammar grammar, string form, string target, int lengthCap)
    {
        if (form.Length > lengthCap) return false;

        var minimumYield = 0;
        var prefixFixed = true;
        for (var i = 0; i < form.Length; i++)
        {
            var c = form[i];
            if (ContextFreeGrammar.IsVariable(c))
            {
                prefixFixed = false;
                if (!grammar.IsNullable(c)) minimumYield++;
                continue;
            }

            minimumYield++;
            if (prefixFixed && (i >= target.Length || target[i] != c)) return false;
        }
        return minimumYield <= target.Length;
    }

    private static Derivation Build(
        string end,
        Dictionary<string, (string Form, GrammarRule Rule, int Position)?> parent,
        char start)
    {
        var steps = new List<DerivationStep>();
        var cursor = end;
        GrammarRule? appliedNext = null;
        var positionNext = -1;

        while (parent[cursor] is { } link)
        {
            steps.Add(new DerivationStep(cursor, appliedNext, positionNext));
            appliedNext = link.Rule;
            positionNext = link.Position;
            cursor = link.Form;
        }
        steps.Add(new DerivationStep(cursor, appliedNext, positionNext));
        steps.Reverse();

        return new Derivation(steps, BuildTree(steps, start));
    }

    /// <summary>Replays the derivation, expanding the leftmost unexpanded variable leaf each time.</summary>
    private static ParseTreeNode BuildTree(IReadOnlyList<DerivationStep> steps, char start)
    {
        var root = new ParseTreeNode(start);
        var frontier = new List<ParseTreeNode> { root };

        foreach (var step in steps)
        {
            if (step.Rule is not { } rule) continue;

            var index = frontier.FindIndex(n => n.IsLeaf && ContextFreeGrammar.IsVariable(n.Symbol));
            var node = frontier[index];
            var children = rule.Body.Length == 0
                ? [new ParseTreeNode('e')]
                : rule.Body.Select(c => new ParseTreeNode(c)).ToList();
            node.Children.AddRange(children);

            frontier.RemoveAt(index);
            frontier.InsertRange(index, rule.Body.Length == 0 ? [] : children);
        }
        return root;
    }
}
