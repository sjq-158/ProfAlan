using ProfAlan.Features.Quizzes.Models;

namespace ProfAlan.Features.Quizzes.Services;

/// <summary>Starter problems from the midterm review, shown in demo mode so the arena is never empty.</summary>
public static class SeedQuizzes
{
    public static IReadOnlyList<(Quiz Quiz, List<TestCase> Cases)> Build()
    {
        return
        [
            Make("Starts with a and ends with bb", "Construct a DFA for L = { w ∈ {a,b}* | w starts with a and ends with bb }.",
                AnswerKind.Dfa, "ab", "a(a ∪ b)*bb",
                ["abb", "ab", "aabb", "babb"], ["abbabb", "abba", "a", "bb", "", "aabbb"]),
            Make("Number of b's divisible by 3", "Construct a DFA for L = { w ∈ {a,b}* | the number of b's in w is divisible by 3 }.",
                AnswerKind.Dfa, "ab", "a*(ba*ba*ba*)*",
                ["", "bbb", "ab", "abab"], ["bbbbbb", "babab", "b", "abbbbab", "aaa"]),
            Make("Contains bba", "Construct an NFA for L = { w ∈ {a,b}* | w contains bba as a substring }.",
                AnswerKind.Nfa, "ab", "(a ∪ b)*bba(a ∪ b)*",
                ["bba", "abba", "bb", "aba"], ["bbba", "babba", "bab", "", "aabbaa"]),
            Make("aa or bb, then ab blocks", "Write a regular expression for strings over {a,b} that begin with aa or bb followed by any number of ab blocks.",
                AnswerKind.Regex, "ab", "(aa ∪ bb)(ab)*",
                ["aa", "bbab", "ab", "aab"], ["aaabab", "bb", "abab", "", "bba"]),
            Make("Equal a's then b's", "Construct a PDA for L = { aⁿbⁿ : n ≥ 0 }. Accept by final state with an empty stack.",
                AnswerKind.Pda, "ab", "S → aSb | e",
                ["", "ab", "aab", "ba"], ["aaabbb", "abab", "aaaabbbb", "b", "aabbb"])
        ];
    }

    private static (Quiz, List<TestCase>) Make(string title, string prompt, AnswerKind kind, string alphabet, string reference,
        string[] visible, string[] hidden)
    {
        var oracle = QuizGrader.ReferenceOracle(reference, out _)!;
        var cases = visible.Select(w => new TestCase { Input = w, ShouldAccept = oracle(w) })
            .Concat(hidden.Select(w => new TestCase { Input = w, ShouldAccept = oracle(w), IsHidden = true }))
            .ToList();

        var quiz = new Quiz
        {
            Id = DeterministicId(title),
            AuthorId = null,
            AuthorName = "ProfAlan course staff",
            Title = title,
            Prompt = prompt,
            AnswerKind = kind,
            Alphabet = alphabet,
            IsPublished = true,
            VisibleCount = visible.Length,
            HiddenCount = hidden.Length,
            CreatedAt = new DateTimeOffset(2026, 10, 15, 8, 0, 0, TimeSpan.Zero)
        };
        return (quiz, cases);
    }

    private static Guid DeterministicId(string title)
    {
        var bytes = System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(title))[..16];
        return new Guid(bytes);
    }
}
