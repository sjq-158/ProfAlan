using ProfAlan.Domain.Automata;
using ProfAlan.Features.Quizzes.Models;
using ProfAlan.Features.Quizzes.Services;
using ProfAlan.Features.StudyArea.Models;

namespace ProfAlan.Features.StudyArea.Services;

public sealed record VerifiedProblem(
    Quiz Quiz,
    List<TestCase> Cases,
    string ReferenceAnswer,
    string Hint,
    IReadOnlyList<string> Notes,
    bool ReferenceParsed,
    IReadOnlySet<string> Disagreements)
{
    /// <summary>The reference answer parses and agrees with every expected verdict.</summary>
    public bool IsVerified => ReferenceParsed && Disagreements.Count == 0;
}

/// <summary>
/// Never trusts the model blindly. Inputs outside Σ and duplicates are dropped, then every
/// expected verdict is checked against the reference answer. When the two disagree it is not
/// known which one is wrong, so nothing is overwritten: the problem is reported as unverified.
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

        var disagreements = new HashSet<string>();
        var oracle = QuizGrader.ReferenceOracle(generated.ReferenceAnswer, out _);
        if (oracle is null)
        {
            notes.Add("The reference answer could not be parsed, so the expected verdicts could not be checked.");
        }
        else
        {
            foreach (var c in cases.Where(c => oracle(c.Input) != c.ShouldAccept)) disagreements.Add(c.Input);
            notes.Add(disagreements.Count == 0
                ? "Every expected verdict agrees with the reference answer."
                : $"The reference answer disagrees with {disagreements.Count} expected verdict(s), marked below. Review them before using this problem.");
        }

        if (cases.Count > 0 && cases.All(c => c.IsHidden)) cases[0].IsHidden = false;

        var quiz = new Quiz
        {
            Title = generated.Title.Trim(),
            Prompt = generated.Prompt.Trim(),
            AnswerKind = kind,
            Alphabet = alphabet
        };
        return new VerifiedProblem(quiz, cases, generated.ReferenceAnswer, generated.Hint, notes, oracle is not null, disagreements);
    }
}
