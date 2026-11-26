namespace ProfAlan.Models;

public sealed record Production(string Lhs, string Rhs);

public sealed record Grammar(string StartSymbol, IReadOnlyList<Production> Productions);