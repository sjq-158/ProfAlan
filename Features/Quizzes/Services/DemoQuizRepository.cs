using ProfAlan.Features.Auth.Services;
using ProfAlan.Features.Quizzes.Models;
using ProfAlan.Shared.Services;

namespace ProfAlan.Features.Quizzes.Services;

/// <summary>
/// Browser-only quiz storage for demo mode. It is shared by every demo account in this browser,
/// so an instructor account and a student account can try the full publish-and-solve loop.
/// </summary>
public sealed class DemoQuizRepository(BrowserStorage storage, AuthSession auth) : IQuizRepository
{
    private const string QuizzesKey = "profalan.demo.quizzes";
    private const string CasesKey = "profalan.demo.cases";
    private const string SubmissionsKey = "profalan.demo.submissions";

    private sealed class Store
    {
        public List<Quiz> Quizzes { get; set; } = [];
        public Dictionary<Guid, List<TestCase>> Cases { get; set; } = [];
    }

    public async Task<IReadOnlyList<Quiz>> ListAsync()
    {
        var store = await LoadAsync();
        var me = auth.Current?.UserId;
        return store.Quizzes.Where(q => q.IsPublished || q.AuthorId == me).OrderByDescending(q => q.CreatedAt).ToList();
    }

    public async Task<Quiz?> GetAsync(Guid id) => (await LoadAsync()).Quizzes.FirstOrDefault(q => q.Id == id);

    public async Task<IReadOnlyList<TestCase>> GetCasesAsync(Guid quizId, bool includeHidden)
    {
        var store = await LoadAsync();
        var quiz = store.Quizzes.FirstOrDefault(q => q.Id == quizId);
        var isAuthor = quiz?.AuthorId is not null && quiz.AuthorId == auth.Current?.UserId;
        var cases = store.Cases.GetValueOrDefault(quizId) ?? [];
        return cases.Where(c => !c.IsHidden || (includeHidden && isAuthor)).ToList();
    }

    public async Task<IReadOnlyList<TestCase>> GetGradingCasesAsync(Guid quizId) =>
        (await LoadAsync()).Cases.GetValueOrDefault(quizId) ?? [];

    public async Task<Quiz> SaveAsync(Quiz quiz, IReadOnlyList<TestCase> cases)
    {
        var store = await LoadAsync();
        quiz.VisibleCount = cases.Count(c => !c.IsHidden);
        quiz.HiddenCount = cases.Count(c => c.IsHidden);
        store.Quizzes.RemoveAll(q => q.Id == quiz.Id);
        store.Quizzes.Add(quiz);
        store.Cases[quiz.Id] = cases.ToList();
        await PersistAsync(store);
        return quiz;
    }

    public async Task DeleteAsync(Guid id)
    {
        var store = await LoadAsync();
        store.Quizzes.RemoveAll(q => q.Id == id);
        store.Cases.Remove(id);
        await PersistAsync(store);
    }

    public async Task RecordSubmissionAsync(Submission submission)
    {
        var all = await storage.GetJsonAsync<List<Submission>>(SubmissionsKey) ?? [];
        all.Insert(0, submission);
        await storage.SetJsonAsync(SubmissionsKey, all.Take(500).ToList());
    }

    public async Task<IReadOnlyList<Submission>> SubmissionsAsync(Guid quizId)
    {
        var quiz = await GetAsync(quizId);
        var me = auth.Current?.UserId;
        var all = await storage.GetJsonAsync<List<Submission>>(SubmissionsKey) ?? [];
        var isAuthor = quiz?.AuthorId is not null && quiz.AuthorId == me;
        return all.Where(s => s.QuizId == quizId && (isAuthor || s.StudentId == me)).ToList();
    }

    public async Task<IReadOnlyList<Submission>> MySubmissionsAsync()
    {
        var me = auth.Current?.UserId;
        var all = await storage.GetJsonAsync<List<Submission>>(SubmissionsKey) ?? [];
        return all.Where(s => s.StudentId == me).ToList();
    }

    private async Task<Store> LoadAsync()
    {
        var quizzes = await storage.GetJsonAsync<List<Quiz>>(QuizzesKey);
        var cases = await storage.GetJsonAsync<Dictionary<Guid, List<TestCase>>>(CasesKey);
        if (quizzes is not null && cases is not null) return new Store { Quizzes = quizzes, Cases = cases };

        var seeded = new Store();
        foreach (var (quiz, seedCases) in SeedQuizzes.Build())
        {
            seeded.Quizzes.Add(quiz);
            seeded.Cases[quiz.Id] = seedCases;
        }
        await PersistAsync(seeded);
        return seeded;
    }

    private async Task PersistAsync(Store store)
    {
        await storage.SetJsonAsync(QuizzesKey, store.Quizzes);
        await storage.SetJsonAsync(CasesKey, store.Cases);
    }
}
