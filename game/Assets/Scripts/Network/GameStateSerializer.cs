using System;
using System.Collections.Generic;
using UnityEngine;
using AbyssWalker.Core;
using AbyssWalker.Map;

namespace AbyssWalker.Network
{
    /// <summary>
    /// Handles serialization of Unity game state into network messages for the Python AI server,
    /// and deserialization of AI server responses back into usable game data.
    /// </summary>
    public class GameStateSerializer
    {
        #region Public Methods - Serialization (Unity -> Python)

        /// <summary>
        /// Creates a complete GameStateMessage from the current game state.
        /// This is sent to the Python AI server each turn so it can make decisions.
        /// </summary>
        /// <param name="player">The player GameObject (must have a Player component or be tagged "Player").</param>
        /// <param name="enemies">List of active enemy GameObjects.</param>
        /// <returns>A GameStateMessage ready to send, or null if critical data is missing.</returns>
        public GameStateMessage CreateGameStateMessage(GameObject player, List<GameObject> enemies)
        {
            if (player == null)
            {
                Debug.LogError("[GameStateSerializer] Player object is null.");
                return null;
            }

            GameStateMessage msg = new GameStateMessage();

            // Floor and turn info
            if (GameManager.Instance != null)
            {
                msg.Floor = GameManager.Instance.CurrentFloor;
                msg.TurnCount = GameManager.Instance.TurnManager != null
                    ? GameManager.Instance.TurnManager.TurnCount : 0;
                msg.TurnPhase = GameManager.Instance.TurnManager != null
                    ? GameManager.Instance.TurnManager.CurrentPhase.ToString().ToLower() : "unknown";
            }

            // Player data
            msg.Player = SerializePlayer(player);

            // Enemy data
            msg.Enemies = new List<EnemyData>();
            if (enemies != null)
            {
                foreach (var enemy in enemies)
                {
                    if (enemy != null)
                    {
                        EnemyData ed = SerializeEnemy(enemy);
                        if (ed != null)
                            msg.Enemies.Add(ed);
                    }
                }
            }

            // Map tiles
            if (GameManager.Instance != null && GameManager.Instance.GridManager != null)
            {
                msg.MapTiles = SerializeMapTiles(GameManager.Instance.GridManager);
            }

            // Rooms
            msg.Rooms = new List<RoomData>();
            // Room data would be populated from a RoomManager if available

            msg.CurrentEvent = "none";

            return msg;
        }

        /// <summary>
        /// Serializes the current game state into a JSON string ready to send over the network.
        /// </summary>
        /// <returns>JSON string of the game state, or null on failure.</returns>
        public string SerializeGameState(GameObject player, List<GameObject> enemies)
        {
            GameStateMessage msg = CreateGameStateMessage(player, enemies);
            if (msg == null) return null;
            return MessageProtocol.Serialize(msg);
        }

        /// <summary>
        /// Creates a floor request message as a JSON string.
        /// </summary>
        /// <param name="floorNumber">The floor to generate.</param>
        /// <param name="seed">Optional generation seed for reproducibility.</param>
        /// <returns>JSON string of the floor request.</returns>
        public string CreateFloorRequestMessage(int floorNumber, int seed = 0)
        {
            FloorRequestMessage msg = new FloorRequestMessage
            {
                Floor = floorNumber,
                Seed = seed == 0 ? UnityEngine.Random.Range(0, int.MaxValue) : seed
            };
            return MessageProtocol.Serialize(msg);
        }

        #endregion

        #region Public Methods - Deserialization (Python -> Unity)

        /// <summary>
        /// Deserializes a MapDataMessage JSON string into usable map data.
        /// </summary>
        /// <param name="json">The raw JSON string from the server.</param>
        /// <returns>The parsed MapDataMessage, or null on failure.</returns>
        public MapDataMessage DeserializeMapData(string json)
        {
            return MessageProtocol.Deserialize<MapDataMessage>(json);
        }

        /// <summary>
        /// Deserializes an AIDecisionMessage JSON string into usable AI decision data.
        /// </summary>
        /// <param name="json">The raw JSON string from the server.</param>
        /// <returns>The parsed AIDecisionMessage, or null on failure.</returns>
        public AIDecisionMessage DeserializeAIDecision(string json)
        {
            return MessageProtocol.Deserialize<AIDecisionMessage>(json);
        }

        /// <summary>
        /// Converts MapDataMessage room data into a list of Room objects.
        /// </summary>
        /// <param name="mapData">The map data message.</param>
        /// <returns>List of Room objects.</returns>
        public List<Room> ConvertToRooms(MapDataMessage mapData)
        {
            List<Room> rooms = new List<Room>();
            if (mapData?.Rooms == null) return rooms;

            foreach (RoomData rd in mapData.Rooms)
            {
                Room room = new Room(rd.Id, rd.X, rd.Y, rd.Width, rd.Height);
                room.HasExit = rd.HasExit;
                room.IsStartRoom = rd.IsStartRoom;
                room.Connections = rd.Connections != null
                    ? new List<int>(rd.Connections)
                    : new List<int>();
                rooms.Add(room);
            }

            return rooms;
        }

        /// <summary>
        /// Applies enemy actions from an AIDecisionMessage to the enemy GameObjects.
        /// Returns a list of actions that need to be animated/executed.
        /// </summary>
        /// <param name="decision">The AI decision message.</param>
        /// <param name="enemiesById">Dictionary mapping enemy IDs to their GameObjects.</param>
        /// <returns>List of (enemy GO, action) pairs for the turn manager to execute.</returns>
        public List<(GameObject enemy, EnemyAction action)> ApplyAIDecisions(
            AIDecisionMessage decision,
            Dictionary<int, GameObject> enemiesById)
        {
            var results = new List<(GameObject, EnemyAction)>();

            if (decision?.Actions == null || enemiesById == null)
                return results;

            foreach (EnemyAction action in decision.Actions)
            {
                if (enemiesById.TryGetValue(action.EnemyId, out GameObject enemyGO))
                {
                    results.Add((enemyGO, action));
                }
                else
                {
                    Debug.LogWarning($"[GameStateSerializer] Enemy ID {action.EnemyId} not found in scene.");
                }
            }

            return results;
        }

        #endregion

        #region Private Methods - Serialization Helpers

        /// <summary>
        /// Serializes a player GameObject into a PlayerData object.
        /// Expects the player to have specific components or uses Transform position as fallback.
        /// </summary>
        private PlayerData SerializePlayer(GameObject playerGO)
        {
            PlayerData data = new PlayerData();

            // Position from transform
            Vector2Int gridPos = Vector2Int.zero;
            if (GameManager.Instance?.GridManager != null)
            {
                gridPos = GameManager.Instance.GridManager.WorldToGrid(playerGO.transform.position);
            }
            data.X = gridPos.x;
            data.Y = gridPos.y;

            // Read stats from Player component directly
            var playerComponent = playerGO.GetComponent<AbyssWalker.Entity.Player>();
            if (playerComponent != null)
            {
                data.Hp = playerComponent.GetCurrentHP();
                data.MaxHp = playerComponent.GetMaxHP();
                data.Attack = playerComponent.Stats.attack;
                data.Defense = playerComponent.Stats.defense;
                data.Level = playerComponent.Stats.level;
                data.Exp = playerComponent.Stats.exp;
                data.Id = 0;
            }
            else
            {
                data.Id = 0;
                data.Hp = 100;
                data.MaxHp = 100;
                data.Attack = 10;
                data.Defense = 5;
                data.Level = 1;
                data.Exp = 0;
            }

            data.Inventory = new List<string>();
            data.Skills = new List<string>();

            return data;
        }

        /// <summary>
        /// Serializes an enemy GameObject into an EnemyData object.
        /// </summary>
        private EnemyData SerializeEnemy(GameObject enemyGO)
        {
            EnemyData data = new EnemyData();

            Vector2Int gridPos = Vector2Int.zero;
            if (GameManager.Instance?.GridManager != null)
            {
                gridPos = GameManager.Instance.GridManager.WorldToGrid(enemyGO.transform.position);
            }
            data.X = gridPos.x;
            data.Y = gridPos.y;

            // Read from Enemy component directly
            var enemyComponent = enemyGO.GetComponent<AbyssWalker.Entity.Enemy>();
            if (enemyComponent != null)
            {
                data.Id = enemyComponent.EnemyId;
                data.Name = enemyGO.name;
                data.Hp = enemyComponent.Stats.hp;
                data.MaxHp = enemyComponent.Stats.maxHp;
                data.Attack = enemyComponent.Stats.attack;
                data.Defense = enemyComponent.Stats.defense;
                data.Behavior = enemyComponent.Type.ToString().ToLower();
            }
            else
            {
                data.Id = enemyGO.GetInstanceID();
                data.Name = enemyGO.name;
                data.Hp = 30;
                data.MaxHp = 30;
                data.Attack = 5;
                data.Defense = 3;
                data.Behavior = "aggressive";
            }

            return data;
        }

        /// <summary>
        /// Serializes the GridManager's tile grid into a 2D int array (jagged).
        /// Format: Tiles[y][x] matches the Python convention.
        /// </summary>
        private int[][] SerializeMapTiles(GridManager gridManager)
        {
            if (gridManager?.TileGrid == null) return null;

            int width = gridManager.Width;
            int height = gridManager.Height;

            int[][] tiles = new int[height][];
            for (int y = 0; y < height; y++)
            {
                tiles[y] = new int[width];
                for (int x = 0; x < width; x++)
                {
                    tiles[y][x] = gridManager.GetTileAt(x, y);
                }
            }

            return tiles;
        }

        /// <summary>
        /// Converts a Room list into serializable RoomData objects.
        /// </summary>
        private List<RoomData> SerializeRooms(List<Room> rooms)
        {
            List<RoomData> dataList = new List<RoomData>();
            if (rooms == null) return dataList;

            foreach (Room room in rooms)
            {
                RoomData rd = new RoomData
                {
                    Id = room.Id,
                    X = room.Bounds.x,
                    Y = room.Bounds.y,
                    Width = room.Bounds.width,
                    Height = room.Bounds.height,
                    HasExit = room.HasExit,
                    IsStartRoom = room.IsStartRoom,
                    Connections = room.Connections != null
                        ? new List<int>(room.Connections)
                        : new List<int>()
                };
                dataList.Add(rd);
            }

            return dataList;
        }

        #endregion

        #region Private Methods - Reflection Helpers

        /// <summary>
        /// Safely reads an int field from a component via reflection.
        /// Returns the default value if the field is not found.
        /// </summary>
        private static int GetIntField(Component component, string fieldName, int defaultValue)
        {
            if (component == null) return defaultValue;

            var type = component.GetType();
            var field = type.GetField(fieldName,
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);

            if (field != null && field.FieldType == typeof(int))
            {
                return (int)field.GetValue(component);
            }

            // Try property
            var prop = type.GetProperty(fieldName,
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);

            if (prop != null && prop.PropertyType == typeof(int) && prop.CanRead)
            {
                return (int)prop.GetValue(component);
            }

            return defaultValue;
        }

        /// <summary>
        /// Safely reads a string field from a component via reflection.
        /// Returns the default value if the field is not found.
        /// </summary>
        private static string GetStringField(Component component, string fieldName, string defaultValue)
        {
            if (component == null) return defaultValue;

            var type = component.GetType();
            var field = type.GetField(fieldName,
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);

            if (field != null && field.FieldType == typeof(string))
            {
                return (string)field.GetValue(component) ?? defaultValue;
            }

            var prop = type.GetProperty(fieldName,
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);

            if (prop != null && prop.PropertyType == typeof(string) && prop.CanRead)
            {
                return (string)prop.GetValue(component) ?? defaultValue;
            }

            return defaultValue;
        }

        #endregion
    }
}
