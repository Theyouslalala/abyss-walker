using UnityEngine;
using System;
using System.Collections.Generic;

namespace AbyssWalker.Meta
{
    /// <summary>
    /// Manages unlockable content: classes, skills, and items.
    /// Checks unlock conditions against save data after each run.
    /// </summary>
    public class UnlockSystem : MonoBehaviour
    {
        [Header("Unlock Definitions")]
        [SerializeField] private UnlockDefinition[] unlockDefinitions;

        [Header("Class Definitions")]
        [SerializeField] private ClassDefinition[] classDefinitions;

        public event Action<UnlockDefinition> OnItemUnlocked;
        public event Action<ClassDefinition> OnClassUnlocked;

        private ProgressManager progressManager;

        private void Awake()
        {
            progressManager = ProgressManager.Instance;
        }

        /// <summary>
        /// Checks all unlock conditions against current save data.
        /// Call this at the end of each run.
        /// </summary>
        public List<UnlockDefinition> CheckUnlocks()
        {
            List<UnlockDefinition> newlyUnlocked = new List<UnlockDefinition>();

            if (progressManager == null || unlockDefinitions == null) return newlyUnlocked;

            SaveData save = progressManager.SaveData;

            foreach (var def in unlockDefinitions)
            {
                // Already unlocked?
                if (IsUnlocked(def.unlockId)) continue;

                // Check condition
                if (def.IsMet(save))
                {
                    GrantUnlock(def);
                    newlyUnlocked.Add(def);
                }
            }

            return newlyUnlocked;
        }

        /// <summary>
        /// Checks if a specific item is unlocked.
        /// </summary>
        public bool IsUnlocked(string unlockId)
        {
            if (progressManager == null) return false;
            SaveData save = progressManager.SaveData;

            return unlockId switch
            {
                // Classes
                "class_warrior" => save.unlockedClasses.Contains("Warrior"),
                "class_mage" => save.unlockedClasses.Contains("Mage"),
                "class_ranger" => save.unlockedClasses.Contains("Ranger"),
                "class_assassin" => save.unlockedClasses.Contains("Assassin"),
                // Skills and items check the respective lists
                _ => save.unlockedSkills.Contains(unlockId) || save.unlockedItems.Contains(unlockId)
            };
        }

        /// <summary>
        /// Returns all unlocked class names.
        /// </summary>
        public List<string> GetUnlockedClasses()
        {
            return progressManager?.SaveData?.unlockedClasses ?? new List<string> { "Warrior" };
        }

        /// <summary>
        /// Returns the class definition for a given class name.
        /// </summary>
        public ClassDefinition GetClassDefinition(string className)
        {
            if (classDefinitions == null) return null;
            foreach (var def in classDefinitions)
            {
                if (def.className == className) return def;
            }
            return null;
        }

        /// <summary>
        /// Returns all class definitions that are currently unlocked.
        /// </summary>
        public List<ClassDefinition> GetAvailableClasses()
        {
            List<ClassDefinition> available = new List<ClassDefinition>();
            if (classDefinitions == null) return available;

            foreach (var def in classDefinitions)
            {
                if (GetUnlockedClasses().Contains(def.className))
                {
                    available.Add(def);
                }
            }

            return available;
        }

        /// <summary>
        /// Force-unlock a class (e.g., for debugging or special rewards).
        /// </summary>
        public void ForceUnlockClass(string className)
        {
            if (progressManager == null) return;
            SaveData save = progressManager.SaveData;

            if (!save.unlockedClasses.Contains(className))
            {
                save.unlockedClasses.Add(className);
                progressManager.SaveProgress();

                ClassDefinition def = GetClassDefinition(className);
                if (def != null)
                {
                    OnClassUnlocked?.Invoke(def);
                }
            }
        }

        private void GrantUnlock(UnlockDefinition def)
        {
            if (progressManager == null) return;
            SaveData save = progressManager.SaveData;

            switch (def.unlockType)
            {
                case UnlockType.Class:
                    string className = def.unlockId.Replace("class_", "");
                    className = char.ToUpper(className[0]) + className.Substring(1);
                    if (!save.unlockedClasses.Contains(className))
                    {
                        save.unlockedClasses.Add(className);
                        ClassDefinition classDef = GetClassDefinition(className);
                        if (classDef != null) OnClassUnlocked?.Invoke(classDef);
                    }
                    break;

                case UnlockType.Skill:
                    if (!save.unlockedSkills.Contains(def.unlockId))
                    {
                        save.unlockedSkills.Add(def.unlockId);
                    }
                    break;

                case UnlockType.Item:
                    if (!save.unlockedItems.Contains(def.unlockId))
                    {
                        save.unlockedItems.Add(def.unlockId);
                    }
                    break;
            }

            OnItemUnlocked?.Invoke(def);
            progressManager.SaveProgress();

            Debug.Log($"[UnlockSystem] Unlocked: {def.unlockName} ({def.unlockType})");
        }
    }

    /// <summary>
    /// Defines a playable class with base stats and starting equipment.
    /// </summary>
    [System.Serializable]
    public class ClassDefinition
    {
        public string className;
        [TextArea(1, 3)]
        public string description;

        [Header("Base Stats")]
        public int baseHP = 100;
        public int baseATK = 10;
        public int baseDEF = 5;
        public int baseSpeed = 5;
        public int baseCrit = 5;
        public int basePerception = 3;

        [Header("Starting Equipment")]
        public string startWeapon;
        public string startArmor;
        public int startGold = 0;
        public int startPotions = 1;

        [Header("Starting Skills")]
        public string[] startSkills;

        [Header("UI")]
        public Sprite classIcon;
    }
}
