namespace ProfAlan.Domain.Automata;

/// <summary>
/// M = (K, Σ, Γ, Δ, s, F) with Δ ⊆ (K × (Σ ∪ {e}) × Γ*) × (K × Γ*).
/// M accepts w iff (s, w, e) ⊢* (p, e, e) for some p ∈ F.
/// </summary>
public sealed class PdaEngine(Automaton machine) : IMachineEngine
{
    public const int ConfigurationBudget = 50_000;

    public IReadOnlyList<DefinitionIssue> Validate()
    {
        var issues = DefinitionChecks.Common(machine);
        if (issues.HasErrors()) return issues;

        if (machine.StackAlphabet.Count == 0)
        {
            issues.Add(DefinitionIssue.Warning("Γ is empty, so the stack can never be used."));
        }

        foreach (var rule in machine.Transitions)
        {
            if (rule.Read.Length > 1 || (rule.Read.Length == 1 && !machine.Alphabet.Contains(rule.Read[0])))
            {
                issues.Add(DefinitionIssue.Error($"{rule.Formal(MachineKind.Pda)} reads \"{rule.Read}\"; it must be one symbol of Σ or e."));
            }
            var stray = (rule.Pop + rule.Push).FirstOrDefault(c => !machine.StackAlphabet.Contains(c));
            if (stray != default)
            {
                issues.Add(DefinitionIssue.Error($"{rule.Formal(MachineKind.Pda)} uses '{stray}', which is not in Γ."));
            }
        }

        if (!machine.States.Any(s => s.IsFinal))
        {
            issues.Add(DefinitionIssue.Warning("F is empty, so L(M) = ∅."));
        }

        return issues;
    }

    public ComputationResult Run(string input)
    {
        if (DefinitionChecks.CheckInput(machine, input) is { } bad) return ComputationResult.Invalid(bad);
        if (machine.InitialState is not { } start) return ComputationResult.Invalid("The machine has no initial state.");

        var stackCap = input.Length * 2 + 24;
        var root = new Configuration(start.Name, input, 0, "");
        var parent = new Dictionary<(string, int, string), Configuration?> { [Key(root)] = null };
        var queue = new Queue<Configuration>([root]);
        var deepest = root;
        var truncated = false;

        while (queue.Count > 0)
        {
            var c = queue.Dequeue();
            if (c.Position > deepest.Position || (c.Position == deepest.Position && c.Stack.Length < deepest.Stack.Length))
            {
                deepest = c;
            }

            if (c.Position == input.Length && c.Stack.Length == 0 && machine.FindState(c.State)?.IsFinal == true)
            {
                return new ComputationResult(Verdict.Accepted, Trace(c, parent),
                    $"(s, w, e) ⊢* ({c.State}, e, e) with {c.State} ∈ F: input read and stack empty.", parent.Count);
            }

            foreach (var rule in machine.Transitions.Where(t => t.From == c.State))
            {
                if (!input.AsSpan(c.Position).StartsWith(rule.Read)) continue;
                if (!c.Stack.StartsWith(rule.Pop, StringComparison.Ordinal)) continue;

                var stack = rule.Push + c.Stack[rule.Pop.Length..];
                if (stack.Length > stackCap)
                {
                    truncated = true;
                    continue;
                }

                var next = new Configuration(rule.To, input, c.Position + rule.Read.Length, stack, RuleId: rule.Id);
                var key = Key(next);
                if (parent.ContainsKey(key)) continue;

                parent[key] = c;
                queue.Enqueue(next);

                if (parent.Count >= ConfigurationBudget)
                {
                    return new ComputationResult(Verdict.DidNotHalt, Trace(deepest, parent),
                        $"Stopped after exploring {ConfigurationBudget:N0} configurations without finding an accepting computation.",
                        parent.Count);
                }
            }
        }

        var note = truncated ? $" Branches whose stack grew beyond {stackCap} symbols were pruned." : "";
        return new ComputationResult(Verdict.Rejected, Trace(deepest, parent),
            $"No computation reaches (f, e, e) with f ∈ F after exploring {parent.Count} configurations.{note} Showing the branch that read the most input.",
            parent.Count);
    }

    private static (string, int, string) Key(Configuration c) => (c.State, c.Position, c.Stack);

    private static List<Configuration> Trace(Configuration end, Dictionary<(string, int, string), Configuration?> parent)
    {
        var path = new List<Configuration>();
        Configuration? cursor = end;
        while (cursor is not null)
        {
            path.Add(cursor);
            cursor = parent[Key(cursor)];
        }
        path.Reverse();
        return path;
    }
}
