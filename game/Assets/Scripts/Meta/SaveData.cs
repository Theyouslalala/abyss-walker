using UnityEngine;
using System.Collections.Generic;

namespace AbyssWalker.Meta
{
    /// <summary>
    /// Serializable data class for all persistent save data.
    /// Serialized to JSON and stored in PlayerPrefs or a local file.
    /// </summary>
    [System.Serializable]
    public class SaveData
    {
        // ── Permanent Currency ──
        public int abyssSouls;

        // ── Permanent Upgrades ──
        public int hpUpgradeLevel;
        public int atkUpgradeLevel;
        public int defUpgradeLevel;
        public int speedUpgradeLevel;
        public int critUpgradeLevel;
        public int potionHealUpgradeLevel;

        // ── Unlocks ──
        public List<string> unlockedClasses = new List<string>();
        public List<string> unlockedSkills = new List<string>();
        public List<string> unlockedItems = new List<string>();

        // ── Run Statistics ──
        public int totalRuns;
        public int totalDeaths;
        public int furthestFloor;
        public int totalEnemiesKilled;
        public int totalGoldEarned;
        public int totalSoulsEarned;
        public float totalPlayTimeSeconds;
        public int totalEventsTriggered;
        public int totalBossesKilled;

        // ── Settings ──
        public float masterVolume = 1f;
        public float sfxVolume = 1f;
        public float musicVolume = 1f;

        // ── Meta ──
        public string lastSaveTimestamp;

        /// <summary>
        /// Creates a fresh save with default values.
        /// </summary>
        public static SaveData CreateNew()
        {
            return new SaveData
            {
                unlockedClasses = new List<string> { "Warrior" },
                unlockedSkills = new List<string>(),
                unlockedItems = new List<string>()
            };
        }

        /// <summary>
        /// Serializes to JSON string.
        /// </summary>
        public string ToJson()
        {
            lastSaveTimestamp = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            return JsonUtility.ToJson(this, true);
        }

        /// <summary>
        /// Deserializes from JSON string.
        /// </summary>
        public static SaveData FromJson(string json)
        {
            if (string.IsNullOrEmpty(json))
            {
                return CreateNew();
            }

            SaveData data = JsonUtility.FromJson<SaveData>(json);

            // Ensure lists are initialized
            data.unlockedClasses ??= new List<string> { "Warrior" };
            data.unlockedSkills ??= new List<string>();
            data.unlockedItems ??= new List<string>();

            return data;
        }
    }

    /// <summary>
    /// Defines a permanent upgrade with its costs and effects per level.
    /// </summary>
    [System.Serializable]
    public class UpgradeDefinition
    {
        public string upgradeId;
        public string upgradeName;
        public string description;
        public int maxLevel;
        public int[] soulCostPerLevel;
        public int[] valuePerLevel;

        /// <summary>
        /// Returns the soul cost for the next upgrade level, or -1 if maxed.
        /// </summary>
        public int GetCostForNextLevel(int currentLevel)
        {
            if (currentLevel >= maxLevel) return -1;
            if (soulCostPerLevel == null || currentLevel >= soulCostPerLevel.Length) return -1;
            return soulCostPerLevel[currentLevel];
        }

        /// <summary>
        /// Returns the stat value at a given level.
        /// </summary>
        public int GetValueAtLevel(int level)
        {
            if (valuePerLevel == null || level < 0 || level >= valuePerLevel.Length) return 0;
            return valuePerLevel[level];
        }
    }

    /// <summary>
    /// Defines unlock conditions for classes, skills, or items.
    /// </summary>
    [System.Serializable]
    public class UnlockDefinition
    {
        public string unlockId;
        public string unlockName;
        public UnlockType unlockType;
        public UnlockCondition condition;
        public int conditionValue;

        public bool IsMet(SaveData saveData)
        {
            return condition switch
            {
                UnlockCondition.ReachFloor => saveData.furthestFloor >= conditionValue,
                UnlockCondition.KillEnemies => saveData.totalEnemiesKilled >= conditionValue,
                UnlockCondition.CompleteRuns => saveData.totalRuns >= conditionValue,
                UnlockCondition.EarnSouls => saveData.totalSoulsEarned >= conditionValue,
                UnlockCondition.KillBosses => saveData.totalBossesKilled >= conditionValue,
                _ => false
            };
        }
    }

    public enum UnlockType
    {
        Class,
        Skill,
        Item
    }

    public enum UnlockCondition
    {
        ReachFloor,
        KillEnemies,
        CompleteRuns,
        EarnSouls,
        KillBosses
    }
}
