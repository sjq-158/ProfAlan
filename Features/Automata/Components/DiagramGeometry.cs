using System.Globalization;
using ProfAlan.Domain.Automata;

namespace ProfAlan.Features.Automata.Components;

public sealed record EdgeLabelLine(string Text, string? RuleId, double X, double Y);

public sealed record EdgeShape(
    string From,
    string To,
    IReadOnlyList<TransitionRule> Rules,
    string Path,
    string Anchor,
    IReadOnlyList<EdgeLabelLine> Lines);

/// <summary>
/// Lays out arrows the way state diagrams are drawn by hand: forward arrows straight with
/// the label above, a reverse arrow arcing below, self-loops on top of the state, and every
/// rule on an arrow stacked as its own line.
/// </summary>
public static class DiagramGeometry
{
    public const double Radius = 26;
    public const double Width = 900;
    public const double Height = 520;
    private const double LineHeight = 15;

    public static string F(double v) => v.ToString("0.#", CultureInfo.InvariantCulture);

    public static IEnumerable<EdgeShape> Edges(Automaton machine)
    {
        var groups = machine.Transitions
            .GroupBy(t => (t.From, t.To))
            .ToList();

        foreach (var group in groups)
        {
            var a = machine.FindState(group.Key.From);
            var b = machine.FindState(group.Key.To);
            if (a is null || b is null) continue;

            var rules = group.ToList();
            var labels = LabelLines(machine.Kind, rules);

            if (a == b)
            {
                yield return Loop(a, rules, labels);
                continue;
            }

            var hasReverse = groups.Any(g => g.Key.From == b.Name && g.Key.To == a.Name);
            var blocked = machine.States.Any(c => c != a && c != b && DistanceToSegment(c, a, b) < Radius + 10);
            var bend = blocked ? 58 : hasReverse ? 30 : 0;
            yield return Arc(a, b, rules, labels, bend);
        }
    }

    private static List<(string Text, string? RuleId)> LabelLines(MachineKind kind, List<TransitionRule> rules)
    {
        if (kind is MachineKind.Dfa or MachineKind.Nfa)
        {
            return [(string.Join(", ", rules.Select(r => r.Label(kind))), rules.Count == 1 ? rules[0].Id : null)];
        }
        return rules.Select(r => (r.Label(kind), (string?)r.Id)).ToList();
    }

    private static EdgeShape Loop(StateNode s, List<TransitionRule> rules, List<(string Text, string? RuleId)> labels)
    {
        var dx = Radius * 0.5;
        var dy = Radius * 0.866;
        var path = $"M {F(s.X - dx)} {F(s.Y - dy)} C {F(s.X - 46)} {F(s.Y - 92)}, {F(s.X + 46)} {F(s.Y - 92)}, {F(s.X + dx)} {F(s.Y - dy)}";

        var baseY = s.Y - 80;
        var lines = labels
            .Select((l, i) => new EdgeLabelLine(l.Text, l.RuleId, s.X, baseY - (labels.Count - 1 - i) * LineHeight))
            .ToList();
        return new EdgeShape(s.Name, s.Name, rules, path, "middle", lines);
    }

    private static EdgeShape Arc(StateNode a, StateNode b, List<TransitionRule> rules, List<(string Text, string? RuleId)> labels, double bend)
    {
        var dx = b.X - a.X;
        var dy = b.Y - a.Y;
        var length = Math.Max(1, Math.Sqrt(dx * dx + dy * dy));

        // The label side: above for left-to-right arrows, below for right-to-left ones.
        var sx = dy / length;
        var sy = -dx / length;

        var cx = (a.X + b.X) / 2 + sx * bend;
        var cy = (a.Y + b.Y) / 2 + sy * bend;

        var (x1, y1) = Toward(a.X, a.Y, cx, cy, Radius);
        var (x2, y2) = Toward(b.X, b.Y, cx, cy, Radius + 1);
        var path = bend == 0
            ? $"M {F(x1)} {F(y1)} L {F(x2)} {F(y2)}"
            : $"M {F(x1)} {F(y1)} Q {F(cx)} {F(cy)} {F(x2)} {F(y2)}";

        var mx = 0.25 * x1 + 0.5 * cx + 0.25 * x2;
        var my = 0.25 * y1 + 0.5 * cy + 0.25 * y2;
        var lx = mx + sx * 12;
        var ly = my + sy * 12;

        string anchor;
        List<EdgeLabelLine> lines;
        if (Math.Abs(sx) > 0.75)
        {
            anchor = sx > 0 ? "start" : "end";
            var top = ly - (labels.Count - 1) * LineHeight / 2 + 4;
            lines = labels.Select((l, i) => new EdgeLabelLine(l.Text, l.RuleId, lx, top + i * LineHeight)).ToList();
        }
        else
        {
            anchor = "middle";
            lines = sy < 0
                ? labels.Select((l, i) => new EdgeLabelLine(l.Text, l.RuleId, lx, ly - (labels.Count - 1 - i) * LineHeight)).ToList()
                : labels.Select((l, i) => new EdgeLabelLine(l.Text, l.RuleId, lx, ly + 10 + i * LineHeight)).ToList();
        }

        return new EdgeShape(a.Name, b.Name, rules, path, anchor, lines);
    }

    private static (double X, double Y) Toward(double x, double y, double tx, double ty, double distance)
    {
        var dx = tx - x;
        var dy = ty - y;
        var length = Math.Max(1, Math.Sqrt(dx * dx + dy * dy));
        return (x + dx / length * distance, y + dy / length * distance);
    }

    private static double DistanceToSegment(StateNode p, StateNode a, StateNode b)
    {
        var dx = b.X - a.X;
        var dy = b.Y - a.Y;
        var lengthSquared = dx * dx + dy * dy;
        if (lengthSquared == 0) return double.MaxValue;
        var t = Math.Clamp(((p.X - a.X) * dx + (p.Y - a.Y) * dy) / lengthSquared, 0, 1);
        var px = a.X + t * dx - p.X;
        var py = a.Y + t * dy - p.Y;
        return Math.Sqrt(px * px + py * py);
    }

    public static (double X, double Y) Clamp(double x, double y) =>
        (Math.Clamp(x, Radius + 6, Width - Radius - 6), Math.Clamp(y, Radius + 70, Height - Radius - 8));
}
