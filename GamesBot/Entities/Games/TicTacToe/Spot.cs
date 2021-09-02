using System.Diagnostics;

namespace Games.Entities.Games.TicTacToe
{
    [DebuggerDisplay("Type: {Type}; {ToString(),nq}")]
    public class Spot
    {
        public SpotType Type { get; }
        public int[] Data { get; }

        public Spot(SpotType type, int[] buffer)
        {
            this.Type = type;
            this.Data = buffer;
        }

        public int this[int index]
            => Data[index];

        public override string ToString()
        {
            return $"{Data[0]},{Data[1]},{Data[2]}";
        }

        public static Spot[] Values { get; } = new Spot[]
        {
            new(SpotType.Horizontal, new []{0, 1, 2 }),
            new(SpotType.Horizontal, new []{3, 4, 5 }),
            new(SpotType.Horizontal, new []{6, 7, 8 }),

            new(SpotType.Vertical, new []{0, 3, 6 }),
            new(SpotType.Vertical, new []{1, 4, 7 }),
            new(SpotType.Vertical, new []{2, 5, 8 }),

            new(SpotType.Diagonal, new []{0, 4, 8 }),
            new(SpotType.Diagonal, new []{2, 4, 6 }),
        };
    }
}
