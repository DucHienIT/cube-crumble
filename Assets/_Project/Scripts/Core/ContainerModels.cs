using System;
using System.Collections.Generic;

namespace CubeBurst.Core
{
    public class ContainerModel
    {
        public GameColor Color;
        public int Capacity;
        public int Filled;
        public int InFlight;

        public int FreeSpace => Capacity - Filled - InFlight;
        public bool IsFull => Filled >= Capacity;
    }

    /// Four visible container slots fed by a per-level queue.
    public class ContainerManagerModel
    {
        public const int SlotCount = 4;

        public readonly ContainerModel[] Active = new ContainerModel[SlotCount];
        readonly Queue<ContainerModel> _queue = new Queue<ContainerModel>();

        /// (slot, container) — container is null when the queue ran out.
        public event Action<int, ContainerModel> ContainerEntered;
        public event Action<int, ContainerModel> ContainerCompleted;

        public int QueueRemaining => _queue.Count;

        /// Upcoming containers in dequeue order (for the queue-stack display).
        public ContainerModel[] QueueSnapshot() => _queue.ToArray();

        public ContainerManagerModel(LevelData data)
        {
            foreach (var def in data.containerQueue)
                _queue.Enqueue(new ContainerModel { Color = (GameColor)def.color, Capacity = def.capacity });
        }

        public void FillInitialSlots()
        {
            for (int i = 0; i < SlotCount; i++)
            {
                Active[i] = _queue.Count > 0 ? _queue.Dequeue() : null;
                ContainerEntered?.Invoke(i, Active[i]);
            }
        }

        /// Reserves space for one incoming ball. Returns the slot index or -1.
        public int TryReserve(GameColor color)
        {
            for (int i = 0; i < SlotCount; i++)
            {
                var c = Active[i];
                if (c != null && c.Color == color && c.FreeSpace > 0)
                {
                    c.InFlight++;
                    return i;
                }
            }
            return -1;
        }

        /// Consumes a reservation. Returns true when the container completed
        /// (the next queued container has already been shifted in).
        public bool BallArrived(int slot)
        {
            var c = Active[slot];
            if (c == null) return false;
            c.InFlight--;
            c.Filled++;
            if (!c.IsFull) return false;

            ContainerCompleted?.Invoke(slot, c);
            Active[slot] = _queue.Count > 0 ? _queue.Dequeue() : null;
            ContainerEntered?.Invoke(slot, Active[slot]);
            return true;
        }
    }
}
