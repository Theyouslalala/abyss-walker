using UnityEngine;
using System;

namespace AbyssWalker.Events
{
    /// <summary>
    /// Handles treasure event logic: random loot generation and UI display.
    /// </summary>
    public class TreasureEvent : MonoBehaviour
    {
        [Header("Loot Tables")]
        [SerializeField] private LootTableEntry[] lootTable;

        [Header("UI Reference")]
        [SerializeField] private PopupUI lootPopup;

        public event Action<TreasureLoot> OnLootGranted;

        /// <summary>
        /// Triggers the treasure event, generates loot, and shows the popup.
        /// </summary>
        public void Trigger(EventData eventData)
        {
            if (eventData == null || eventData.hasBeenTriggered) return;

            eventData.hasBeenTriggered = true;
            eventData.isActive = false;

            TreasureLoot loot = GenerateLoot(eventData);

            // Apply loot to player
            ApplyLoot(loot);

            // Show UI
            ShowLootPopup(loot);

            OnLootGranted?.Invoke(loot);
        }

        private TreasureLoot GenerateLoot(EventData eventData)
        {
            TreasureLoot loot = new TreasureLoot();

            // Base gold from event data
            loot.gold = eventData.goldAmount > 0
                ? eventData.goldAmount
                : UnityEngine.Random.Range(10, 50);

            // Roll for additional items from loot table
            if (lootTable != null && lootTable.Length > 0)
            {
                foreach (var entry in lootTable)
                {
                    if (UnityEngine.Random.Range(0f, 1f) <= entry.dropChance)
                    {
                        switch (entry.itemCategory)
                        {
                            case LootCategory.Potion:
                                loot.potions++;
                                loot.potionName = entry.itemName;
                                break;
                            case LootCategory.Weapon:
                                loot.weaponId = entry.itemId;
                                loot.weaponName = entry.itemName;
                                loot.weaponAttack = entry.value;
                                break;
                            case LootCategory.Armor:
                                loot.armorId = entry.itemId;
                                loot.armorName = entry.itemName;
                                loot.armorDefense = entry.value;
                                break;
                        }
                    }
                }
            }

            // Floor scaling
            float floorScale = 1f + (eventData.floor - 1) * 0.1f;
            loot.gold = Mathf.RoundToInt(loot.gold * floorScale);

            return loot;
        }

        private void ApplyLoot(TreasureLoot loot)
        {
            // Notify player controller to add gold/items
            // This will be connected via event or direct reference
            PlayerController player = FindObjectOfType<PlayerController>();
            if (player != null)
            {
                player.AddGold(loot.gold);
                for (int i = 0; i < loot.potions; i++)
                {
                    player.AddPotion();
                }
            }
        }

        private void ShowLootPopup(TreasureLoot loot)
        {
            if (lootPopup == null)
            {
                lootPopup = FindObjectOfType<PopupUI>();
            }

            if (lootPopup != null)
            {
                string title = "Treasure Found!";
                string body = loot.GetDescription();
                lootPopup.Show(title, body, "OK", null);
            }
            else
            {
                Debug.Log($"[Treasure] {loot.GetDescription()}");
            }
        }
    }

    /// <summary>
    /// Data class representing loot received from a treasure event.
    /// </summary>
    [System.Serializable]
    public class TreasureLoot
    {
        public int gold;
        public int potions;
        public string potionName;
        public string weaponId;
        public string weaponName;
        public int weaponAttack;
        public string armorId;
        public string armorName;
        public int armorDefense;

        public bool HasWeapon => !string.IsNullOrEmpty(weaponId);
        public bool HasArmor => !string.IsNullOrEmpty(armorId);

        public string GetDescription()
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"Gold: +{gold}");

            if (potions > 0)
                sb.AppendLine($"{potionName} x{potions}");
            if (HasWeapon)
                sb.AppendLine($"{weaponName} (ATK +{weaponAttack})");
            if (HasArmor)
                sb.AppendLine($"{armorName} (DEF +{armorDefense})");

            return sb.ToString();
        }
    }

    [System.Serializable]
    public class LootTableEntry
    {
        public string itemId;
        public string itemName;
        public LootCategory itemCategory;
        [Range(0f, 1f)]
        public float dropChance = 0.5f;
        public int value;
    }

    public enum LootCategory
    {
        Potion,
        Weapon,
        Armor
    }
}
