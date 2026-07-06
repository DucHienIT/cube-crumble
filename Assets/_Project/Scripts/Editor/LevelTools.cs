using System.Collections.Generic;
using CubeBurst.Core;
using UnityEditor;
using UnityEngine;

namespace CubeBurst.EditorTools
{
    /// Sanity checks for the generated level JSONs.
    public static class LevelTools
    {
        [MenuItem("Tools/Cube Burst/Validate Levels")]
        public static void ValidateAll()
        {
            int ok = 0, bad = 0;
            for (int i = 1; i <= 999; i++)
            {
                var ta = Resources.Load<TextAsset>($"Levels/level_{i:000}");
                if (ta == null) break;

                var data = JsonUtility.FromJson<LevelData>(ta.text);
                var errors = Validate(data);
                if (errors.Count == 0)
                {
                    ok++;
                }
                else
                {
                    bad++;
                    Debug.LogError($"Level {i}: {string.Join("; ", errors)}");
                }
            }
            Debug.Log($"[CubeBurst] Level validation done: {ok} ok, {bad} bad.");
        }

        static List<string> Validate(LevelData data)
        {
            var errors = new List<string>();
            if (data.cubes.Count == 0) errors.Add("no cubes");
            if (data.timeLimitSeconds <= 0) errors.Add("bad time limit");
            if (data.sharedSlotCapacity <= 0) errors.Add("bad shared slot capacity");

            // total balls per color must exactly match total container capacity per color
            var ballTotals = new Dictionary<int, int>();
            foreach (var c in data.cubes)
                ballTotals[c.color] = ballTotals.GetValueOrDefault(c.color) + c.ballCount;
            var capTotals = new Dictionary<int, int>();
            foreach (var q in data.containerQueue)
                capTotals[q.color] = capTotals.GetValueOrDefault(q.color) + q.capacity;

            foreach (var pair in ballTotals)
                if (capTotals.GetValueOrDefault(pair.Key) != pair.Value)
                    errors.Add($"color {pair.Key}: {pair.Value} balls vs {capTotals.GetValueOrDefault(pair.Key)} capacity");
            foreach (var pair in capTotals)
                if (!ballTotals.ContainsKey(pair.Key))
                    errors.Add($"color {pair.Key}: containers but no cubes");

            // every stacked cube needs support below it
            var occupied = new HashSet<(int, int, int)>();
            foreach (var c in data.cubes) occupied.Add((c.x, c.y, c.z));
            foreach (var c in data.cubes)
                if (c.y > 0 && !occupied.Contains((c.x, c.y - 1, c.z)))
                    errors.Add($"floating cube at ({c.x},{c.y},{c.z})");

            return errors;
        }
    }
}
