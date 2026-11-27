namespace ProfAlan.Features.StudyArea.Models;

public sealed record GeneratedCase(string Input, bool Accept, bool Hidden);

public sealed record GeneratedProblem(
    string Title,
    string Prompt,
    string AnswerKind,
    string Alphabet,
    string ReferenceAnswer,
    string Hint,
    IReadOnlyList<GeneratedCase> TestCases);

public sealed record LanguageExplanation(
    string Summary,
    string Explanation,
    IReadOnlyList<string> Members,
    IReadOnlyList<string> NonMembers);

public sealed record GenerationRequest(string Topic, string AnswerKind, string Difficulty, string Alphabet);
