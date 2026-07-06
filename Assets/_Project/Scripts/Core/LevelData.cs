using System;
using System.Collections.Generic;

namespace CubeBurst.Core
{
    [Serializable]
    public class CubeDef
    {
        public int x;
        public int y;
        public int z;
        public int color;
        public int ballCount = 3;
    }

    [Serializable]
    public class ContainerDef
    {
        public int color;
        public int capacity = 3;
    }

    [Serializable]
    public class LevelData
    {
        public int levelId;
        public int timeLimitSeconds = 90;
        public int sharedSlotCapacity = 8;
        public List<CubeDef> cubes = new List<CubeDef>();
        public List<ContainerDef> containerQueue = new List<ContainerDef>();

        public int TotalBalls
        {
            get
            {
                int total = 0;
                foreach (var c in cubes) total += c.ballCount;
                return total;
            }
        }
    }
}
