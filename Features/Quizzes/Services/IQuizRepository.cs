using ProfAlan.Features.Quizzes.Models;

namespace ProfAlan.Features.Quizzes.Services;

public interface IQuizRepository
{
    /// <summary>Published quizzes plus every quiz the current user wrote.</summary>
    Task<IReadOnlyList<Quiz>> ListAsync();
    Task<Quiz?> GetAsync(Guid id);

    /// <summary>Cases a solver may see. The author also receives hidden cases for editing.</summary>
    Task<IReadOnlyList<TestCase>> GetCasesAsync(Guid quizId, bool includeHidden);

    /// <summary>All cases, fetched only for grading and never rendered.</summary>
    Task<IReadOnlyList<TestCase>> GetGradingCasesAsync(Guid quizId);

    Task<Quiz> SaveAsync(Quiz quiz, IReadOnlyList<TestCase> cases);
    Task DeleteAsync(Guid id);
    Task RecordSubmissionAsync(Submission submission);

    /// <summary>The author sees every submission; anyone else sees only their own.</summary>
    Task<IReadOnlyList<Submission>> SubmissionsAsync(Guid quizId);
    Task<IReadOnlyList<Submission>> MySubmissionsAsync();
}
