using UnityEngine;
using System.Collections.Generic;

namespace AbyssWalker.Events
{
    /// <summary>
    /// Receives event data (from Python AI server or local generation) and places
    /// events on the dungeon grid. Manages event activation as the player explores.
    /// </summary>
    public class EventPlacer : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private EventDatabase eventDatabase;
        [SerializeField] private Transform eventParent;

        [Header("Event Components (prefabs or scene references)")]
        [SerializeField] private TreasureEvent treasureEvent;
        [SerializeField] private TrapEvent trapEvent;
        [SerializeField] private ShopEvent shopEvent;
        [SerializeField] private AltarEvent altarEvent;

        [Header("Configuration")]
        [SerializeField] private int minEventsPerFloor = 3;
        [SerializeField] private int maxEventsPerFloor = 7;

        private Dictionary<Vector2Int, EventData> placedEvents = new Dictionary<Vector2Int, EventData>();
        private List<EventData> activeEvents = new List<EventData>();

        public event Action<EventData> OnEventPlaced;
        public event Action<EventData, Vector2Int> OnEventTriggered;

        /// <summary>
        /// Places events for a new floor. Called by the floor generator.
        /// </summary>
        public void PlaceEventsForFloor(int floor, int difficulty, List<Vector2Int> availableTiles)
        {
            ClearEvents();

            if (availableTiles == null || availableTiles.Count == 0) return;

            int eventCount = UnityEngine.Random.Range(minEventsPerFloor, maxEventsPerFloor + 1);
            eventCount = Mathf.Min(eventCount, availableTiles.Count);

            // Shuffle available tiles
            List<Vector2Int> shuffled = new List<Vector2Int>(availableTiles);
            for (int i = 0; i < shuffled.Count; i++)
            {
                int rand = UnityEngine.Random.Range(i, shuffled.Count);
                (shuffled[i], shuffled[rand]) = (shuffled[rand], shuffled[i]);
            }

            // Place events
            for (int i = 0; i < eventCount; i++)
            {
                Vector2Int pos = shuffled[i];
                EventData evt = eventDatabase.GetRandomEvent(floor, difficulty);

                if (evt != null)
                {
                    evt.position = pos;
                    evt.isActive = true;
                    placedEvents[pos] = evt;
                    activeEvents.Add(evt);

                    OnEventPlaced?.Invoke(evt);
                }
            }
        }

        /// <summary>
        /// Receives event data from the Python AI server and places it on the grid.
        /// This is the integration point for server-driven event placement.
        /// </summary>
        public void PlaceEventsFromServer(string jsonData)
        {
            ServerEventPayload payload = JsonUtility.FromJson<ServerEventPayload>(jsonData);
            if (payload == null || payload.events == null) return;

            foreach (var serverEvent in payload.events)
            {
                EventData evt = new EventData
                {
                    eventId = serverEvent.eventId,
                    type = serverEvent.type,
                    eventName = serverEvent.eventName,
                    description = serverEvent.description,
                    position = new Vector2Int(serverEvent.x, serverEvent.y),
                    floor = serverEvent.floor,
                    isActive = true,
                    goldAmount = serverEvent.goldAmount,
                    damageAmount = serverEvent.damageAmount,
                    healingAmount = serverEvent.healingAmount,
                    debuffType = serverEvent.debuffType
                };

                placedEvents[evt.position] = evt;
                activeEvents.Add(evt);

                OnEventPlaced?.Invoke(evt);
            }
        }

        /// <summary>
        /// Checks if there is an event at the given position and triggers it.
        /// </summary>
        public bool TryTriggerEvent(Vector2Int position)
        {
            if (!placedEvents.TryGetValue(position, out EventData evt)) return false;
            if (!evt.isActive || evt.hasBeenTriggered) return false;

            TriggerEvent(evt, position);
            return true;
        }

        /// <summary>
        /// Returns the event at a given position, or null if none exists.
        /// </summary>
        public EventData GetEventAt(Vector2Int position)
        {
            placedEvents.TryGetValue(position, out EventData evt);
            return evt;
        }

        /// <summary>
        /// Returns all currently active (untriggered) events.
        /// </summary>
        public List<EventData> GetActiveEvents()
        {
            return activeEvents.FindAll(e => e.isActive && !e.hasBeenTriggered);
        }

        private void TriggerEvent(EventData evt, Vector2Int position)
        {
            switch (evt.type)
            {
                case EventType.Treasure:
                    treasureEvent?.Trigger(evt);
                    break;
                case EventType.Trap:
                    PlayerController player = FindObjectOfType<PlayerController>();
                    int perception = player != null ? player.GetPerception() : 0;
                    if (!trapEvent.CanDetectTrap(evt, perception))
                    {
                        trapEvent.TriggerTrap(evt, player != null ? player.GetDEF() : 0);
                    }
                    break;
                case EventType.Shop:
                    shopEvent?.OpenShop(evt);
                    break;
                case EventType.Altar:
                    altarEvent?.OpenAltar(evt);
                    break;
                case EventType.NPC:
                    // NPC events handled by dialogue system
                    break;
                case EventType.Elite:
                    // Elite encounters trigger combat
                    break;
            }

            OnEventTriggered?.Invoke(evt, position);
        }

        private void ClearEvents()
        {
            placedEvents.Clear();
            activeEvents.Clear();
        }
    }

    /// <summary>
    /// Payload format for server-driven event placement.
    /// </summary>
    [System.Serializable]
    public class ServerEventPayload
    {
        public ServerEvent[] events;
    }

    [System.Serializable]
    public class ServerEvent
    {
        public string eventId;
        public EventType type;
        public string eventName;
        public string description;
        public int x;
        public int y;
        public int floor;
        public int goldAmount;
        public int damageAmount;
        public int healingAmount;
        public string debuffType;
    }
}
