namespace ProfAlan.Domain.Automata;

/// <summary>
/// M = (K, Σ, δ, s, H) with δ : (K − H) × Σ → K × (Σ ∪ {←, →}).
/// When some halting state is marked accepting (y), halting there accepts and halting elsewhere (n) rejects.
/// Otherwise M semidecides: reaching any halting state accepts.
/// </summary>
public sealed class TuringEngine(Automaton machine) : IMachineEngine
{
    public const int StepBudget = 10_000;

    public IReadOnlyList<DefinitionIssue> Validate()
    {
        var issues = DefinitionChecks.Common(machine);
        if (issues.HasErrors()) return issues;

        if (!machine.States.Any(s => s.IsHalting))
        {
            issues.Add(DefinitionIssue.Error("H is empty. Mark at least one halting state."));
        }

        if (machine.InitialState is { IsHalting: true } s)
        {
            issues.Add(DefinitionIssue.Warning($"The initial state {s.Name} is a halting state, so M halts immediately."));
        }

        foreach (var rule in machine.Transitions)
        {
            if (rule.Read.Length != 1)
            {
                issues.Add(DefinitionIssue.Error($"{rule.Formal(MachineKind.Turing)} must read exactly one tape symbol."));
            }
            if (!Symbols.IsMove(rule.Action) && rule.Action.Length != 1)
            {
                issues.Add(DefinitionIssue.Error($"{rule.Formal(MachineKind.Turing)} must write one symbol or move ← / →."));
            }
            if (machine.FindState(rule.From)?.IsHalting == true)
            {
                issues.Add(DefinitionIssue.Error($"{rule.Formal(MachineKind.Turing)} leaves halting state {rule.From}; δ is only defined on K − H."));
            }
            if (rule.Read == Symbols.LeftEnd.ToString() && rule.Action != Symbols.MoveRight)
            {
                issues.Add(DefinitionIssue.Error($"{rule.Formal(MachineKind.Turing)}: on ▷ the head must move →."));
            }
            if (rule.Action == Symbols.LeftEnd.ToString())
            {
                issues.Add(DefinitionIssue.Warning($"{rule.Formal(MachineKind.Turing)} writes ▷, which should only appear in cell 0."));
            }
        }

        foreach (var clash in machine.Transitions.GroupBy(r => (r.From, r.Read)).Where(g => g.Count() > 1))
        {
            issues.Add(DefinitionIssue.Error($"δ({clash.Key.From}, {clash.Key.Read}) has more than one value; δ must be a function."));
        }

        var tapeAlphabet = machine.TapeAlphabet();
        var missing = machine.States
            .Where(q => !q.IsHalting)
            .SelectMany(q => tapeAlphabet.Select(a => (q.Name, a)))
            .Where(p => !machine.Transitions.Any(t => t.From == p.Name && t.Read == p.a.ToString()))
            .Select(p => $"δ({p.Name}, {p.a})")
            .ToList();
        if (missing.Count > 0)
        {
            var preview = string.Join(", ", missing.Take(4)) + (missing.Count > 4 ? $" and {missing.Count - 4} more" : "");
            issues.Add(DefinitionIssue.Warning($"δ is partial: {preview} undefined. M hangs if it ever reaches one of these."));
        }

        return issues;
    }

    public ComputationResult Run(string input)
    {
        if (DefinitionChecks.CheckInput(machine, input) is { } bad) return ComputationResult.Invalid(bad);
        if (machine.InitialState is not { } start) return ComputationResult.Invalid("The machine has no initial state.");

        var tape = new Tape(input);
        var state = machine.FindState(start.Name)!;
        var path = new List<Configuration> { Snapshot(state.Name, tape, null) };
        var decides = machine.States.Any(s => s.IsHalting && s.IsFinal);

        for (var step = 0; step < StepBudget; step++)
        {
            if (state.IsHalting)
            {
                var accepted = !decides || state.IsFinal;
                var output = Symbols.Show(tape.Output);
                return new ComputationResult(accepted ? Verdict.Accepted : Verdict.Rejected, path,
                    decides
                        ? $"Halted in {state.Name}, which {(accepted ? "is the accepting state y" : "is a rejecting halting state")}. Tape: {output}."
                        : $"Halted in {state.Name} ∈ H after {step} steps. Tape: {output}.");
            }

            var scanned = tape.Read();
            var rule = machine.Transitions.FirstOrDefault(t => t.From == state.Name && t.Read == scanned.ToString());
            if (rule is null)
            {
                return new ComputationResult(Verdict.Rejected, path,
                    $"δ({state.Name}, {scanned}) is undefined, so M hangs without reaching H.");
            }

            switch (rule.Action)
            {
                case Symbols.MoveLeft:
                    tape.MoveLeft();
                    break;
                case Symbols.MoveRight:
                    tape.MoveRight();
                    break;
                default:
                    tape.Write(rule.Action[0]);
                    break;
            }

            state = machine.FindState(rule.To)!;
            path.Add(Snapshot(state.Name, tape, rule.Id));
        }

        return new ComputationResult(Verdict.DidNotHalt, path,
            $"M did not halt within {StepBudget:N0} steps. It may loop forever on this input.");
    }

    private static Configuration Snapshot(string state, Tape tape, string? ruleId) =>
        new(state, Tape: tape.Contents, Head: tape.Head, RuleId: ruleId);
}
