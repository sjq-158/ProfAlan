using System.Text.Json.Serialization;
using ProfAlan.Features.Auth.Services;
using ProfAlan.Features.Quizzes.Models;
using ProfAlan.Shared.Supabase;

namespace ProfAlan.Features.Quizzes.Services;

/// <summary>
/// Row level security does the enforcement: hidden cases are readable only by the author,
/// and solvers receive them solely through the grading_cases function.
/// </summary>
public sealed class SupabaseQuizRepository(SupabaseRest db, AuthSession auth) : IQuizRepository
{
    private const string QuizColumns = "id,author_id,author_name,title,prompt,answer_kind,alphabet,is_published,visible_count,hidden_count,created_at";

    private sealed record QuizRow(
        [property: JsonPropertyName("id")] Guid Id,
        [property: JsonPropertyName("author_id")] string? AuthorId,
        [property: JsonPropertyName("author_name")] string AuthorName,
        [property: JsonPropertyName("title")] string Title,
        [property: JsonPropertyName("prompt")] string Prompt,
        [property: JsonPropertyName("answer_kind")] string AnswerKind,
        [property: JsonPropertyName("alphabet")] string Alphabet,
        [property: JsonPropertyName("is_published")] bool IsPublished,
        [property: JsonPropertyName("visible_count")] int VisibleCount,
        [property: JsonPropertyName("hidden_count")] int HiddenCount,
        [property: JsonPropertyName("created_at")] DateTimeOffset CreatedAt);

    private sealed record QuizWrite(
        [property: JsonPropertyName("id")] Guid Id,
        [property: JsonPropertyName("author_name")] string AuthorName,
        [property: JsonPropertyName("title")] string Title,
        [property: JsonPropertyName("prompt")] string Prompt,
        [property: JsonPropertyName("answer_kind")] string AnswerKind,
        [property: JsonPropertyName("alphabet")] string Alphabet,
        [property: JsonPropertyName("is_published")] bool IsPublished,
        [property: JsonPropertyName("visible_count")] int VisibleCount,
        [property: JsonPropertyName("hidden_count")] int HiddenCount);

    private sealed record CaseRow(
        [property: JsonPropertyName("id")] Guid Id,
        [property: JsonPropertyName("quiz_id")] Guid QuizId,
        [property: JsonPropertyName("position")] int Position,
        [property: JsonPropertyName("input")] string Input,
        [property: JsonPropertyName("should_accept")] bool ShouldAccept,
        [property: JsonPropertyName("is_hidden")] bool IsHidden);

    private sealed record SubmissionRow(
        [property: JsonPropertyName("id")] Guid Id,
        [property: JsonPropertyName("quiz_id")] Guid QuizId,
        [property: JsonPropertyName("student_id")] string? StudentId,
        [property: JsonPropertyName("student_name")] string StudentName,
        [property: JsonPropertyName("answer")] string Answer,
        [property: JsonPropertyName("passed")] int Passed,
        [property: JsonPropertyName("total")] int Total,
        [property: JsonPropertyName("created_at")] DateTimeOffset CreatedAt);

    public async Task<IReadOnlyList<Quiz>> ListAsync()
    {
        var me = auth.Current?.UserId;
        var filter = me is null ? "is_published=eq.true" : $"or=(is_published.eq.true,author_id.eq.{me})";
        var rows = await db.SelectAsync<QuizRow>($"quizzes?select={QuizColumns}&{filter}&order=created_at.desc");
        return rows.Select(ToModel).ToList();
    }

    public async Task<Quiz?> GetAsync(Guid id)
    {
        var rows = await db.SelectAsync<QuizRow>($"quizzes?select={QuizColumns}&id=eq.{id}");
        return rows.Count == 0 ? null : ToModel(rows[0]);
    }

    public async Task<IReadOnlyList<TestCase>> GetCasesAsync(Guid quizId, bool includeHidden)
    {
        var hidden = includeHidden ? "" : "&is_hidden=eq.false";
        var rows = await db.SelectAsync<CaseRow>($"quiz_test_cases?select=*&quiz_id=eq.{quizId}{hidden}&order=position");
        return rows.Select(ToCase).ToList();
    }

    public async Task<IReadOnlyList<TestCase>> GetGradingCasesAsync(Guid quizId)
    {
        var rows = await db.RpcAsync<CaseRow>("grading_cases", new { p_quiz_id = quizId });
        return rows.OrderBy(r => r.Position).Select(ToCase).ToList();
    }

    public async Task<Quiz> SaveAsync(Quiz quiz, IReadOnlyList<TestCase> cases)
    {
        var row = await db.UpsertAsync<QuizRow>($"quizzes?select={QuizColumns}", new QuizWrite(
            quiz.Id, quiz.AuthorName, quiz.Title, quiz.Prompt, quiz.AnswerKind.ToString().ToLowerInvariant(), quiz.Alphabet,
            quiz.IsPublished, cases.Count(c => !c.IsHidden), cases.Count(c => c.IsHidden)));

        await db.DeleteAsync($"quiz_test_cases?quiz_id=eq.{quiz.Id}");
        if (cases.Count > 0)
        {
            await db.InsertManyAsync("quiz_test_cases",
                cases.Select((c, i) => new CaseRow(c.Id, quiz.Id, i, c.Input, c.ShouldAccept, c.IsHidden)).ToList());
        }
        return ToModel(row);
    }

    public Task DeleteAsync(Guid id) => db.DeleteAsync($"quizzes?id=eq.{id}");

    public Task RecordSubmissionAsync(Submission s) =>
        db.InsertManyAsync("submissions", new[]
        {
            new { id = s.Id, quiz_id = s.QuizId, student_name = s.StudentName, answer = s.Answer, passed = s.Passed, total = s.Total }
        });

    public async Task<IReadOnlyList<Submission>> SubmissionsAsync(Guid quizId)
    {
        var rows = await db.SelectAsync<SubmissionRow>($"submissions?select=*&quiz_id=eq.{quizId}&order=created_at.desc");
        return rows.Select(ToSubmission).ToList();
    }

    public async Task<IReadOnlyList<Submission>> MySubmissionsAsync()
    {
        if (auth.Current is not { } me) return [];
        var rows = await db.SelectAsync<SubmissionRow>($"submissions?select=*&student_id=eq.{me.UserId}&order=created_at.desc&limit=100");
        return rows.Select(ToSubmission).ToList();
    }

    private static Quiz ToModel(QuizRow r) => new()
    {
        Id = r.Id,
        AuthorId = r.AuthorId,
        AuthorName = r.AuthorName,
        Title = r.Title,
        Prompt = r.Prompt,
        AnswerKind = Enum.TryParse<AnswerKind>(r.AnswerKind, true, out var kind) ? kind : AnswerKind.Dfa,
        Alphabet = r.Alphabet,
        IsPublished = r.IsPublished,
        VisibleCount = r.VisibleCount,
        HiddenCount = r.HiddenCount,
        CreatedAt = r.CreatedAt
    };

    private static TestCase ToCase(CaseRow r) => new() { Id = r.Id, Input = r.Input, ShouldAccept = r.ShouldAccept, IsHidden = r.IsHidden };

    private static Submission ToSubmission(SubmissionRow r) =>
        new(r.Id, r.QuizId, r.StudentId ?? "", r.StudentName, r.Answer, r.Passed, r.Total, r.CreatedAt);
}
