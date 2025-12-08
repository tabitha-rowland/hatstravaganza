using Microsoft.Xna.Framework;

namespace Hatstravaganza
{
    // positioning offset data for a hat
    public class HatOffset
    {
        public int X { get; set; }
        public int Y { get; set; }
        
        public HatOffset()
        {
            X = 0;
            Y = -8; // Default offset
        }
        
        public HatOffset(int x, int y)
        {
            X = x;
            Y = y;
        }
        
        public Vector2 ToVector2()
        {
            return new Vector2(X, Y);
        }
    }
    
    // Stores hat offsets for all four directions
    public class NPCHatOffsets
    {
        public HatOffset Down { get; set; }
        public HatOffset Up { get; set; }
        public HatOffset Left { get; set; }
        public HatOffset Right { get; set; }
        
        public NPCHatOffsets()
        {
            // Default offsets for all directions
            Down = new HatOffset(0, -8);
            Up = new HatOffset(0, -8);
            Left = new HatOffset(0, -8);
            Right = new HatOffset(0, -8);
        }
        
        // Get offset for a specific direction
        // <param name="direction">0=up, 1=right, 2=down, 3=left</param>
        public HatOffset GetOffsetForDirection(int direction)
        {
            return direction switch
            {
                0 => Up,
                1 => Right,
                2 => Down,
                3 => Left,
                _ => Down // Fallback
            };
        }
    }
}