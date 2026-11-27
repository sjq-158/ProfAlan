using System.Text.Json;

namespace ProfAlan.Domain.Automata;

/// <summary>
/// The editable definition of a state machine. Engines read it; the builders mutate it.
/// </summary>
public sealed class Automaton
{
    public MachineKind Kind { get; set; } = MachineKind.Dfa;
    public string Name { get; set; } = "Untitled machine";
    public string Description { get; set; } = "";

    /// <summary>Σ. For a TM the blank ⊔ and left end ▷ are always implied.</summary>
    public List<char> Alphabet { get; set; } = ['a', 'b'];

    /// <summary>Γ, used by PDAs only.</summary>
    public List<char> StackAlphabet { get; set; } = [];

    public List<StateNode> States { get; set; } = [];
    public List<TransitionRule> Transitions { get; set; } = [];

    public StateNode? InitialState => States.FirstOrDefault(s => s.IsInitial);

    public StateNode? FindState(string name) => States.FirstOrDefault(s => s.Name == name);

    /// <summary>Σ as a TM sees it: input symbols plus ⊔ and ▷, plus anything the rules write.</summary>
    public IReadOnlyList<char> TapeAlphabet()
    {
        var symbols = new List<char> { Symbols.LeftEnd, Symbols.Blank };
        symbols.AddRange(Alphabet);
        foreach (var rule in Transitions)
        {
            if (rule.Read.Length == 1) symbols.Add(rule.Read[0]);
            if (rule.Action.Length == 1 && !Symbols.IsMove(rule.Action)) symbols.Add(rule.Action[0]);
        }
        return symbols.Distinct().ToList();
    }

    public StateNode AddState(double x, double y)
    {
        const string prefix = "q";
        var n = 0;
        while (States.Any(s => s.Name == $"{prefix}{n}")) n++;

        var state = new StateNode { Name = $"{prefix}{n}", X = x, Y = y, IsInitial = States.Count == 0 };
        States.Add(state);
        return state;
    }

    public bool RenameState(StateNode state, string newName)
    {
        newName = newName.Trim();
        if (newName.Length == 0 || States.Any(s => s != state && s.Name == newName)) return false;

        foreach (var rule in Transitions)
        {
            if (rule.From == state.Name) rule.From = newName;
            if (rule.To == state.Name) rule.To = newName;
        }
        state.Name = newName;
        return true;
    }

    public void RemoveState(StateNode state)
    {
        States.Remove(state);
        Transitions.RemoveAll(t => t.From == state.Name || t.To == state.Name);
        if (state.IsInitial && States.Count > 0 && InitialState is null)
        {
            States[0].IsInitial = true;
        }
    }

    public void MakeInitial(StateNode state)
    {
        foreach (var s in States) s.IsInitial = s == state;
    }

    /// <summary>
    /// Completes δ by sending every missing (q, σ) to a fresh trap state, which is the
    /// standard way to turn a partial diagram into a DFA in the strict sense.
    /// </summary>
    public StateNode? AddTrapState()
    {
        var missing = States
            .SelectMany(q => Alphabet.Select(a => (q, a)))
            .Where(p => !Transitions.Any(t => t.From == p.q.Name && t.Read == p.a.ToString()))
            .ToList();
        if (missing.Count == 0) return null;

        var maxX = States.Count == 0 ? 120 : States.Max(s => s.X);
        var maxY = States.Count == 0 ? 120 : States.Max(s => s.Y);
        var trap = AddState(Math.Min(maxX, 760), Math.Min(maxY + 140, 470));
        RenameState(trap, UniqueName("trap"));

        foreach (var (q, a) in missing)
        {
            Transitions.Add(new TransitionRule { From = q.Name, To = trap.Name, Read = a.ToString() });
        }
        foreach (var a in Alphabet)
        {
            Transitions.Add(new TransitionRule { From = trap.Name, To = trap.Name, Read = a.ToString() });
        }
        return trap;
    }

    private string UniqueName(string stem)
    {
        if (States.All(s => s.Name != stem)) return stem;
        var n = 1;
        while (States.Any(s => s.Name == $"{stem}{n}")) n++;
        return $"{stem}{n}";
    }

    /// <summary>Lays states out left to right in breadth-first order from s, the way they are drawn on paper.</summary>
    public void ArrangeLeftToRight(double width = 900, double height = 520)
    {
        if (States.Count == 0) return;

        var start = InitialState ?? States[0];
        var depth = new Dictionary<string, int> { [start.Name] = 0 };
        var queue = new Queue<string>([start.Name]);
        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            foreach (var next in Transitions.Where(t => t.From == current).Select(t => t.To))
            {
                if (depth.ContainsKey(next)) continue;
                depth[next] = depth[current] + 1;
                queue.Enqueue(next);
            }
        }
        var unreachableDepth = depth.Count == 0 ? 0 : depth.Values.Max() + 1;
        foreach (var s in States.Where(s => !depth.ContainsKey(s.Name))) depth[s.Name] = unreachableDepth;

        var columns = depth.GroupBy(d => d.Value).OrderBy(g => g.Key).ToList();
        var columnGap = columns.Count > 1 ? Math.Min(170, (width - 220) / (columns.Count - 1)) : 0;
        for (var c = 0; c < columns.Count; c++)
        {
            var members = columns[c].Select(d => FindState(d.Key)!).ToList();
            var rowGap = Math.Min(150, (height - 160) / Math.Max(1, members.Count));
            for (var r = 0; r < members.Count; r++)
            {
                members[r].X = 110 + c * columnGap;
                members[r].Y = height / 2 + (r - (members.Count - 1) / 2.0) * rowGap;
            }
        }
    }

    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = false };

    public string ToJson() => JsonSerializer.Serialize(this, JsonOptions);

    public static Automaton FromJson(string json) =>
        JsonSerializer.Deserialize<Automaton>(json, JsonOptions) ?? new Automaton();

    public Automaton Clone() => FromJson(ToJson());

    public static Automaton Blank(MachineKind kind)
    {
        var machine = new Automaton
        {
            Kind = kind,
            Name = $"Untitled {kind.ShortName()}",
            StackAlphabet = kind == MachineKind.Pda ? ['a', 'b'] : []
        };
        var s = machine.AddState(140, 260);
        if (kind == MachineKind.Turing)
        {
            var h = machine.AddState(420, 260);
            machine.RenameState(h, "h");
            h.IsHalting = true;
            machine.Transitions.Add(new TransitionRule { From = s.Name, To = s.Name, Read = Symbols.LeftEnd.ToString(), Action = Symbols.MoveRight });
        }
        return machine;
    }
}
