namespace ProfAlan.Domain.Regular;

public sealed class RegexSyntaxException(string message, int position) : Exception(message)
{
    public int Position { get; } = position;
}

/// <summary>
/// Parses the textbook notation. Union may be written ∪, + or |; e or ε is the empty string;
/// ∅ is the empty language. Precedence, from tightest: *, concatenation, union.
/// </summary>
public sealed class RegexParser
{
    private readonly string text;
    private int pos;

    private RegexParser(string text) => this.text = text;

    public static RegexNode Parse(string text)
    {
        var parser = new RegexParser(text);
        parser.SkipSpace();
        if (parser.AtEnd) throw new RegexSyntaxException("The expression is empty. Write e for the empty string.", 0);

        var node = parser.ParseUnion();
        parser.SkipSpace();
        if (!parser.AtEnd)
        {
            throw new RegexSyntaxException($"Unexpected '{parser.text[parser.pos]}' at position {parser.pos + 1}.", parser.pos);
        }
        return node;
    }

    public static bool TryParse(string text, out RegexNode? node, out string? error)
    {
        try
        {
            node = Parse(text);
            error = null;
            return true;
        }
        catch (RegexSyntaxException ex)
        {
            node = null;
            error = ex.Message;
            return false;
        }
    }

    private bool AtEnd => pos >= text.Length;

    private char Peek => text[pos];

    private static bool IsUnionOperator(char c) => c is '∪' or '+' or '|';

    private void SkipSpace()
    {
        while (!AtEnd && char.IsWhiteSpace(Peek)) pos++;
    }

    private RegexNode ParseUnion()
    {
        var left = ParseConcat();
        SkipSpace();
        while (!AtEnd && IsUnionOperator(Peek))
        {
            pos++;
            var right = ParseConcat();
            left = new RegexNode.Union(left, right);
            SkipSpace();
        }
        return left;
    }

    private RegexNode ParseConcat()
    {
        SkipSpace();
        if (AtEnd || Peek == ')' || IsUnionOperator(Peek))
        {
            throw new RegexSyntaxException($"Expected an expression at position {pos + 1}.", pos);
        }

        var node = ParseStar();
        SkipSpace();
        while (!AtEnd && Peek != ')' && !IsUnionOperator(Peek))
        {
            node = new RegexNode.Concat(node, ParseStar());
            SkipSpace();
        }
        return node;
    }

    private RegexNode ParseStar()
    {
        var node = ParseAtom();
        SkipSpace();
        while (!AtEnd && Peek == '*')
        {
            pos++;
            node = node is RegexNode.Star ? node : new RegexNode.Star(node);
            SkipSpace();
        }
        return node;
    }

    private RegexNode ParseAtom()
    {
        SkipSpace();
        var c = Peek;
        var start = pos;
        pos++;

        switch (c)
        {
            case '(':
                var inner = ParseUnion();
                SkipSpace();
                if (AtEnd || Peek != ')') throw new RegexSyntaxException($"Missing ')' for the '(' at position {start + 1}.", start);
                pos++;
                return inner;
            case 'e' or 'ε':
                return new RegexNode.EmptyString();
            case '∅' or 'Ø' or 'ø':
                return new RegexNode.EmptySet();
            case '*':
                throw new RegexSyntaxException($"'*' at position {start + 1} has nothing to repeat.", start);
            default:
                if (char.IsLetterOrDigit(c)) return new RegexNode.Symbol(c);
                throw new RegexSyntaxException($"'{c}' at position {start + 1} is not a symbol or operator.", start);
        }
    }
}
