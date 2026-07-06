using System;
using System.Collections.Generic;

namespace CubeBurst.Core
{
    public enum GameStatus { Playing, Won, LostTime, LostOverflow }

    public enum BallTarget { Container, SharedSlot }

    public class BallRoute
    {
        public GameColor Color;
        public BallTarget Target;
        public int Slot = -1;        // container slot when Target == Container
        public int SocketIndex = -1; // which of the container's holes this ball fills
        public bool FromSharedSlot;  // true for shared-slot -> container transfers
    }

    /// One level in progress. Pure logic; views drive it via TapCube/BallArrived
    /// and listen to its events.
    public class GameSession
    {
        public readonly LevelData Level;
        public readonly CubeShapeModel Shape;
        public readonly ContainerManagerModel Containers;
        public readonly SharedSlotModel Shared;

        public GameStatus Status { get; private set; } = GameStatus.Playing;
        public float TimeRemaining { get; private set; }
        public int TotalBalls { get; private set; }
        public int BallsDelivered { get; private set; }

        public event Action<GameStatus> Ended;
        public event Action ProgressChanged;
        /// A ball leaves the shared slot toward a newly opened container.
        public event Action<BallRoute> TransferSpawned;

        public GameSession(LevelData level)
        {
            Level = level;
            Shape = new CubeShapeModel(level);
            Containers = new ContainerManagerModel(level);
            Shared = new SharedSlotModel(level.sharedSlotCapacity);
            TimeRemaining = level.timeLimitSeconds;
            TotalBalls = level.TotalBalls;
        }

        /// Call after views have subscribed, so initial containers animate in.
        public void Begin() => Containers.FillInitialSlots();

        public void Tick(float dt)
        {
            if (Status != GameStatus.Playing) return;
            TimeRemaining -= dt;
            if (TimeRemaining <= 0f)
            {
                TimeRemaining = 0f;
                End(GameStatus.LostTime);
            }
        }

        /// Crumbles an exposed cube. Returns one route per spawned ball,
        /// or null if the tap was invalid.
        public List<BallRoute> TapCube(int cubeId)
        {
            if (Status != GameStatus.Playing) return null;
            var cube = Shape.Get(cubeId);
            if (cube == null || !cube.IsExposed) return null;

            Shape.Remove(cubeId);
            var routes = new List<BallRoute>(cube.BallCount);
            for (int i = 0; i < cube.BallCount; i++)
            {
                int slot = Containers.TryReserve(cube.Color);
                // after TryReserve the container's InFlight counts this ball, so
                // its eventual hole is Filled + InFlight - 1
                int socket = slot >= 0
                    ? Containers.Active[slot].Filled + Containers.Active[slot].InFlight - 1
                    : -1;
                routes.Add(new BallRoute
                {
                    Color = cube.Color,
                    Target = slot >= 0 ? BallTarget.Container : BallTarget.SharedSlot,
                    Slot = slot,
                    SocketIndex = socket,
                });
            }
            return routes;
        }

        public void BallArrived(BallRoute route)
        {
            if (Status != GameStatus.Playing) return;

            if (route.Target == BallTarget.Container)
            {
                bool completed = Containers.BallArrived(route.Slot);
                BallsDelivered++;
                ProgressChanged?.Invoke();
                if (completed) DrainSharedSlot();
                if (BallsDelivered >= TotalBalls) End(GameStatus.Won);
            }
            else
            {
                Shared.Add(route.Color);
                ProgressChanged?.Invoke();
                if (Shared.Count > Shared.Capacity) End(GameStatus.LostOverflow);
            }
        }

        /// After a container completes and a new one shifts in, matching balls
        /// waiting in the shared slot fly out to fill it.
        void DrainSharedSlot()
        {
            for (int slot = 0; slot < ContainerManagerModel.SlotCount; slot++)
            {
                var c = Containers.Active[slot];
                if (c == null) continue;
                while (c.FreeSpace > 0 && Shared.CountOf(c.Color) > 0)
                {
                    Shared.RemoveOne(c.Color);
                    c.InFlight++;
                    TransferSpawned?.Invoke(new BallRoute
                    {
                        Color = c.Color,
                        Target = BallTarget.Container,
                        Slot = slot,
                        SocketIndex = c.Filled + c.InFlight - 1,
                        FromSharedSlot = true,
                    });
                }
            }
        }

        void End(GameStatus status)
        {
            if (Status != GameStatus.Playing) return;
            Status = status;
            Ended?.Invoke(status);
        }
    }
}
