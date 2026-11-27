using ProfAlan.Domain.Automata;
using ProfAlan.Features.Quizzes.Models;
using ProfAlan.Features.Quizzes.Services;
using ProfAlan.Features.StudyArea.Models;

namespace ProfAlan.Features.StudyArea.Services;

public sealed record VerifiedProblem(Quiz Quiz, List<TestCase> Cases, string ReferenceAnswer, string Hint, IReadOnlyList<string> Notes, bool ReferenceChecked);

/// <summary>
/// Never trusts the model's verdicts blindly: inputs outside Σ are dropped, duplicates removed,
/// and every expected result is recomputed from the reference answer when it parses.
/// </summary>
public static class ProblemVerifier
{
    public static VerifiedProblem Verify(GeneratedProblem generated)
    {
        var notes = new List<string>();
        var alphabet = new string(generated.Alphabet.Where(c => !char.IsWhiteSpace(c) && c != ',' && !Symbols.IsReserved(c)).Distinct().ToArray());
        if (alphabet.Length == 0) alphabet = "ab";

        var kind = Enum.TryParse<AnswerKind>(generated.AnswerKind, ignoreCase: true, out var parsed) ? parsed : AnswerKind.Dfa;

        var cases = new List<TestCase>();
        var dropped = 0;
        foreach (var c in generated.TestCases)
        {
            var input = Symbols.IsEmpty(c.Input) ? "" : c.Input.Trim();
            if (input.Any(ch => !alphabet.Contains(ch)) || cases.Any(existing => existing.Input == input))
            {
                dropped++;
                continue;
            }
            cases.Add(new TestCase { Input = input, ShouldAccept = c.Accept, IsHidden = c.Hidden });
        }
        if (dropped > 0) notes.Add($"Dropped {dropped} test case(s) that were duplicates or used symbols outside Σ.");

        var oracle = QuizGrader.ReferenceOracle(generated.ReferenceAnswer, out _);
        if (oracle is not null)
        {
            var corrected = 0;
            foreach (var c in cases)
            {
                var truth = oracle(c.Input);
                if (truth == c.ShouldAccept) continue;
                c.ShouldAccept = truth;
                corrected++;
            }
            notes.Add(corrected == 0
                ? "Every expected verdict agrees with the reference answer."
                : $"Corrected {corrected} expected verdict(s) that disagreed with the reference answer.");
        }
        else
        {
            notes.Add("The reference answer could not be parsed, so the expected verdicts are unverified. Check them before publishing.");
        }

        if (cases.Count > 0 && cases.All(c => c.IsHidden)) cases[0].IsHidden = false;

        var quiz = new Quiz
        {
            Title = generated.Title.Trim(),
            Prompt = generated.Prompt.Trim(),
            AnswerKind = kind,
            Alphabet = alphabet
        };
        return new VerifiedProblem(quiz, cases, generated.ReferenceAnswer, generated.Hint, notes, oracle is not null);
    }
}
