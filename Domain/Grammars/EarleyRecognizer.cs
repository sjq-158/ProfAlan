namespace ProfAlan.Domain.Grammars;

/// <summary>
/// Decides w ∈ L(G) for any context-free grammar, including ones with e-rules and left recursion.
/// Uses the Aycock–Horspool fix so nullable variables are skipped during prediction.
/// </summary>
public static class EarleyRecognizer
{
    private readonly record struct Item(int Rule, int Dot, int Origin);

    public static bool Accepts(ContextFreeGrammar grammar, string input)
    {
        var rules = grammar.Rules;
        var chart = new List<HashSet<Item>>();
        var agenda = new List<List<Item>>();
        for (var i = 0; i <= input.Length; i++)
        {
            chart.Add([]);
            agenda.Add([]);
        }

        void Add(int column, Item item)
        {
            if (chart[column].Add(item)) agenda[column].Add(item);
        }

        for (var r = 0; r < rules.Count; r++)
        {
            if (rules[r].Variable == grammar.Start) Add(0, new Item(r, 0, 0));
        }

        for (var i = 0; i <= input.Length; i++)
        {
            for (var k = 0; k < agenda[i].Count; k++)
            {
                var item = agenda[i][k];
                var body = rules[item.Rule].Body;

                if (item.Dot == body.Length)
                {
                    var finished = rules[item.Rule].Variable;
                    foreach (var waiting in chart[item.Origin].ToList())
                    {
                        var waitingBody = rules[waiting.Rule].Body;
                        if (waiting.Dot < waitingBody.Length && waitingBody[waiting.Dot] == finished)
                        {
                            Add(i, waiting with { Dot = waiting.Dot + 1 });
                        }
                    }
                    continue;
                }

                var next = body[item.Dot];
                if (ContextFreeGrammar.IsVariable(next))
                {
                    for (var r = 0; r < rules.Count; r++)
                    {
                        if (rules[r].Variable == next) Add(i, new Item(r, 0, i));
                    }
                    if (grammar.IsNullable(next)) Add(i, item with { Dot = item.Dot + 1 });
                }
                else if (i < input.Length && input[i] == next)
                {
                    Add(i + 1, item with { Dot = item.Dot + 1 });
                }
            }
        }

        return chart[input.Length].Any(item =>
            item.Origin == 0 &&
            rules[item.Rule].Variable == grammar.Start &&
            item.Dot == rules[item.Rule].Body.Length);
    }
}
