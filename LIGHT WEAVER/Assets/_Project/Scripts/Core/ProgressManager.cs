using System;
using UnityEngine;

namespace LightWeaver.Core
{
    public enum ShopUpgradeType
    {
        BatteryCapacity = 0,
        BatteryEfficiency = 1,
        CoinMagnet = 2
    }

    /// <summary>
    /// Oyuncu ilerlemesi, en yüksek skor, en uzak mesafe, oyun içi altın (Scarab Coin),
    /// kalıcı atölye yükseltmeleri ve ses/haptik tercihlerini PlayerPrefs üzerinde saklar.
    /// </summary>
    public static class ProgressManager
    {
        private const string KeyHighScore = "LW_HIGH_SCORE";
        private const string KeyBestDistance = "LW_BEST_DISTANCE";
        private const string KeyCoins = "LW_SCARAB_COINS";
        private const string KeyCapLevel = "LW_UPG_CAP";
        private const string KeyEffLevel = "LW_UPG_EFF";
        private const string KeyMagLevel = "LW_UPG_MAG";
        private const string KeyMusicEnabled = "LW_MUSIC_ENABLED";
        private const string KeySfxEnabled = "LW_SFX_ENABLED";
        private const string KeyHapticsEnabled = "LW_HAPTICS_ENABLED";

        public static int HighScore { get; private set; }
        public static float BestDistanceMeters { get; private set; }
        public static int Coins { get; private set; }
        public static int BatteryCapacityLevel { get; private set; }
        public static int BatteryEfficiencyLevel { get; private set; }
        public static int CoinMagnetLevel { get; private set; }
        public static bool MusicEnabled { get; private set; } = true;
        public static bool SfxEnabled { get; private set; } = true;
        public static bool HapticsEnabled { get; private set; } = true;

        public static event Action<int> OnCoinsChanged;
        public static event Action OnUpgradesChanged;

        private static readonly int[] CapCosts = { 50, 110, 190, 290, 420 };
        private static readonly int[] EffCosts = { 65, 140, 230, 350, 500 };
        private static readonly int[] MagCosts = { 45, 100, 175, 275 };

        private static bool _loaded;

        public static void EnsureLoaded()
        {
            if (_loaded) return;
            _loaded = true;

            HighScore = PlayerPrefs.GetInt(KeyHighScore, 0);
            BestDistanceMeters = PlayerPrefs.GetFloat(KeyBestDistance, 0f);
            Coins = Mathf.Max(0, PlayerPrefs.GetInt(KeyCoins, 0));
            BatteryCapacityLevel = Mathf.Clamp(PlayerPrefs.GetInt(KeyCapLevel, 0), 0, CapCosts.Length);
            BatteryEfficiencyLevel = Mathf.Clamp(PlayerPrefs.GetInt(KeyEffLevel, 0), 0, EffCosts.Length);
            CoinMagnetLevel = Mathf.Clamp(PlayerPrefs.GetInt(KeyMagLevel, 0), 0, MagCosts.Length);
            MusicEnabled = PlayerPrefs.GetInt(KeyMusicEnabled, 1) == 1;
            SfxEnabled = PlayerPrefs.GetInt(KeySfxEnabled, 1) == 1;
            HapticsEnabled = PlayerPrefs.GetInt(KeyHapticsEnabled, 1) == 1;
        }

        public static void AddCoins(int amount)
        {
            if (amount <= 0) return;
            EnsureLoaded();
            Coins += amount;
            PlayerPrefs.SetInt(KeyCoins, Coins);
            PlayerPrefs.Save();
            OnCoinsChanged?.Invoke(Coins);
        }

        public static int GetUpgradeLevel(ShopUpgradeType type)
        {
            EnsureLoaded();
            return type switch
            {
                ShopUpgradeType.BatteryCapacity => BatteryCapacityLevel,
                ShopUpgradeType.BatteryEfficiency => BatteryEfficiencyLevel,
                ShopUpgradeType.CoinMagnet => CoinMagnetLevel,
                _ => 0
            };
        }

        public static int GetMaxUpgradeLevel(ShopUpgradeType type)
        {
            return type switch
            {
                ShopUpgradeType.BatteryCapacity => CapCosts.Length,
                ShopUpgradeType.BatteryEfficiency => EffCosts.Length,
                ShopUpgradeType.CoinMagnet => MagCosts.Length,
                _ => 0
            };
        }

        public static int GetUpgradeCost(ShopUpgradeType type)
        {
            EnsureLoaded();
            int level = GetUpgradeLevel(type);
            return type switch
            {
                ShopUpgradeType.BatteryCapacity => level < CapCosts.Length ? CapCosts[level] : -1,
                ShopUpgradeType.BatteryEfficiency => level < EffCosts.Length ? EffCosts[level] : -1,
                ShopUpgradeType.CoinMagnet => level < MagCosts.Length ? MagCosts[level] : -1,
                _ => -1
            };
        }

        public static bool TryPurchaseUpgrade(ShopUpgradeType type)
        {
            EnsureLoaded();
            int cost = GetUpgradeCost(type);
            if (cost < 0 || Coins < cost) return false;

            Coins -= cost;
            PlayerPrefs.SetInt(KeyCoins, Coins);

            switch (type)
            {
                case ShopUpgradeType.BatteryCapacity:
                    BatteryCapacityLevel++;
                    PlayerPrefs.SetInt(KeyCapLevel, BatteryCapacityLevel);
                    break;
                case ShopUpgradeType.BatteryEfficiency:
                    BatteryEfficiencyLevel++;
                    PlayerPrefs.SetInt(KeyEffLevel, BatteryEfficiencyLevel);
                    break;
                case ShopUpgradeType.CoinMagnet:
                    CoinMagnetLevel++;
                    PlayerPrefs.SetInt(KeyMagLevel, CoinMagnetLevel);
                    break;
            }

            PlayerPrefs.Save();
            OnCoinsChanged?.Invoke(Coins);
            OnUpgradesChanged?.Invoke();
            return true;
        }

        public static float GetMaxBatteryBonus()
        {
            EnsureLoaded();
            return BatteryCapacityLevel * 20f;
        }

        public static float GetMetaDrainMultiplier()
        {
            EnsureLoaded();
            return Mathf.Clamp(1.0f - BatteryEfficiencyLevel * 0.04f, 0.75f, 1.0f);
        }

        public static float GetCoinMagnetBonusRadius()
        {
            EnsureLoaded();
            return CoinMagnetLevel * 0.75f;
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
