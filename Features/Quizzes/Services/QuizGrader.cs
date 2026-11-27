using System.Text.Json;
using ProfAlan.Domain.Automata;
using ProfAlan.Domain.Grammars;
using ProfAlan.Domain.Regular;
using ProfAlan.Features.Quizzes.Models;

namespace ProfAlan.Features.Quizzes.Services;

/// <summary>
/// Runs a submitted answer against every test case. A machine must first be a valid instance
/// of its kind; an answer that is not well formed fails every case with the reason attached.
/// </summary>
public static class QuizGrader
{
    public static GradeReport EvaluateTestCases(Quiz quiz, string answer, IReadOnlyList<TestCase> cases)
    {
        var notes = new List<string>();
        Func<string, (Verdict, string)> decide;

        try
        {
            decide = quiz.AnswerKind switch
            {
                AnswerKind.Regex => RegexDecider(answer),
                AnswerKind.Grammar => GrammarDecider(answer),
                _ => MachineDecider(quiz, answer, notes)
            };
        }
        catch (AnswerException ex)
        {
            var failed = cases.Select((c, i) => new CaseResult(i + 1, c.IsHidden, c.IsHidden ? null : c.Input, c.ShouldAccept, Verdict.InvalidInput, ex.Message)).ToList();
            return new GradeReport(failed, ex.Message, notes);
        }

        var results = cases.Select((c, i) =>
        {
            var (verdict, detail) = decide(c.Input);
            return new CaseResult(i + 1, c.IsHidden, c.IsHidden ? null : c.Input, c.ShouldAccept, verdict, c.IsHidden ? "" : detail);
        }).ToList();

        return new GradeReport(results, null, notes);
    }

    private sealed class AnswerException(string message) : Exception(message);

    private static Func<string, (Verdict, string)> MachineDecider(Quiz quiz, string answer, List<string> notes)
    {
        Automaton machine;
        try
        {
            machine = Automaton.FromJson(answer);
        }
        catch (JsonException)
        {
            throw new AnswerException("The submitted machine could not be read.");
        }

        var expected = quiz.AnswerKind.Machine();
        if (machine.Kind != expected)
        {
            throw new AnswerException($"This problem expects a {quiz.AnswerKind.Label()}, but a {machine.Kind.ShortName()} was submitted.");
        }

        var engine = MachineEngines.For(machine);
        var errors = engine.Validate().Where(i => i.Severity == IssueSeverity.Error).ToList();
        if (errors.Count > 0)
        {
            throw new AnswerException($"Not a valid {machine.Kind.ShortName()}: {errors[0].Message}");
        }

        var missing = quiz.Alphabet.Where(c => !machine.Alphabet.Contains(c)).Distinct().ToList();
        if (missing.Count > 0)
        {
            notes.Add($"The problem's alphabet includes {string.Join(", ", missing)}, which your Σ does not declare.");
        }

        return input =>
        {
            var result = engine.Run(input);
            return (result.Verdict, result.Explanation);
        };
    }

    private static Func<string, (Verdict, string)> RegexDecider(string answer)
    {
        if (!RegexParser.TryParse(answer, out var regex, out var error)) throw new AnswerException($"Regular expression error: {error}");
        var nfa = EpsilonNfa.FromRegex(regex!);
        return input => nfa.Accepts(input) ? (Verdict.Accepted, "w ∈ L(R)") : (Verdict.Rejected, "w ∉ L(R)");
    }

    private static Func<string, (Verdict, string)> GrammarDecider(string answer)
    {
        ContextFreeGrammar grammar;
        try
        {
            grammar = ContextFreeGrammar.Parse(answer);
        }
        catch (GrammarSyntaxException ex)
        {
            throw new AnswerException($"Grammar error: {ex.Message}");
        }
        return input => EarleyRecognizer.Accepts(grammar, input) ? (Verdict.Accepted, "S ⇒* w") : (Verdict.Rejected, "no derivation of w");
    }

    /// <summary>Labels test strings using a reference regular expression or grammar the author trusts.</summary>
    public static Func<string, bool>? ReferenceOracle(string reference, out string? error)
    {
        error = null;
        if (RegexParser.TryParse(reference, out var regex, out var regexError))
        {
            var nfa = EpsilonNfa.FromRegex(regex!);
            return nfa.Accepts;
        }
        if (reference.Contains("->") || reference.Contains('→'))
        {
            try
            {
                var grammar = ContextFreeGrammar.Parse(reference);
                return w => EarleyRecognizer.Accepts(grammar, w);
            }
            catch (GrammarSyntaxException ex)
            {
                error = ex.Message;
                return null;
            }
        }
        error = regexError;
        return null;
    }

    /// <summary>Every string over Σ up to the given length, shortest first.</summary>
    public static IEnumerable<string> AllStrings(string alphabet, int maxLength)
    {
        var symbols = alphabet.Distinct().OrderBy(c => c).ToArray();
        var layer = new List<string> { "" };
        for (var length = 0; length <= maxLength; length++)
        {
            foreach (var w in layer) yield return w;
            layer = layer.SelectMany(w => symbols.Select(c => w + c)).ToList();
        }
    }
}
