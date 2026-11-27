namespace ProfAlan.Domain.Regular;

/// <summary>Regular expressions over Σ built from ∅, e, symbols, ∪, concatenation and *.</summary>
public abstract record RegexNode
{
    public sealed record EmptySet : RegexNode;
    public sealed record EmptyString : RegexNode;
    public sealed record Symbol(char Value) : RegexNode;
    public sealed record Union(RegexNode Left, RegexNode Right) : RegexNode;
    public sealed record Concat(RegexNode Left, RegexNode Right) : RegexNode;
    public sealed record Star(RegexNode Inner) : RegexNode;

    public IEnumerable<char> SymbolsUsed() => this switch
    {
        Symbol s => [s.Value],
        Union u => u.Left.SymbolsUsed().Concat(u.Right.SymbolsUsed()),
        Concat c => c.Left.SymbolsUsed().Concat(c.Right.SymbolsUsed()),
        Star s => s.Inner.SymbolsUsed(),
        _ => []
    };

    public sealed override string ToString() => this switch
    {
        EmptySet => "∅",
        EmptyString => "e",
        Symbol s => s.Value.ToString(),
        Union u => $"({u.Left} ∪ {u.Right})",
        Concat c => $"{c.Left}{c.Right}",
        Star s => s.Inner is Symbol ? $"{s.Inner}*" : $"({Unwrap(s.Inner)})*",
        _ => "?"
    };

    private static string Unwrap(RegexNode node)
    {
        var text = node.ToString();
        return node is Union && text.StartsWith('(') && text.EndsWith(')') ? text[1..^1] : text;
    }
}
