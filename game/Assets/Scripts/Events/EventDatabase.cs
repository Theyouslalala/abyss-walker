using UnityEngine;
using System.Collections.Generic;

namespace AbyssWalker.Events
{
    /// <summary>
    /// ScriptableObject holding all predefined event templates.
    /// Attach via Assets > Create > AbyssWalker > Event Database.
    /// </summary>
    [CreateAssetMenu(fileName = "EventDatabase", menuName = "AbyssWalker/Event Database")]
    public class EventDatabase : ScriptableObject
    {
        [Header("Treasure Events")]
        public EventData[] treasureEvents;

        [Header("Trap Events")]
        public EventData[] trapEvents;

        [Header("Shop Events")]
        public EventData[] shopEvents;

        [Header("NPC Events")]
        public EventData[] npcEvents;

        [Header("Altar Events")]
        public EventData[] altarEvents;

        [Header("Elite Events")]
        public EventData[] eliteEvents;

        [Header("Spawn Weights by Floor Range")]
        public FloorSpawnConfig[] floorSpawnConfigs;

        /// <summary>
        /// Returns a random event appropriate for the given floor and difficulty.
        /// </summary>
        public EventData GetRandomEvent(int floor, int difficulty)
        {
            EventType chosenType = ChooseEventType(floor, difficulty);
            EventData template = GetRandomTemplate(chosenType);

            if (template == null)
            {
                Debug.LogWarning($"No template found for event type {chosenType}");
                return null;
            }

            EventData evt = template.Clone();
            evt.floor = floor;
            evt.eventId = System.Guid.NewGuid().ToString();
            ScaleEventToDifficulty(evt, difficulty);
            return evt;
        }

        /// <summary>
        /// Picks an event type weighted by floor-based spawn configuration.
        /// </summary>
        private EventType ChooseEventType(int floor, int difficulty)
        {
            FloorSpawnConfig config = GetSpawnConfig(floor);

            float totalWeight = config.treasureWeight + config.trapWeight +
                                config.shopWeight + config.npcWeight +
                                config.altarWeight + config.eliteWeight;

            // Increase elite chance with difficulty
            float eliteBonus = difficulty * 0.05f;
            totalWeight += eliteBonus;

            float roll = Random.Range(0f, totalWeight);
            float cumulative = 0f;

            cumulative += config.treasureWeight;
            if (roll < cumulative) return EventType.Treasure;

            cumulative += config.trapWeight;
            if (roll < cumulative) return EventType.Trap;

            cumulative += config.shopWeight;
            if (roll < cumulative) return EventType.Shop;

            cumulative += config.npcWeight;
            if (roll < cumulative) return EventType.NPC;

            cumulative += config.altarWeight;
            if (roll < cumulative) return EventType.Altar;

            return EventType.Elite;
        }

        private FloorSpawnConfig GetSpawnConfig(int floor)
        {
            if (floorSpawnConfigs == null || floorSpawnConfigs.Length == 0)
            {
                return FloorSpawnConfig.Default;
            }

            for (int i = floorSpawnConfigs.Length - 1; i >= 0; i--)
            {
                if (floor >= floorSpawnConfigs[i].minFloor)
                {
                    return floorSpawnConfigs[i];
                }
            }

            return floorSpawnConfigs[0];
        }

        private EventData GetRandomTemplate(EventType type)
        {
            EventData[] pool = type switch
            {
                EventType.Treasure => treasureEvents,
                EventType.Trap => trapEvents,
                EventType.Shop => shopEvents,
                EventType.NPC => npcEvents,
                EventType.Altar => altarEvents,
                EventType.Elite => eliteEvents,
                _ => null
            };

            if (pool == null || pool.Length == 0) return null;
            return pool[Random.Range(0, pool.Length)];
        }

        /// <summary>
        /// Scales event rewards/difficulty based on the difficulty multiplier.
        /// </summary>
        private void ScaleEventToDifficulty(EventData evt, int difficulty)
        {
            float scale = 1f + (difficulty - 1) * 0.2f;

            evt.goldAmount = Mathf.RoundToInt(evt.goldAmount * scale);
            evt.damageAmount = Mathf.RoundToInt(evt.damageAmount * (1f + (difficulty - 1) * 0.15f));
            evt.healingAmount = Mathf.RoundToInt(evt.healingAmount * scale);

            if (evt.shopItems != null)
            {
                foreach (var item in evt.shopItems)
                {
                    item.price = Mathf.RoundToInt(item.price * scale);
                }
            }
        }
    }

    /// <summary>
    /// Defines spawn weight configuration for a range of floors.
    /// </summary>
    [System.Serializable]
    public class FloorSpawnConfig
    {
        public int minFloor = 1;
        public float treasureWeight = 3f;
        public float trapWeight = 3f;
        public float shopWeight = 1.5f;
        public float npcWeight = 1f;
        public float altarWeight = 0.8f;
        public float eliteWeight = 1f;

        public static FloorSpawnConfig Default => new FloorSpawnConfig();
    }
}
