using ProfAlan.Domain.Automata;

namespace ProfAlan.Features.Quizzes.Models;

/// <summary>What the solver must submit: a machine, a regular expression or a grammar.</summary>
public enum AnswerKind
{
    Dfa,
    Nfa,
    Pda,
    Turing,
    Regex,
    Grammar
}

public static class AnswerKindNames
{
    public static string Label(this AnswerKind kind) => kind switch
    {
        AnswerKind.Dfa => "DFA",
        AnswerKind.Nfa => "NFA",
        AnswerKind.Pda => "PDA",
        AnswerKind.Turing => "Turing machine",
        AnswerKind.Regex => "Regular expression",
        AnswerKind.Grammar => "Context-free grammar",
        _ => kind.ToString()
    };

    public static MachineKind? Machine(this AnswerKind kind) => kind switch
    {
        AnswerKind.Dfa => MachineKind.Dfa,
        AnswerKind.Nfa => MachineKind.Nfa,
        AnswerKind.Pda => MachineKind.Pda,
        AnswerKind.Turing => MachineKind.Turing,
        _ => null
    };
}

public sealed class Quiz
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string? AuthorId { get; set; }
    public string AuthorName { get; set; } = "";
    public string Title { get; set; } = "";
    public string Prompt { get; set; } = "";
    public AnswerKind AnswerKind { get; set; } = AnswerKind.Dfa;
    public string Alphabet { get; set; } = "ab";
    public bool IsPublished { get; set; }
    public int VisibleCount { get; set; }
    public int HiddenCount { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public int TotalCases => VisibleCount + HiddenCount;
}

public sealed class TestCase
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Input { get; set; } = "";
    public bool ShouldAccept { get; set; } = true;
    public bool IsHidden { get; set; }
}

public sealed record Submission(
    Guid Id,
    Guid QuizId,
    string StudentId,
    string StudentName,
    string Answer,
    int Passed,
    int Total,
    DateTimeOffset CreatedAt)
{
    public bool IsPerfect => Total > 0 && Passed == Total;
}
