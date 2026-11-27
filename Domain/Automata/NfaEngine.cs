namespace ProfAlan.Domain.Automata;

/// <summary>M = (K, Σ, Δ, s, F) with Δ ⊆ K × Σ* × K.</summary>
public sealed class NfaEngine(Automaton machine) : IMachineEngine
{
    public IReadOnlyList<DefinitionIssue> Validate()
    {
        var issues = DefinitionChecks.Common(machine);
        if (issues.HasErrors()) return issues;

        foreach (var rule in machine.Transitions.Where(r => r.Read.Any(c => !machine.Alphabet.Contains(c))))
        {
            issues.Add(DefinitionIssue.Error($"{rule.Formal(MachineKind.Nfa)} reads \"{rule.Read}\", which is not a string over Σ."));
        }

        if (!machine.States.Any(s => s.IsFinal))
        {
            issues.Add(DefinitionIssue.Warning("F is empty, so L(M) = ∅."));
        }

        return issues;
    }

    /// <summary>
    /// Breadth-first search over configurations (q, position). The state space is finite,
    /// so the search always terminates with a definite answer.
    /// </summary>
    public ComputationResult Run(string input)
    {
        if (DefinitionChecks.CheckInput(machine, input) is { } bad) return ComputationResult.Invalid(bad);
        if (machine.InitialState is not { } start) return ComputationResult.Invalid("The machine has no initial state.");

        var root = new Configuration(start.Name, input, 0);
        var parent = new Dictionary<(string, int), Configuration?> { [(root.State, 0)] = null };
        var nodes = new Dictionary<(string, int), Configuration> { [(root.State, 0)] = root };
        var queue = new Queue<Configuration>([root]);
        var deepest = root;

        while (queue.Count > 0)
        {
            var c = queue.Dequeue();
            if (c.Position > deepest.Position) deepest = c;

            if (c.Position == input.Length && machine.FindState(c.State)?.IsFinal == true)
            {
                var path = Trace(c, parent, nodes);
                return new ComputationResult(Verdict.Accepted, path,
                    $"Found an accepting computation ending in ({c.State}, e) with {c.State} ∈ F.", parent.Count);
            }

            foreach (var rule in machine.Transitions.Where(t => t.From == c.State))
            {
                if (!input.AsSpan(c.Position).StartsWith(rule.Read)) continue;

                var next = new Configuration(rule.To, input, c.Position + rule.Read.Length, RuleId: rule.Id);
                var key = (next.State, next.Position);
                if (parent.ContainsKey(key)) continue;

                parent[key] = c;
                nodes[key] = next;
                queue.Enqueue(next);
            }
        }

        return new ComputationResult(Verdict.Rejected, Trace(deepest, parent, nodes),
            $"All {parent.Count} reachable configurations were explored; none is (f, e) with f ∈ F. Showing the branch that read the most input.",
            parent.Count);
    }

    private static List<Configuration> Trace(
        Configuration end,
        Dictionary<(string, int), Configuration?> parent,
        Dictionary<(string, int), Configuration> nodes)
    {
        var path = new List<Configuration>();
        Configuration? cursor = nodes[(end.State, end.Position)];
        while (cursor is not null)
        {
            path.Add(cursor);
            cursor = parent[(cursor.State, cursor.Position)];
        }
        path.Reverse();
        return path;
    }
}
