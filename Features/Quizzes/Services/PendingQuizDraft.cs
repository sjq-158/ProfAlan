using ProfAlan.Features.Quizzes.Models;

namespace ProfAlan.Features.Quizzes.Services;

/// <summary>Hands a generated problem from the Study Area to the quiz editor without a round trip.</summary>
public sealed class PendingQuizDraft
{
    private (Quiz Quiz, List<TestCase> Cases)? pending;

    public void Offer(Quiz quiz, List<TestCase> cases) => pending = (quiz, cases);

    public (Quiz Quiz, List<TestCase> Cases)? Take()
    {
        var taken = pending;
        pending = null;
        return taken;
    }
}
