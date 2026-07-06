using System.Collections.Generic;

namespace CubeBurst.Core
{
    public class CubeUnit
    {
        public int Id;
        public int X, Y, Z;
        public GameColor Color;
        public int BallCount;
        public bool IsExposed;
    }

    /// Voxel model of the tappable polycube. Pure logic, no Unity dependency.
    public class CubeShapeModel
    {
        readonly Dictionary<long, CubeUnit> _byPos = new Dictionary<long, CubeUnit>();
        readonly Dictionary<int, CubeUnit> _byId = new Dictionary<int, CubeUnit>();

        static long Key(int x, int y, int z) =>
            ((long)(x + 64) << 32) | ((long)(y + 64) << 16) | (uint)(z + 64);

        public int Count => _byId.Count;
        public IEnumerable<CubeUnit> Cubes => _byId.Values;

        public CubeShapeModel(LevelData data)
        {
            int id = 0;
            foreach (var def in data.cubes)
            {
                var cube = new CubeUnit
                {
                    Id = id++,
                    X = def.x, Y = def.y, Z = def.z,
                    Color = (GameColor)def.color,
                    BallCount = def.ballCount,
                };
                _byPos[Key(def.x, def.y, def.z)] = cube;
                _byId[cube.Id] = cube;
            }
            RecomputeExposure();
        }

        public CubeUnit Get(int id) => _byId.TryGetValue(id, out var c) ? c : null;

        public bool Occupied(int x, int y, int z) => _byPos.ContainsKey(Key(x, y, z));

        /// Which 90°-snapped yaw the player rotated the shape to (0..3).
        /// 0 is the authored view; levels are guaranteed solvable there.
        public int Orientation { get; private set; }

        public void SetOrientation(int orientation)
        {
            Orientation = ((orientation % 4) + 4) % 4;
            RecomputeExposure();
        }

        public void Remove(int id)
        {
            if (!_byId.TryGetValue(id, out var cube)) return;
            _byId.Remove(id);
            _byPos.Remove(Key(cube.X, cube.Y, cube.Z));
            RecomputeExposure();
        }

        /// Grid-space signs of the two camera-facing horizontal axes for each
        /// snapped yaw (+y is always camera-facing under the fixed -33° pitch).
        public static void FacingSigns(int orientation, out int sx, out int sz)
        {
            sx = orientation == 0 || orientation == 1 ? 1 : -1;
            sz = orientation == 0 || orientation == 3 ? 1 : -1;
        }

        /// A cube can be tapped when at least one of its three camera-facing
        /// neighbours (for the current orientation) is empty.
        public void RecomputeExposure()
        {
            FacingSigns(Orientation, out int sx, out int sz);
            foreach (var c in _byId.Values)
                c.IsExposed = !(Occupied(c.X + sx, c.Y, c.Z) &&
                                Occupied(c.X, c.Y + 1, c.Z) &&
                                Occupied(c.X, c.Y, c.Z + sz));
        }
    }
}
