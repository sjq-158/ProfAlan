namespace ProfAlan.Domain.Regular;

/// <summary>A shortest string in exactly one of the two languages.</summary>
public sealed record Counterexample(string Word, bool InFirst, bool InSecond);

public static class LanguageComparer
{
    /// <summary>
    /// Runs the subset construction on both machines in lockstep and searches breadth-first
    /// for a pair of subsets that disagree on acceptance. Returns null when L(A) = L(B).
    /// </summary>
    public static Counterexample? FindDifference(EpsilonNfa first, EpsilonNfa second, IEnumerable<char> alphabet)
    {
        var symbols = alphabet.Distinct().OrderBy(c => c).ToList();
        var start = (first.InitialSet(), second.InitialSet());
        var seen = new HashSet<string> { Key(start.Item1, start.Item2) };
        var queue = new Queue<(SortedSet<int> A, SortedSet<int> B, string Word)>();
        queue.Enqueue((start.Item1, start.Item2, ""));

        while (queue.Count > 0)
        {
            var (a, b, word) = queue.Dequeue();
            var inFirst = first.IsAccepting(a);
            var inSecond = second.IsAccepting(b);
            if (inFirst != inSecond) return new Counterexample(word, inFirst, inSecond);

            foreach (var c in symbols)
            {
                var nextA = first.Step(a, c);
                var nextB = second.Step(b, c);
                if (seen.Add(Key(nextA, nextB))) queue.Enqueue((nextA, nextB, word + c));
            }
        }
        return null;
    }

    private static string Key(SortedSet<int> a, SortedSet<int> b) => $"{string.Join(',', a)}|{string.Join(',', b)}";
}
