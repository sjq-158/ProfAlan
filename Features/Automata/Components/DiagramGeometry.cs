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
/// the label above, a reverse arrow arcing below, self-loops on the free side of the state,
/// and every rule on an arrow stacked as its own line. Finite automata use the small-dot
/// notation; PDAs and TMs keep labelled circles.
/// </summary>
public static class DiagramGeometry
{
    public const double Radius = 26;
    public const double DotRadius = 7;
    public const double Width = 900;
    public const double Height = 520;
    private const double LineHeight = 15;

    public static string F(double v) => v.ToString("0.#", CultureInfo.InvariantCulture);

    public static bool UsesDots(MachineKind kind) => kind is MachineKind.Dfa or MachineKind.Nfa;

    /// <summary>How far from a state's centre arrows start and end.</summary>
    public static double NodeRadius(MachineKind kind) => UsesDots(kind) ? DotRadius + 4 : Radius;

    /// <summary>World-space box around every state, with room for loops and labels.</summary>
    public static (double X, double Y, double W, double H) Bounds(Automaton machine)
    {
        if (machine.States.Count == 0) return (0, 0, Width, Height);
        var r = NodeRadius(machine.Kind);
        var minX = machine.States.Min(s => s.X) - r - 70;
        var maxX = machine.States.Max(s => s.X) + r + 60;
        var minY = machine.States.Min(s => s.Y) - r - 90;
        var maxY = machine.States.Max(s => s.Y) + r + 90;
        return (minX, minY, maxX - minX, maxY - minY);
    }

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
                yield return Loop(machine, a, rules, labels);
                continue;
            }

            var hasReverse = groups.Any(g => g.Key.From == b.Name && g.Key.To == a.Name);
            var r = NodeRadius(machine.Kind);
            var blocked = machine.States.Any(c => c != a && c != b && DistanceToSegment(c, a, b) < r + 12);
            var bend = blocked ? 58 : hasReverse ? 30 : 0;
            yield return Arc(a, b, rules, labels, bend, r);
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

    private static EdgeShape Loop(Automaton machine, StateNode s, List<TransitionRule> rules, List<(string Text, string? RuleId)> labels)
    {
        var angle = FreeDirection(machine, s);
        var (ux, uy) = (Math.Cos(angle), Math.Sin(angle));
        var dots = UsesDots(machine.Kind);
        var r = NodeRadius(machine.Kind);
        var reach = dots ? 48 : 70;
        var labelDistance = dots ? 46 : 62;

        (double X, double Y) At(double offset, double distance) =>
            (s.X + Math.Cos(angle + offset) * distance, s.Y + Math.Sin(angle + offset) * distance);

        var start = At(-0.52, r);
        var end = At(0.52, r);
        var c1 = At(-0.62, r + reach);
        var c2 = At(0.62, r + reach);
        var path = $"M {F(start.X)} {F(start.Y)} C {F(c1.X)} {F(c1.Y)}, {F(c2.X)} {F(c2.Y)}, {F(end.X)} {F(end.Y)}";

        var lx = s.X + ux * (r + labelDistance);
        var ly = s.Y + uy * (r + labelDistance);
        string anchor;
        List<EdgeLabelLine> lines;
        if (Math.Abs(ux) > 0.7)
        {
            anchor = ux > 0 ? "start" : "end";
            var top = ly - (labels.Count - 1) * LineHeight / 2 + 4;
            lines = labels.Select((l, i) => new EdgeLabelLine(l.Text, l.RuleId, lx + ux * 4, top + i * LineHeight)).ToList();
        }
        else
        {
            anchor = "middle";
            lines = uy < 0
                ? labels.Select((l, i) => new EdgeLabelLine(l.Text, l.RuleId, lx, ly - (labels.Count - 1 - i) * LineHeight)).ToList()
                : labels.Select((l, i) => new EdgeLabelLine(l.Text, l.RuleId, lx, ly + 10 + i * LineHeight)).ToList();
        }
        return new EdgeShape(s.Name, s.Name, rules, path, anchor, lines);
    }

    /// <summary>Picks up, down, right or left for a self-loop, whichever is furthest from the state's other arrows.</summary>
    private static double FreeDirection(Automaton machine, StateNode s)
    {
        var neighbourAngles = machine.Transitions
            .Where(t => (t.From == s.Name) != (t.To == s.Name))
            .Select(t => machine.FindState(t.From == s.Name ? t.To : t.From))
            .OfType<StateNode>()
            .Distinct()
            .Select(n => Math.Atan2(n.Y - s.Y, n.X - s.X))
            .ToList();
        if (s.IsInitial) neighbourAngles.Add(Math.PI);
        if (UsesDots(machine.Kind)) neighbourAngles.Add(Math.PI / 2);

        double[] candidates = [-Math.PI / 2, Math.PI / 2, 0, Math.PI];
        if (neighbourAngles.Count == 0) return candidates[0];

        static double Gap(double a, double b)
        {
            var d = Math.Abs(a - b) % (2 * Math.PI);
            return d > Math.PI ? 2 * Math.PI - d : d;
        }

        return candidates
            .Select((c, i) => (Angle: c, Clearance: neighbourAngles.Min(n => Gap(c, n)), Order: i))
            .OrderByDescending(x => Math.Round(x.Clearance, 2) >= 1.2 ? 1 : 0)
            .ThenBy(x => Math.Round(x.Clearance, 2) >= 1.2 ? x.Order : 0)
            .ThenByDescending(x => x.Clearance)
            .First().Angle;
    }

    private static EdgeShape Arc(StateNode a, StateNode b, List<TransitionRule> rules, List<(string Text, string? RuleId)> labels, double bend, double r)
    {
        var dx = b.X - a.X;
        var dy = b.Y - a.Y;
        var length = Math.Max(1, Math.Sqrt(dx * dx + dy * dy));

        // The label side: above for left-to-right arrows, below for right-to-left ones.
        var sx = dy / length;
        var sy = -dx / length;

        var cx = (a.X + b.X) / 2 + sx * bend;
        var cy = (a.Y + b.Y) / 2 + sy * bend;

        var (x1, y1) = Toward(a.X, a.Y, cx, cy, r);
        var (x2, y2) = Toward(b.X, b.Y, cx, cy, r + 1);
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
}
