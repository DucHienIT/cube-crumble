using UnityEngine;

namespace CubeBurst.Systems
{
    public static class SaveSystem
    {
        const string Prefix = "CubeBurst_";

        public static int UnlockedLevel
        {
            get => PlayerPrefs.GetInt(Prefix + "Unlocked", 1);
            set
            {
                PlayerPrefs.SetInt(Prefix + "Unlocked", value);
                PlayerPrefs.Save();
            }
        }

        public static int GetStars(int level) => PlayerPrefs.GetInt(Prefix + "Stars_" + level, 0);

        public static bool SoundOn
        {
            get => PlayerPrefs.GetInt(Prefix + "Sound", 1) == 1;
            set
            {
                PlayerPrefs.SetInt(Prefix + "Sound", value ? 1 : 0);
                PlayerPrefs.Save();
            }
        }

        public static void CompleteLevel(int level, int stars, int totalLevels)
        {
            if (stars > GetStars(level)) PlayerPrefs.SetInt(Prefix + "Stars_" + level, stars);
            if (level + 1 > UnlockedLevel && level + 1 <= totalLevels) UnlockedLevel = level + 1;
            PlayerPrefs.Save();
        }
    }
}
