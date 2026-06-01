using UnityEngine;
using System;

namespace AbyssWalker.Meta
{
    /// <summary>
    /// Manages permanent upgrades across runs using "Abyss Souls" currency.
    /// Handles save/load of all persistent progress.
    /// </summary>
    public class ProgressManager : MonoBehaviour
    {
        public static ProgressManager Instance { get; private set; }

        [Header("Upgrade Definitions")]
        [SerializeField] private UpgradeDefinition[] upgradeDefinitions;

        [Header("Save Settings")]
        [SerializeField] private string saveKey = "AbyssWalker_SaveData";
        [SerializeField] private bool useFileSave = false;
        [SerializeField] private string saveFileName = "abyss_walker_save.json";

        public SaveData SaveData { get; private set; }

        public event Action<int> OnSoulsChanged;
        public event Action<string, int> OnUpgradePurchased;
        public event Action OnProgressSaved;
        public event Action OnProgressLoaded;

        // ── Upgrade Stat Accessors ──
        public int MaxHPBonus => GetUpgradeValue("hp_upgrade", SaveData.hpUpgradeLevel);
        public int ATKBonus => GetUpgradeValue("atk_upgrade", SaveData.atkUpgradeLevel);
        public int DEFBonus => GetUpgradeValue("def_upgrade", SaveData.defUpgradeLevel);
        public int SpeedBonus => GetUpgradeValue("speed_upgrade", SaveData.speedUpgradeLevel);
        public int CritBonus => GetUpgradeValue("crit_upgrade", SaveData.critUpgradeLevel);
        public int PotionHealBonus => GetUpgradeValue("potion_heal_upgrade", SaveData.potionHealUpgradeLevel);

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            LoadProgress();
        }

        // ── Soul Management ──

        public int GetSouls() => SaveData.abyssSouls;

        public void AddSouls(int amount)
        {
            if (amount <= 0) return;
            SaveData.abyssSouls += amount;
            SaveData.totalSoulsEarned += amount;
            OnSoulsChanged?.Invoke(SaveData.abyssSouls);
        }

        public bool SpendSouls(int amount)
        {
            if (amount <= 0 || SaveData.abyssSouls < amount) return false;
            SaveData.abyssSouls -= amount;
            OnSoulsChanged?.Invoke(SaveData.abyssSouls);
            return true;
        }

        // ── Upgrade Purchasing ──

        /// <summary>
        /// Attempts to purchase the named upgrade. Returns true if successful.
        /// </summary>
        public bool PurchaseUpgrade(string upgradeId)
        {
            int currentLevel = GetCurrentUpgradeLevel(upgradeId);
            int cost = GetUpgradeCost(upgradeId, currentLevel);

            if (cost < 0)
            {
                Debug.Log($"Upgrade {upgradeId} is already at max level.");
                return false;
            }

            if (!SpendSouls(cost))
            {
                Debug.Log("Not enough Abyss Souls.");
                return false;
            }

            SetUpgradeLevel(upgradeId, currentLevel + 1);
            OnUpgradePurchased?.Invoke(upgradeId, currentLevel + 1);
            SaveProgress();

            return true;
        }

        public int GetCurrentUpgradeLevel(string upgradeId)
        {
            return upgradeId switch
            {
                "hp_upgrade" => SaveData.hpUpgradeLevel,
                "atk_upgrade" => SaveData.atkUpgradeLevel,
                "def_upgrade" => SaveData.defUpgradeLevel,
                "speed_upgrade" => SaveData.speedUpgradeLevel,
                "crit_upgrade" => SaveData.critUpgradeLevel,
                "potion_heal_upgrade" => SaveData.potionHealUpgradeLevel,
                _ => 0
            };
        }

        public int GetUpgradeCost(string upgradeId, int currentLevel)
        {
            UpgradeDefinition def = FindDefinition(upgradeId);
            return def?.GetCostForNextLevel(currentLevel) ?? -1;
        }

        public int GetUpgradeMaxLevel(string upgradeId)
        {
            UpgradeDefinition def = FindDefinition(upgradeId);
            return def?.maxLevel ?? 0;
        }

        public string GetUpgradeName(string upgradeId)
        {
            UpgradeDefinition def = FindDefinition(upgradeId);
            return def?.upgradeName ?? upgradeId;
        }

        public string GetUpgradeDescription(string upgradeId)
        {
            UpgradeDefinition def = FindDefinition(upgradeId);
            return def?.description ?? "";
        }

        // ── Save / Load ──

        public void SaveProgress()
        {
            string json = SaveData.ToJson();

            if (useFileSave)
            {
                string path = GetSavePath();
                System.IO.File.WriteAllText(path, json);
            }
            else
            {
                PlayerPrefs.SetString(saveKey, json);
                PlayerPrefs.Save();
            }

            OnProgressSaved?.Invoke();
        }

        public void LoadProgress()
        {
            string json = null;

            if (useFileSave)
            {
                string path = GetSavePath();
                if (System.IO.File.Exists(path))
                {
                    json = System.IO.File.ReadAllText(path);
                }
            }
            else
            {
                if (PlayerPrefs.HasKey(saveKey))
                {
                    json = PlayerPrefs.GetString(saveKey);
                }
            }

            SaveData = SaveData.FromJson(json);
            OnProgressLoaded?.Invoke();
        }

        public void DeleteSave()
        {
            if (useFileSave)
            {
                string path = GetSavePath();
                if (System.IO.File.Exists(path))
                {
                    System.IO.File.Delete(path);
                }
            }
            else
            {
                PlayerPrefs.DeleteKey(saveKey);
            }

            SaveData = SaveData.CreateNew();
            SaveProgress();
        }

        public bool HasSaveData()
        {
            if (useFileSave)
            {
                return System.IO.File.Exists(GetSavePath());
            }
            return PlayerPrefs.HasKey(saveKey);
        }

        // ── Private Helpers ──

        private int GetUpgradeValue(string upgradeId, int level)
        {
            UpgradeDefinition def = FindDefinition(upgradeId);
            return def?.GetValueAtLevel(level) ?? 0;
        }

        private void SetUpgradeLevel(string upgradeId, int level)
        {
            switch (upgradeId)
            {
                case "hp_upgrade": SaveData.hpUpgradeLevel = level; break;
                case "atk_upgrade": SaveData.atkUpgradeLevel = level; break;
                case "def_upgrade": SaveData.defUpgradeLevel = level; break;
                case "speed_upgrade": SaveData.speedUpgradeLevel = level; break;
                case "crit_upgrade": SaveData.critUpgradeLevel = level; break;
                case "potion_heal_upgrade": SaveData.potionHealUpgradeLevel = level; break;
            }
        }

        private UpgradeDefinition FindDefinition(string upgradeId)
        {
            if (upgradeDefinitions == null) return null;
            foreach (var def in upgradeDefinitions)
            {
                if (def.upgradeId == upgradeId) return def;
            }
            return null;
        }

        private string GetSavePath()
        {
            return System.IO.Path.Combine(Application.persistentDataPath, saveFileName);
        }
    }
}
