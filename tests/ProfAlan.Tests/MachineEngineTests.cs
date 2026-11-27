namespace ProfAlan.Tests;

public class DfaEngineTests
{
    [Theory]
    [InlineData("abb", true)]
    [InlineData("abab", false)]
    [InlineData("aabbabb", true)]
    [InlineData("bbb", false)]
    [InlineData("", false)]
    public void Decides_starts_with_a_and_ends_with_bb(string input, bool expected)
    {
        var result = new DfaEngine(Machines.StartsWithAEndsWithBb()).Run(input);
        Assert.Equal(expected, result.Accepted);
        Assert.Equal(input.Length + 1, result.Path.Count);
    }

    [Fact]
    public void Flags_partial_transition_function_and_trap_state_fixes_it()
    {
        var m = Machines.Build(MachineKind.Dfa, "ab", "q0", ["q1"], ("q0", "a", "q1"));
        var issues = new DfaEngine(m).Validate();
        Assert.Contains(issues, i => i.Fix == IssueFix.AddTrapState);

        m.AddTrapState();
        Assert.DoesNotContain(new DfaEngine(m).Validate(), i => i.Severity == IssueSeverity.Error);
    }

    [Fact]
    public void Rejects_symbols_outside_sigma()
    {
        var result = new DfaEngine(Machines.StartsWithAEndsWithBb()).Run("abc");
        Assert.Equal(Verdict.InvalidInput, result.Verdict);
    }
}

public class NfaEngineTests
{
    [Theory]
    [InlineData("bbba", true)]
    [InlineData("abba", true)]
    [InlineData("bab", false)]
    [InlineData("", false)]
    public void Finds_substring_bba_with_string_labels(string input, bool expected)
    {
        var m = Machines.Build(MachineKind.Nfa, "ab", "q0", ["q1"],
            ("q0", "a", "q0"), ("q0", "b", "q0"), ("q0", "bba", "q1"), ("q1", "a", "q1"), ("q1", "b", "q1"));
        Assert.Equal(expected, new NfaEngine(m).Run(input).Accepted);
    }

    [Fact]
    public void Follows_empty_transitions()
    {
        var m = Machines.Build(MachineKind.Nfa, "a", "q0", ["q2"], ("q0", "", "q1"), ("q1", "a", "q2"));
        Assert.True(new NfaEngine(m).Run("a").Accepted);
    }
}

public class PdaEngineTests
{
    [Theory]
    [InlineData("", true)]
    [InlineData("ab", true)]
    [InlineData("aaabbb", true)]
    [InlineData("aab", false)]
    [InlineData("abab", false)]
    public void Accepts_by_final_state_and_empty_stack(string input, bool expected)
    {
        var result = new PdaEngine(Machines.AnBn()).Run(input);
        Assert.Equal(expected, result.Accepted);
        if (expected) Assert.Equal("", result.Path[^1].Stack);
    }
}

public class TuringEngineTests
{
    [Theory]
    [InlineData("", true)]
    [InlineData("aa", true)]
    [InlineData("aaa", false)]
    public void Decides_with_y_and_n(string input, bool expected)
    {
        var result = new TuringEngine(Machines.EvenLengthDecider()).Run(input);
        Assert.Equal(expected, result.Accepted);
    }

    [Fact]
    public void Starts_on_the_blank_after_the_left_end()
    {
        var first = new TuringEngine(Machines.EvenLengthDecider()).Run("aa").Path[0];
        Assert.Equal($"{Symbols.LeftEnd}{Symbols.Blank}aa", first.Tape);
        Assert.Equal(1, first.Head);
    }

    [Fact]
    public void Requires_moving_right_on_left_end()
    {
        var m = Machines.EvenLengthDecider();
        m.Transitions.First(t => t.Read == Symbols.LeftEnd.ToString()).Action = Symbols.MoveLeft;
        Assert.Contains(new TuringEngine(m).Validate(), i => i.Severity == IssueSeverity.Error && i.Message.Contains('▷'));
    }

    [Fact]
    public void Reports_non_halting_runs()
    {
        var m = Machines.EvenLengthDecider();
        m.Transitions.First(t => t.From == "even" && t.Read == Symbols.Blank.ToString()).Action = Symbols.MoveLeft;
        m.Transitions.First(t => t.From == "even" && t.Read == Symbols.Blank.ToString()).To = "odd";
        m.Transitions.First(t => t.From == "odd" && t.Read == Symbols.Blank.ToString()).Action = Symbols.MoveRight;
        m.Transitions.First(t => t.From == "odd" && t.Read == Symbols.Blank.ToString()).To = "even";
        Assert.Equal(Verdict.DidNotHalt, new TuringEngine(m).Run("").Verdict);
    }
}
