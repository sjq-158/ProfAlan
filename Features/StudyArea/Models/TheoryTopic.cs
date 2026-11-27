namespace ProfAlan.Features.StudyArea.Models;

public enum NoteKind
{
    Paragraph,
    Definition,
    Example,
    Tip
}

public sealed record NoteBlock(NoteKind Kind, string Text, string? Formal = null, string? Title = null);

public sealed record StudyLink(string Label, string Href);

public sealed record TheoryTopic(
    string Slug,
    string Title,
    string Section,
    string Summary,
    IReadOnlyList<NoteBlock> Blocks,
    IReadOnlyList<StudyLink> Links);
