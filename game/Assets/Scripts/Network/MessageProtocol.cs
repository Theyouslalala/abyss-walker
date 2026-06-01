using System;
using System.Collections.Generic;
using UnityEngine;

namespace AbyssWalker.Network
{
    #region Base Message

    /// <summary>
    /// Base class for all network messages. Contains the message type discriminator
    /// and a timestamp for logging.
    /// </summary>
    [System.Serializable]
    public class MessageBase
    {
        /// <summary>Message type identifier string (e.g. "game_state", "map_data", "ai_decision").</summary>
        public string Type;

        /// <summary>Unix timestamp (seconds) when the message was created.</summary>
        public long Timestamp;

        public MessageBase()
        {
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        }

        public MessageBase(string type) : this()
        {
            Type = type;
        }
    }

    #endregion

    #region Game State Message

    /// <summary>
    /// Sent from Unity to the Python AI server containing the current game state.
    /// Includes player stats, enemy positions, map data, and turn information.
    /// </summary>
    [System.Serializable]
    public class GameStateMessage : MessageBase
    {
        /// <summary>Current dungeon floor number.</summary>
        public int Floor;

        /// <summary>Current turn count.</summary>
        public int TurnCount;

        /// <summary>Current turn phase name (e.g. "player_turn", "enemy_turn").</summary>
        public string TurnPhase;

        /// <summary>Player data.</summary>
        public PlayerData Player;

        /// <summary>List of all active enemy data.</summary>
        public List<EnemyData> Enemies;

        /// <summary>Current event name (e.g. "combat", "random_event", "none").</summary>
        public string CurrentEvent;

        /// <summary>Map tile data as a 2D array (rows of columns).</summary>
        public int[][] MapTiles;

        /// <summary>List of rooms on the current floor.</summary>
        public List<RoomData> Rooms;

        public GameStateMessage() : base(MessageProtocol.TypeGameState) { }
    }

    /// <summary>
    /// Player state data for network serialization.
    /// </summary>
    [System.Serializable]
    public class PlayerData
    {
        public int Id;
        public int X;
        public int Y;
        public int Hp;
        public int MaxHp;
        public int Attack;
        public int Defense;
        public int Level;
        public int Exp;
        public List<string> Inventory;
        public List<string> Skills;
    }

    /// <summary>
    /// Enemy state data for network serialization.
    /// </summary>
    [System.Serializable]
    public class EnemyData
    {
        public int Id;
        public string Name;
        public int X;
        public int Y;
        public int Hp;
        public int MaxHp;
        public int Attack;
        public int Defense;
        public string Behavior;  // e.g. "aggressive", "patrol", "guard"
    }

    /// <summary>
    /// Room data for network serialization.
    /// </summary>
    [System.Serializable]
    public class RoomData
    {
        public int Id;
        public int X;
        public int Y;
        public int Width;
        public int Height;
        public List<int> Connections;
        public bool HasExit;
        public bool IsStartRoom;
    }

    #endregion

    #region AI Decision Message

    /// <summary>
    /// Sent from the Python AI server back to Unity containing AI decisions
    /// for enemy actions and any random events triggered.
    /// </summary>
    [System.Serializable]
    public class AIDecisionMessage : MessageBase
    {
        /// <summary>List of actions the AI has decided for each enemy.</summary>
        public List<EnemyAction> Actions;

        /// <summary>Optional random event triggered this turn.</summary>
        public RandomEventData RandomEvent;

        /// <summary>Optional dialogue or narrative text to display.</summary>
        public string NarrativeText;

        public AIDecisionMessage() : base(MessageProtocol.TypeAIDecision) { }
    }

    /// <summary>
    /// A single enemy action decided by the AI.
    /// </summary>
    [System.Serializable]
    public class EnemyAction
    {
        /// <summary>ID of the enemy performing the action.</summary>
        public int EnemyId;

        /// <summary>Action type: "move", "attack", "wait", "flee", "use_skill".</summary>
        public string Action;

        /// <summary>Target X coordinate (for move/attack).</summary>
        public int TargetX;

        /// <summary>Target Y coordinate (for move/attack).</summary>
        public int TargetY;

        /// <summary>Target entity ID (for attack/use_skill). -1 if no target.</summary>
        public int TargetId = -1;

        /// <summary>Skill name (for use_skill action).</summary>
        public string SkillName;
    }

    /// <summary>
    /// Random event data attached to an AI decision.
    /// </summary>
    [System.Serializable]
    public class RandomEventData
    {
        /// <summary>Event type identifier.</summary>
        public string EventType;

        /// <summary>Human-readable event description.</summary>
        public string Description;

        /// <summary>Stat changes applied by the event (stat name -> delta).</summary>
        public Dictionary<string, int> Effects;

        /// <summary>Whether the event is beneficial or harmful.</summary>
        public bool IsPositive;
    }

    #endregion

    #region Map Data Message

    /// <summary>
    /// Sent from the Python AI server to Unity when a new floor is generated.
    /// Contains the full tile grid and room layout.
    /// </summary>
    [System.Serializable]
    public class MapDataMessage : MessageBase
    {
        /// <summary>Floor number this map data is for.</summary>
        public int Floor;

        /// <summary>Map width in tiles.</summary>
        public int Width;

        /// <summary>Map height in tiles.</summary>
        public int Height;

        /// <summary>2D tile data: Tiles[y][x] gives the tile type at (x, y).</summary>
        public int[][] Tiles;

        /// <summary>Room definitions for this floor.</summary>
        public List<RoomData> Rooms;

        /// <summary>Player spawn position.</summary>
        public PositionData PlayerSpawn;

        /// <summary>Enemy spawn positions.</summary>
        public List<PositionData> EnemySpawns;

        /// <summary>Exit staircase position.</summary>
        public PositionData ExitPosition;

        public MapDataMessage() : base(MessageProtocol.TypeMapData) { }
    }

    /// <summary>
    /// Simple (X, Y) position pair for network serialization.
    /// </summary>
    [System.Serializable]
    public class PositionData
    {
        public int X;
        public int Y;

        public PositionData() { }

        public PositionData(int x, int y)
        {
            X = x;
            Y = y;
        }
    }

    #endregion

    #region Floor Request Message

    /// <summary>
    /// Sent from Unity to the AI server to request a new floor map.
    /// </summary>
    [System.Serializable]
    public class FloorRequestMessage : MessageBase
    {
        /// <summary>The floor number to generate.</summary>
        public int Floor;

        /// <summary>Optional seed for reproducible generation.</summary>
        public int Seed;

        public FloorRequestMessage() : base(MessageProtocol.TypeFloorRequest) { }
    }

    #endregion

    /// <summary>
    /// Provides JSON serialization and deserialization for all message types.
    /// Uses Unity's built-in JsonUtility. Also handles length-prefixed encoding/decoding.
    /// </summary>
    public static class MessageProtocol
    {
        #region Message Type Constants

        public const string TypeGameState   = "game_state";
        public const string TypeAIDecision  = "ai_decision";
        public const string TypeMapData     = "map_data";
        public const string TypeFloorRequest = "floor_request";

        #endregion

        #region Serialization

        /// <summary>
        /// Serializes a message object to a JSON string.
        /// </summary>
        /// <typeparam name="T">The message type (must extend MessageBase).</typeparam>
        /// <param name="message">The message to serialize.</param>
        /// <returns>A JSON string representation of the message.</returns>
        public static string Serialize<T>(T message) where T : MessageBase
        {
            if (message == null)
                throw new ArgumentNullException(nameof(message));

            return JsonUtility.ToJson(message);
        }

        /// <summary>
        /// Deserializes a JSON string into a message object.
        /// </summary>
        /// <typeparam name="T">The target message type.</typeparam>
        /// <param name="json">The JSON string to parse.</param>
        /// <returns>The deserialized message, or null if parsing fails.</returns>
        public static T Deserialize<T>(string json) where T : class
        {
            if (string.IsNullOrEmpty(json))
            {
                Debug.LogWarning("[MessageProtocol] Cannot deserialize null or empty JSON.");
                return null;
            }

            try
            {
                return JsonUtility.FromJson<T>(json);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[MessageProtocol] Deserialization failed for type {typeof(T).Name}: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Deserializes a JSON string into a message object (non-generic, for runtime type dispatch).
        /// </summary>
        /// <param name="json">The JSON string.</param>
        /// <param name="type">The target Type.</param>
        /// <returns>The deserialized object, or null.</returns>
        public static object Deserialize(string json, Type type)
        {
            if (string.IsNullOrEmpty(json))
                return null;

            try
            {
                return JsonUtility.FromJson(json, type);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[MessageProtocol] Deserialization failed for type {type.Name}: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Encodes a JSON string into a length-prefixed byte array.
        /// Format: [4-byte big-endian length][UTF-8 JSON payload]
        /// </summary>
        /// <param name="json">The JSON string to encode.</param>
        /// <returns>The encoded byte array.</returns>
        public static byte[] EncodeLengthPrefixed(string json)
        {
            if (json == null) json = string.Empty;

            byte[] payload = System.Text.Encoding.UTF8.GetBytes(json);
            byte[] lengthHeader = BitConverter.GetBytes(payload.Length);

            if (BitConverter.IsLittleEndian)
                Array.Reverse(lengthHeader);

            byte[] result = new byte[4 + payload.Length];
            Buffer.BlockCopy(lengthHeader, 0, result, 0, 4);
            Buffer.BlockCopy(payload, 0, result, 4, payload.Length);

            return result;
        }

        /// <summary>
        /// Decodes a length-prefixed byte array into a JSON string.
        /// </summary>
        /// <param name="data">The raw byte data including the 4-byte length header.</param>
        /// <param name="json">The decoded JSON string.</param>
        /// <param name="bytesConsumed">Total bytes consumed (header + payload).</param>
        /// <returns>True if decoding succeeded.</returns>
        public static bool DecodeLengthPrefixed(byte[] data, out string json, out int bytesConsumed)
        {
            json = null;
            bytesConsumed = 0;

            if (data == null || data.Length < 4)
                return false;

            // Read length
            byte[] lengthBytes = new byte[4];
            Buffer.BlockCopy(data, 0, lengthBytes, 0, 4);
            if (BitConverter.IsLittleEndian)
                Array.Reverse(lengthBytes);

            int messageLength = BitConverter.ToInt32(lengthBytes, 0);

            if (messageLength <= 0 || messageLength > 16 * 1024 * 1024)
            {
                Debug.LogError($"[MessageProtocol] Invalid decoded length: {messageLength}");
                return false;
            }

            if (data.Length < 4 + messageLength)
                return false; // Not enough data yet

            json = System.Text.Encoding.UTF8.GetString(data, 4, messageLength);
            bytesConsumed = 4 + messageLength;
            return true;
        }

        /// <summary>
        /// Determines the message type from a raw JSON string without fully deserializing it.
        /// Looks for the "Type" field value.
        /// </summary>
        /// <param name="json">The raw JSON string.</param>
        /// <returns>The message type string, or null if not found.</returns>
        public static string PeekMessageType(string json)
        {
            if (string.IsNullOrEmpty(json)) return null;

            try
            {
                MessageBase peek = JsonUtility.FromJson<MessageBase>(json);
                return peek?.Type;
            }
            catch
            {
                return null;
            }
        }

        #endregion
    }
}
