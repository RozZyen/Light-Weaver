using UnityEngine;

namespace LightWeaver.Core
{
    /// <summary>
    /// Oyuncu ilerlemesi, en yüksek skor, en uzak mesafe ve ses/haptik tercihlerini
    /// PlayerPrefs üzerinde güvenli ve sıfır tahsisle (Zero-GC) saklar.
    /// </summary>
    public static class ProgressManager
    {
        private const string KeyHighScore = "LW_HIGH_SCORE";
        private const string KeyBestDistance = "LW_BEST_DISTANCE";
        private const string KeyMusicEnabled = "LW_MUSIC_ENABLED";
        private const string KeySfxEnabled = "LW_SFX_ENABLED";
        private const string KeyHapticsEnabled = "LW_HAPTICS_ENABLED";

        public static int HighScore { get; private set; }
        public static float BestDistanceMeters { get; private set; }
        public static bool MusicEnabled { get; private set; } = true;
        public static bool SfxEnabled { get; private set; } = true;
        public static bool HapticsEnabled { get; private set; } = true;

        private static bool _loaded;

        public static void EnsureLoaded()
        {
            if (_loaded) return;
            _loaded = true;

            HighScore = PlayerPrefs.GetInt(KeyHighScore, 0);
            BestDistanceMeters = PlayerPrefs.GetFloat(KeyBestDistance, 0f);
            MusicEnabled = PlayerPrefs.GetInt(KeyMusicEnabled, 1) == 1;
            SfxEnabled = PlayerPrefs.GetInt(KeySfxEnabled, 1) == 1;
            HapticsEnabled = PlayerPrefs.GetInt(KeyHapticsEnabled, 1) == 1;
        }

        public static bool TryRecordRunResult(int score, float distanceMeters, int sectorReached)
        {
            EnsureLoaded();
            bool isNewBest = false;

            if (score > HighScore)
            {
                HighScore = score;
                PlayerPrefs.SetInt(KeyHighScore, HighScore);
                isNewBest = true;
            }

            if (distanceMeters > BestDistanceMeters)
            {
                BestDistanceMeters = distanceMeters;
                PlayerPrefs.SetFloat(KeyBestDistance, BestDistanceMeters);
            }

            PlayerPrefs.Save();
            return isNewBest;
        }

        public static void SetMusicEnabled(bool enabled)
        {
            EnsureLoaded();
            MusicEnabled = enabled;
            PlayerPrefs.SetInt(KeyMusicEnabled, enabled ? 1 : 0);
            PlayerPrefs.Save();
        }

        public static void SetSfxEnabled(bool enabled)
        {
            EnsureLoaded();
            SfxEnabled = enabled;
            PlayerPrefs.SetInt(KeySfxEnabled, enabled ? 1 : 0);
            PlayerPrefs.Save();
        }

        public static void SetHapticsEnabled(bool enabled)
        {
            EnsureLoaded();
            HapticsEnabled = enabled;
            PlayerPrefs.SetInt(KeyHapticsEnabled, enabled ? 1 : 0);
            PlayerPrefs.Save();
        }
    }
}
