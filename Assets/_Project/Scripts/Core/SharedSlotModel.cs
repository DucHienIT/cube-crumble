using System.Collections.Generic;

namespace CubeBurst.Core
{
    /// Overflow tray for balls that had no matching open container.
    public class SharedSlotModel
    {
        public readonly int Capacity;
        readonly List<GameColor> _balls = new List<GameColor>();

        public IReadOnlyList<GameColor> Balls => _balls;
        public int Count => _balls.Count;
        public bool IsFull => _balls.Count >= Capacity;

        public SharedSlotModel(int capacity)
        {
            Capacity = capacity;
        }

        public void Add(GameColor c) => _balls.Add(c);

        public bool RemoveOne(GameColor c)
        {
            int i = _balls.LastIndexOf(c);
            if (i < 0) return false;
            _balls.RemoveAt(i);
            return true;
        }

        public int CountOf(GameColor c)
        {
            int n = 0;
            foreach (var b in _balls) if (b == c) n++;
            return n;
        }
    }
}
