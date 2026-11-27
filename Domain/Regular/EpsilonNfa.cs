using ProfAlan.Domain.Automata;

namespace ProfAlan.Domain.Regular;

/// <summary>
/// A compact NFA with single-symbol and e edges. It is the common currency for regular
/// languages here: regexes compile to it, DFA/NFA diagrams convert to it, and the
/// equivalence checker compares two of them.
/// </summary>
public sealed class EpsilonNfa
{
    private readonly List<List<(char? Symbol, int To)>> edges = [];
    private readonly HashSet<int> accepting = [];

    public int Start { get; private set; }
    public int StateCount => edges.Count;

    private int NewState()
    {
        edges.Add([]);
        return edges.Count - 1;
    }

    private void Connect(int from, char? symbol, int to) => edges[from].Add((symbol, to));

    /// <summary>Thompson's construction.</summary>
    public static EpsilonNfa FromRegex(RegexNode regex)
    {
        var nfa = new EpsilonNfa();
        var (start, end) = nfa.Build(regex);
        nfa.Start = start;
        nfa.accepting.Add(end);
        return nfa;
    }

    private (int Start, int End) Build(RegexNode node)
    {
        int s, f;
        switch (node)
        {
            case RegexNode.EmptySet:
                return (NewState(), NewState());
            case RegexNode.EmptyString:
                s = NewState();
                f = NewState();
                Connect(s, null, f);
                return (s, f);
            case RegexNode.Symbol sym:
                s = NewState();
                f = NewState();
                Connect(s, sym.Value, f);
                return (s, f);
            case RegexNode.Concat c:
                var left = Build(c.Left);
                var right = Build(c.Right);
                Connect(left.End, null, right.Start);
                return (left.Start, right.End);
            case RegexNode.Union u:
                s = NewState();
                var a = Build(u.Left);
                var b = Build(u.Right);
                f = NewState();
                Connect(s, null, a.Start);
                Connect(s, null, b.Start);
                Connect(a.End, null, f);
                Connect(b.End, null, f);
                return (s, f);
            case RegexNode.Star st:
                s = NewState();
                var inner = Build(st.Inner);
                f = NewState();
                Connect(s, null, inner.Start);
                Connect(s, null, f);
                Connect(inner.End, null, inner.Start);
                Connect(inner.End, null, f);
                return (s, f);
            default:
                throw new NotSupportedException(node.GetType().Name);
        }
    }

    /// <summary>Converts a DFA or NFA diagram. Multi-symbol NFA labels become chains of fresh states.</summary>
    public static EpsilonNfa FromAutomaton(Automaton machine)
    {
        if (machine.Kind is not (MachineKind.Dfa or MachineKind.Nfa))
        {
            throw new ArgumentException("Only finite automata describe regular languages directly.", nameof(machine));
        }

        var nfa = new EpsilonNfa();
        var index = machine.States.ToDictionary(s => s.Name, _ => nfa.NewState());
        nfa.Start = machine.InitialState is { } s ? index[s.Name] : nfa.NewState();
        foreach (var f in machine.States.Where(q => q.IsFinal)) nfa.accepting.Add(index[f.Name]);

        foreach (var rule in machine.Transitions)
        {
            if (!index.TryGetValue(rule.From, out var from) || !index.TryGetValue(rule.To, out var to)) continue;
            if (rule.Read.Length == 0)
            {
                nfa.Connect(from, null, to);
                continue;
            }

            var current = from;
            for (var i = 0; i < rule.Read.Length; i++)
            {
                var next = i == rule.Read.Length - 1 ? to : nfa.NewState();
                nfa.Connect(current, rule.Read[i], next);
                current = next;
            }
        }
        return nfa;
    }

    public SortedSet<int> Closure(IEnumerable<int> states)
    {
        var closure = new SortedSet<int>(states);
        var stack = new Stack<int>(closure);
        while (stack.Count > 0)
        {
            foreach (var (symbol, to) in edges[stack.Pop()])
            {
                if (symbol is null && closure.Add(to)) stack.Push(to);
            }
        }
        return closure;
    }

    public SortedSet<int> Step(IEnumerable<int> states, char symbol) =>
        Closure(states.SelectMany(q => edges[q].Where(e => e.Symbol == symbol).Select(e => e.To)));

    public bool IsAccepting(IEnumerable<int> states) => states.Any(accepting.Contains);

    public SortedSet<int> InitialSet() => Closure([Start]);

    public bool Accepts(string input)
    {
        var current = InitialSet();
        foreach (var c in input)
        {
            current = Step(current, c);
            if (current.Count == 0) return false;
        }
        return IsAccepting(current);
    }

    /// <summary>Members of the language in shortlex order, which makes good example strings.</summary>
    public IReadOnlyList<string> SampleMembers(IReadOnlyCollection<char> alphabet, int count, int maxLength = 8)
    {
        var found = new List<string>();
        var frontier = new List<(string Word, SortedSet<int> States)> { ("", InitialSet()) };
        var ordered = alphabet.Distinct().OrderBy(c => c).ToList();

        for (var length = 0; length <= maxLength && found.Count < count && frontier.Count > 0; length++)
        {
            foreach (var (word, states) in frontier)
            {
                if (IsAccepting(states)) found.Add(word);
                if (found.Count >= count) break;
            }

            var next = new List<(string, SortedSet<int>)>();
            foreach (var (word, states) in frontier)
            {
                foreach (var c in ordered)
                {
                    var moved = Step(states, c);
                    if (moved.Count > 0) next.Add((word + c, moved));
                }
                if (next.Count > 4000) break;
            }
            frontier = next;
        }
        return found;
    }
}
