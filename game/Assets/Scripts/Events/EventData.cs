using UnityEngine;

namespace AbyssWalker.Events
{
    /// <summary>
    /// Event types that can appear on the dungeon grid.
    /// </summary>
    public enum EventType
    {
        Treasure,
        Trap,
        Shop,
        NPC,
        Altar,
        Elite
    }

    /// <summary>
    /// Serializable data class representing a single random event placed on the grid.
    /// </summary>
    [System.Serializable]
    public class EventData
    {
        [Header("Identity")]
        public string eventId;
        public EventType type;
        public string eventName;

        [Header("Placement")]
        public Vector2Int position;
        public int floor;
        public bool isActive;
        public bool hasBeenTriggered;

        [Header("Description")]
        [TextArea(2, 5)]
        public string description;

        [Header("Effects")]
        public int goldAmount;
        public int damageAmount;
        public int healingAmount;
        public int statBoostAmount;
        public string rewardItemId;
        public string debuffType;

        [Header("Shop Specific")]
        public ShopItem[] shopItems;

        [Header("Altar Specific")]
        public BlessingOption[] blessingOptions;

        /// <summary>
        /// Creates a deep copy of this event data.
        /// </summary>
        public EventData Clone()
        {
            return new EventData
            {
                eventId = eventId,
                type = type,
                eventName = eventName,
                position = position,
                floor = floor,
                isActive = isActive,
                hasBeenTriggered = hasBeenTriggered,
                description = description,
                goldAmount = goldAmount,
                damageAmount = damageAmount,
                healingAmount = healingAmount,
                statBoostAmount = statBoostAmount,
                rewardItemId = rewardItemId,
                debuffType = debuffType,
                shopItems = shopItems != null ? (ShopItem[])shopItems.Clone() : null,
                blessingOptions = blessingOptions != null ? (BlessingOption[])blessingOptions.Clone() : null
            };
        }
    }

    /// <summary>
    /// Represents an item available in a shop event.
    /// </summary>
    [System.Serializable]
    public class ShopItem
    {
        public string itemId;
        public string itemName;
        public string description;
        public int price;
        public ShopItemType itemType;
        public int value;
        public bool isSold;
    }

    public enum ShopItemType
    {
        Weapon,
        Armor,
        Potion,
        Skill,
        Accessory
    }

    /// <summary>
    /// Represents a blessing choice at an altar.
    /// </summary>
    [System.Serializable]
    public class BlessingOption
    {
        public string blessingId;
        public string blessingName;
        [TextArea(1, 3)]
        public string description;
        public BlessingType blessingType;
        public int value;
    }

    public enum BlessingType
    {
        HPBoost,
        ATKBoost,
        DEFBoost,
        NewSkill,
        SpeedBoost,
        CritBoost
    }
}
