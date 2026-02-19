namespace EngineCore;

public class BoardClassifier
{
    private static readonly uint[,] CombinationTable = PrecomputeCombinationTable();

    /// <summary>
    /// Classifies the position based on the GNU Backgammon hierarchy found in eval.c.
    /// </summary>
    public PositionClass Classify(GameState state)
    {
        // Find furthest back checkers for both players.
        int nOppBack = GetFurthestBack(state.Player1Checkers);
        int nBack = GetFurthestBack(state.Player2Checkers);

        // 1. Over: Game is finished if a player has no checkers.
        if (nBack < 0 || nOppBack < 0) 
            return PositionClass.Over;

        // 2. Contact vs. Non-Contact:
        // If back checkers have passed (sum <= 22), contact is impossible.
        if (nBack + nOppBack > 22)
        {
            const int N = 6; 
            // Check if either side is "Crashed" within a contact position.
            if (IsCrashed(state.Player1Checkers, N) || IsCrashed(state.Player2Checkers, N))
            {
                return PositionClass.Crashed;
            }

            return PositionClass.Contact;
        }
        
        // 3. Race vs. Bearoff:
        // If any checker is outside the home board (index > 5), it is a Race.
        if (nBack > 5 || nOppBack > 5) 
            return PositionClass.Race;

        // 4. Bearoff classification:
        // Uses the enumeration threshold (923) from eval.c to decide DB type.
        if (GetPositionBearoff(state.Player1Checkers) > 923 || 
            GetPositionBearoff(state.Player2Checkers) > 923)
        {
            return PositionClass.BearoffOneSided;
        }

        return PositionClass.BearoffTwoSided;
    }

    private int GetFurthestBack(int[] checkers)
    {
        for (int i = 24; i >= 0; i--)
        {
            if (checkers[i] > 0) return i;
        }
        return -1;
    }

    private bool IsCrashed(int[] checkers, int threshold)
    {
        int total = 0;
        foreach (int count in checkers) total += count;

        // Position is crashed if total checkers <= threshold (6).
        if (total <= threshold) return true;

        // Check for "crunched" boards where checkers are trapped on the 1-point.
        if (checkers[0] > 1 && (total - checkers[0]) <= threshold) return true;

        return false;
    }

    /// <summary>
    /// Port of PositionBearoff from positionid.c. 
    /// Enumerates 15 checkers across 6 points.
    /// </summary>
    public uint GetPositionBearoff(int[] playerBoard)
    {
        uint nPoints = 6;
        uint nChequers = 15;
        uint fBits, j;

        j = nPoints - 1;
        for (int i = 0; i < (int)nPoints; i++)
            j += (uint)playerBoard[i];

        fBits = 1u << (int)j;

        for (int i = 0; i < (int)nPoints - 1; i++) {
            j -= (uint)playerBoard[i] + 1;
            fBits |= (1u << (int)j);
        }

        return PositionF(fBits, nChequers + nPoints, nPoints);
    }

    private uint PositionF(uint fBits, uint n, uint r)
    {
        if (n == r) return 0;

        // Recursive enumeration logic matching PositionF in positionid.c.
        if ((fBits & (1u << (int)(n - 1))) != 0)
            return CombinationTable[n - 2, r - 1] + PositionF(fBits, n - 1, r - 1);

        return PositionF(fBits, n - 1, r);
    }

    private static uint[,] PrecomputeCombinationTable()
    {
        uint[,] table = new uint[40, 25]; // MAX_N and MAX_R from positionid.c.
        for (uint i = 0; i < 40; i++) table[i, 0] = i + 1;
        for (uint i = 1; i < 40; i++)
            for (uint j = 1; j < 25; j++)
                table[i, j] = table[i - 1, j - 1] + table[i - 1, j];
        return table;
    }
}