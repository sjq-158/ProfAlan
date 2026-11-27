namespace ProfAlan.Domain.Automata;

/// <summary>M = (K, Σ, δ, s, F) with δ : K × Σ → K.</summary>
public sealed class DfaEngine(Automaton machine) : IMachineEngine
{
    public IReadOnlyList<DefinitionIssue> Validate()
    {
        var issues = DefinitionChecks.Common(machine);
        if (issues.HasErrors()) return issues;

        foreach (var rule in machine.Transitions.Where(r => r.Read.Length != 1 || !machine.Alphabet.Contains(r.Read[0])))
        {
            issues.Add(DefinitionIssue.Error($"{rule.Formal(MachineKind.Dfa)} reads \"{Symbols.Show(rule.Read)}\", which is not a single symbol of Σ."));
        }

        foreach (var clash in machine.Transitions.GroupBy(r => (r.From, r.Read)).Where(g => g.Select(r => r.To).Distinct().Count() > 1))
        {
            issues.Add(DefinitionIssue.Error($"δ({clash.Key.From}, {clash.Key.Read}) has more than one value; a DFA must be deterministic."));
        }

        var missing = machine.States
            .SelectMany(q => machine.Alphabet.Select(a => (q.Name, a)))
            .Where(p => !machine.Transitions.Any(t => t.From == p.Name && t.Read == p.a.ToString()))
            .Select(p => $"δ({p.Name}, {p.a})")
            .ToList();
        if (missing.Count > 0)
        {
            var preview = string.Join(", ", missing.Take(4)) + (missing.Count > 4 ? $" and {missing.Count - 4} more" : "");
            issues.Add(DefinitionIssue.Error($"δ is not total: {preview} undefined.", IssueFix.AddTrapState));
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

        var current = start.Name;
        var path = new List<Configuration> { new(current, input, 0) };

        for (var i = 0; i < input.Length; i++)
        {
            var rule = machine.Transitions.FirstOrDefault(t => t.From == current && t.Read == input[i].ToString());
            if (rule is null)
            {
                return new ComputationResult(Verdict.Rejected, path,
                    $"δ({current}, {input[i]}) is undefined, so the computation cannot continue.");
            }

            current = rule.To;
            path.Add(new Configuration(current, input, i + 1, RuleId: rule.Id));
        }

        var accepted = machine.FindState(current)?.IsFinal == true;
        return new ComputationResult(
            accepted ? Verdict.Accepted : Verdict.Rejected,
            path,
            accepted
                ? $"(s, w) ⊢* ({current}, e) and {current} ∈ F."
                : $"Input consumed in {current}, which is not in F.");
    }
}
