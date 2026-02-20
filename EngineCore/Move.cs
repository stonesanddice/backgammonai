using System.Collections.Generic;

namespace EngineCore;

public struct Move
{
    public int From { get; set; } // 24 = Bar, 0-23 = Board Points
    public int To { get; set; }   // -1 = Born off, 0-23 = Board Points
    public bool IsHit { get; set; } // Did this move hit an opponent's blot?

    public override string ToString()
    {
        string fromStr = From == 24 ? "Bar" : (From + 1).ToString();
        string toStr = To == -1 ? "Off" : (To + 1).ToString();
        return $"{fromStr}/{toStr}{(IsHit ? "*" : "")}";
    }
}

public class Turn
{
    public List<Move> Moves { get; set; } = new List<Move>();

    // Tracks exactly which dice values were used in this turn
    public List<int> DiceUsed { get; set; } = new List<int>();

    public GameState? ResultingState { get; set; }

    public override string ToString()
    {
        return string.Join(" ", Moves);
    }
}