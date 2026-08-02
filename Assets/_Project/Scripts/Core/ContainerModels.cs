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

    /// Four visible container slots, each fed by its OWN column queue. The
    /// level's flat container list is dealt round-robin across the columns
    /// (item i -> column i % SlotCount), so each slot only ever refills from
    /// the stack shown directly beneath it — a completed container slides up
    /// within its own column instead of jumping across. Because every cube's
    /// three same-color containers are three consecutive entries (and three
    /// consecutive indices always fall in three distinct columns), all three
    /// stay simultaneously available in intended play order — solvability is
    /// preserved.
    public class ContainerManagerModel
    {
        public const int SlotCount = 4;

        public readonly ContainerModel[] Active = new ContainerModel[SlotCount];
        readonly Queue<ContainerModel>[] _columns = new Queue<ContainerModel>[SlotCount];

        /// (slot, container) — container is null when that column ran out.
        public event Action<int, ContainerModel> ContainerEntered;
        public event Action<int, ContainerModel> ContainerCompleted;

        public int QueueRemaining
        {
            get
            {
                int n = 0;
                for (int i = 0; i < SlotCount; i++) n += _columns[i].Count;
                return n;
            }
        }

        /// Upcoming containers stacked under one column, front first (display).
        public ContainerModel[] ColumnSnapshot(int col) => _columns[col].ToArray();

        public ContainerManagerModel(LevelData data)
        {
            for (int i = 0; i < SlotCount; i++) _columns[i] = new Queue<ContainerModel>();
            int idx = 0;
            foreach (var def in data.containerQueue)
            {
                _columns[idx % SlotCount].Enqueue(
                    new ContainerModel { Color = (GameColor)def.color, Capacity = def.capacity });
                idx++;
            }
        }

        public void FillInitialSlots()
        {
            for (int i = 0; i < SlotCount; i++)
            {
                Active[i] = _columns[i].Count > 0 ? _columns[i].Dequeue() : null;
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
            Active[slot] = _columns[slot].Count > 0 ? _columns[slot].Dequeue() : null;
            ContainerEntered?.Invoke(slot, Active[slot]);
            return true;
        }
    }
}
