namespace ProfAlan.Domain.Automata;

/// <summary>Reserved symbols, written the way Lewis &amp; Papadimitriou write them.</summary>
public static class Symbols
{
    public const string Empty = "e";
    public const char Blank = '⊔';
    public const char LeftEnd = '▷';
    public const string MoveLeft = "←";
    public const string MoveRight = "→";

    public static bool IsEmpty(string? s) => string.IsNullOrEmpty(s) || s is Empty or "ε";

    /// <summary>Normalizes user input where "e", "ε" and "" all mean the empty string.</summary>
    public static string Normalize(string? s) => IsEmpty(s) ? "" : s!.Trim();

    public static string Show(string? s) => IsEmpty(s) ? Empty : s!;

    public static bool IsMove(string? action) => action is MoveLeft or MoveRight;

    /// <summary>Accepts the arrow glyphs plus the L/R shorthand students tend to type.</summary>
    public static string NormalizeAction(string? action) => action?.Trim() switch
    {
        "L" or "l" or "<" or "<-" or MoveLeft => MoveLeft,
        "R" or "r" or ">" or "->" or MoveRight => MoveRight,
        "_" or "#" or "B" => Blank.ToString(),
        var other => other ?? ""
    };

    public static char NormalizeTapeSymbol(char c) => c is '_' or '#' ? Blank : c;

    public static bool IsReserved(char c) => c is Blank or LeftEnd or 'e' or 'ε';
}
