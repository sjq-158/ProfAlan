using ProfAlan.Domain.Automata;

namespace ProfAlan.Features.Quizzes.Models;

/// <summary>One graded test case. Input is null for hidden cases so the string never reaches the UI.</summary>
public sealed record CaseResult(int Number, bool IsHidden, string? Input, bool ExpectedAccept, Verdict Actual, string Detail)
{
    public bool Passed => Actual is Verdict.Accepted or Verdict.Rejected && (Actual == Verdict.Accepted) == ExpectedAccept;
}

public sealed record GradeReport(IReadOnlyList<CaseResult> Results, string? AnswerError, IReadOnlyList<string> Notes)
{
    public int Passed => Results.Count(r => r.Passed);
    public int Total => Results.Count;
    public int Percent => Total == 0 ? 0 : (int)Math.Round(100.0 * Passed / Total);
    public bool IsPerfect => AnswerError is null && Total > 0 && Passed == Total;
}
