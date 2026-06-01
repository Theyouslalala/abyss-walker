using System.Collections.Generic;
using UnityEngine;

namespace AbyssWalker.Map
{
    /// <summary>
    /// Data class representing a single room in the dungeon. Mirrors the Python Room
    /// data structure used by the AI map generator.
    /// </summary>
    [System.Serializable]
    public class Room
    {
        #region Fields

        /// <summary>Unique identifier for this room.</summary>
        public int Id;

        /// <summary>
        /// Rectangular bounds of the room in grid coordinates.
        /// x, y = bottom-left corner; width, height = room dimensions.
        /// </summary>
        public RectInt Bounds;

        /// <summary>List of room IDs this room is connected to via corridors.</summary>
        public List<int> Connections;

        /// <summary>Whether this room contains the exit staircase.</summary>
        public bool HasExit;

        /// <summary>Whether this room is the player's starting room.</summary>
        public bool IsStartRoom;

        /// <summary>Grid position of the room's center.</summary>
        public Vector2Int Center => new Vector2Int(
            Bounds.x + Bounds.width / 2,
            Bounds.y + Bounds.height / 2
        );

        /// <summary>Number of connections (doors/corridors) this room has.</summary>
        public int ConnectionCount => Connections != null ? Connections.Count : 0;

        #endregion

        #region Constructors

        /// <summary>Default constructor for JSON deserialization.</summary>
        public Room()
        {
            Connections = new List<int>();
        }

        /// <summary>
        /// Creates a room with the specified bounds and ID.
        /// </summary>
        public Room(int id, RectInt bounds)
        {
            Id = id;
            Bounds = bounds;
            Connections = new List<int>();
            HasExit = false;
            IsStartRoom = false;
        }

        /// <summary>
        /// Creates a room from explicit position and size values.
        /// </summary>
        public Room(int id, int x, int y, int width, int height)
        {
            Id = id;
            Bounds = new RectInt(x, y, width, height);
            Connections = new List<int>();
            HasExit = false;
            IsStartRoom = false;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Adds a connection to another room by ID. Avoids duplicates.
        /// </summary>
        /// <param name="roomId">The ID of the room to connect to.</param>
        public void AddConnection(int roomId)
        {
            if (Connections == null)
                Connections = new List<int>();

            if (!Connections.Contains(roomId))
                Connections.Add(roomId);
        }

        /// <summary>
        /// Checks if the given grid position is inside this room's bounds.
        /// </summary>
        /// <param name="gridX">X grid coordinate.</param>
        /// <param name="gridY">Y grid coordinate.</param>
        /// <returns>True if the position is inside the room.</returns>
        public bool ContainsPoint(int gridX, int gridY)
        {
            return gridX >= Bounds.x
                && gridX < Bounds.x + Bounds.width
                && gridY >= Bounds.y
                && gridY < Bounds.y + Bounds.height;
        }

        /// <summary>
        /// Checks if the given grid position is inside this room's bounds.
        /// </summary>
        public bool ContainsPoint(Vector2Int point)
        {
            return ContainsPoint(point.x, point.y);
        }

        /// <summary>
        /// Returns a random walkable position inside the room.
        /// </summary>
        /// <param name="rng">Optional System.Random instance for reproducibility.</param>
        public Vector2Int GetRandomPosition(System.Random rng = null)
        {
            int x, y;
            if (rng != null)
            {
                x = Bounds.x + rng.Next(Bounds.width);
                y = Bounds.y + rng.Next(Bounds.height);
            }
            else
            {
                x = Random.Range(Bounds.x, Bounds.x + Bounds.width);
                y = Random.Range(Bounds.y, Bounds.y + Bounds.height);
            }
            return new Vector2Int(x, y);
        }

        /// <summary>
        /// Calculates the Chebyshev distance (max of dx, dy) from this room's center
        /// to another room's center.
        /// </summary>
        public int DistanceTo(Room other)
        {
            Vector2Int a = Center;
            Vector2Int b = other.Center;
            return Mathf.Max(Mathf.Abs(a.x - b.x), Mathf.Abs(a.y - b.y));
        }

        /// <summary>
        /// Returns the Manhattan distance from this room's center to another room's center.
        /// </summary>
        public int ManhattanDistanceTo(Room other)
        {
            Vector2Int a = Center;
            Vector2Int b = other.Center;
            return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
        }

        public override string ToString()
        {
            return $"Room({Id}, [{Bounds.x},{Bounds.y} {Bounds.width}x{Bounds.height}], conns={ConnectionCount})";
        }

        #endregion
    }

    /// <summary>
    /// JSON-serializable wrapper for a list of rooms, used for network transfer.
    /// </summary>
    [System.Serializable]
    public class RoomListData
    {
        public List<Room> Rooms;

        public RoomListData()
        {
            Rooms = new List<Room>();
        }

        public RoomListData(List<Room> rooms)
        {
            Rooms = rooms ?? new List<Room>();
        }
    }
}
