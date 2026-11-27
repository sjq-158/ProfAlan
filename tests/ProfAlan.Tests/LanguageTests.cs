using ProfAlan.Domain.Grammars;
using ProfAlan.Domain.Regular;

namespace ProfAlan.Tests;

public class RegexTests
{
    [Theory]
    [InlineData("(aa+bb)(ab)*", "aaab", true)]
    [InlineData("(aa+bb)(ab)*", "abab", false)]
    [InlineData("a(a ∪ b)*bb", "abb", true)]
    [InlineData("a(a|b)*bb", "ab", false)]
    [InlineData("e", "", true)]
    [InlineData("∅", "", false)]
    [InlineData("(ab)*", "", true)]
    public void Membership(string regex, string word, bool expected)
    {
        var nfa = EpsilonNfa.FromRegex(RegexParser.Parse(regex));
        Assert.Equal(expected, nfa.Accepts(word));
    }

    [Theory]
    [InlineData("a(")]
    [InlineData("*a")]
    [InlineData("a++b")]
    [InlineData("")]
    public void Reports_syntax_errors(string regex)
    {
        Assert.False(RegexParser.TryParse(regex, out _, out var error));
        Assert.False(string.IsNullOrEmpty(error));
    }

    [Fact]
    public void Equivalent_expressions_have_no_counterexample()
    {
        var a = EpsilonNfa.FromRegex(RegexParser.Parse("(a+b)*"));
        var b = EpsilonNfa.FromRegex(RegexParser.Parse("(a*b*)*"));
        Assert.Null(LanguageComparer.FindDifference(a, b, "ab"));
    }

    [Fact]
    public void Finds_shortest_counterexample()
    {
        var a = EpsilonNfa.FromRegex(RegexParser.Parse("a*"));
        var b = EpsilonNfa.FromRegex(RegexParser.Parse("aa*"));
        var diff = LanguageComparer.FindDifference(a, b, "a");
        Assert.NotNull(diff);
        Assert.Equal("", diff!.Word);
        Assert.True(diff.InFirst);
    }

    [Fact]
    public void Dfa_matches_its_regex()
    {
        var dfa = EpsilonNfa.FromAutomaton(Machines.StartsWithAEndsWithBb());
        var regex = EpsilonNfa.FromRegex(RegexParser.Parse("a(a+b)*bb"));
        Assert.Null(LanguageComparer.FindDifference(dfa, regex, "ab"));
    }
}

public class GrammarTests
{
    private static readonly ContextFreeGrammar Balanced = ContextFreeGrammar.Parse("S -> SS | (S) | e");
    private static readonly ContextFreeGrammar AnBn = ContextFreeGrammar.Parse("S → aSb | e");

    [Theory]
    [InlineData("", true)]
    [InlineData("(())()", true)]
    [InlineData("(()", false)]
    [InlineData(")(", false)]
    public void Earley_decides_balanced_parentheses(string word, bool expected)
    {
        Assert.Equal(expected, EarleyRecognizer.Accepts(Balanced, word));
    }

    [Fact]
    public void Traces_leftmost_derivation_and_tree()
    {
        var derivation = DerivationTracer.FindLeftmost(AnBn, "aabb");
        Assert.NotNull(derivation);
        Assert.Equal(["S", "aSb", "aaSbb", "aabb"], derivation!.Steps.Select(s => s.Form));
        Assert.Equal('S', derivation.Tree.Symbol);
        Assert.Equal(3, derivation.Tree.Children.Count);
    }

    [Fact]
    public void No_derivation_for_non_members()
    {
        Assert.Null(DerivationTracer.FindLeftmost(AnBn, "aab"));
    }

    [Fact]
    public void Converted_pda_accepts_the_same_language()
    {
        var pda = new PdaEngine(GrammarToPda.Build(AnBn));
        Assert.True(pda.Run("aaabbb").Accepted);
        Assert.False(pda.Run("abb").Accepted);
    }

    [Fact]
    public void Rejects_malformed_rules()
    {
        Assert.Throws<GrammarSyntaxException>(() => ContextFreeGrammar.Parse("aS -> b"));
    }
}

public class RegexPrintingTests
{
    [Theory]
    [InlineData("a(a+b)*bb", "a(a ∪ b)*bb")]
    [InlineData("(ab)*", "(ab)*")]
    public void Prints_in_textbook_notation(string input, string expected)
    {
        Assert.Equal(expected, RegexParser.Parse(input).ToString());
    }
}
