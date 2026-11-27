using System.Text;

namespace ProfAlan.Domain.Automata;

/// <summary>A one-way infinite tape that starts with ▷ in cell 0, together with its read/write head.</summary>
public sealed class Tape
{
    private readonly StringBuilder cells;

    public int Head { get; private set; }

    /// <summary>The initial configuration on input w is (s, ▷⊔w) with the head on the blank before w.</summary>
    public Tape(string input)
    {
        cells = new StringBuilder().Append(Symbols.LeftEnd).Append(Symbols.Blank).Append(input);
        Head = 1;
    }

    public char Read() => cells[Head];

    public void Write(char symbol) => cells[Head] = symbol;

    public void MoveLeft()
    {
        if (Head > 0) Head--;
    }

    public void MoveRight()
    {
        Head++;
        if (Head == cells.Length) cells.Append(Symbols.Blank);
    }

    public string Contents => cells.ToString();

    /// <summary>The tape after ▷, without trailing blanks: the machine's output.</summary>
    public string Output => cells.ToString(1, cells.Length - 1).TrimEnd(Symbols.Blank);
}
