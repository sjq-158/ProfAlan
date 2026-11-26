namespace ProfAlan.Models.Machines;

public sealed record State(
    string Id,
    bool IsStart = false,
    bool IsAccepting = false,
    double X = 0,
    double Y = 0);