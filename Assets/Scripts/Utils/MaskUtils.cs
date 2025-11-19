using UnityEngine;

public enum Direction { North = 0, South = 1, East = 2, West = 3 }

public static class MaskUtils
{
    // Bits: N=1, S=2, E=4, W=8
    public const int N = 1;
    public const int S = 2;
    public const int E = 4;
    public const int W = 8;

    // Rotaciona a máscara 90° clockwise `steps` vezes
    public static int RotateMask(int mask, int steps)
    {
        steps = ((steps % 4) + 4) % 4;
        int m = mask;
        for (int i = 0; i < steps; i++)
        {
            int n = 0;
            if ((m & N) != 0) n |= E; // N -> E
            if ((m & E) != 0) n |= S; // E -> S
            if ((m & S) != 0) n |= W; // S -> W
            if ((m & W) != 0) n |= N; // W -> N
            m = n;
        }
        return m;
    }

    // retorna quantas portas em comum entre maskA e maskB
    public static int CommonOpenings(int a, int b)
    {
        return CountBits(a & b);
    }

    public static int CountBits(int x)
    {
        int c = 0;
        while (x != 0) { c += x & 1; x >>= 1; }
        return c;
    }

    public static Direction Opposite(Direction d)
    {
        switch (d)
        {
            case Direction.North: return Direction.South;
            case Direction.South: return Direction.North;
            case Direction.East: return Direction.West;
            case Direction.West: return Direction.East;
            default: return Direction.North;
        }
    }

    public static int DirectionToMask(Direction d)
    {
        switch (d)
        {
            case Direction.North: return N;
            case Direction.South: return S;
            case Direction.East: return E;
            case Direction.West: return W;
            default: return 0;
        }
    }

    public static int RequiredMaskForConnector(Direction connectorDir)
    {
        // Se o conector do currentRoom é North, a nova sala precisa ter SOUTH
        return DirectionToMask(Opposite(connectorDir));
    }
}
