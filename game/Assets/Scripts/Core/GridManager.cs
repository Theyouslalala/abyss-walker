using System;
using System.Collections.Generic;
using UnityEngine;

namespace AbyssWalker.Core
{
    /// <summary>
    /// Generic 2D grid data structure that stores a value per cell.
    /// </summary>
    /// <typeparam name="T">The type stored in each cell.</typeparam>
    [System.Serializable]
    public class Grid<T>
    {
        /// <summary>Width of the grid in cells.</summary>
        public int Width { get; }

        /// <summary>Height of the grid in cells.</summary>
        public int Height { get; }

        private readonly T[] _data;

        /// <summary>
        /// Creates a new grid with the given dimensions, optionally initializing each cell
        /// via the provided factory function.
        /// </summary>
        public Grid(int width, int height, Func<int, int, T> createDefault = null)
        {
            Width = width;
            Height = height;
            _data = new T[width * height];

            if (createDefault != null)
            {
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        _data[y * width + x] = createDefault(x, y);
                    }
                }
            }
        }

        /// <summary>Whether the given grid coordinates are within bounds.</summary>
        public bool IsInBounds(int x, int y)
        {
            return x >= 0 && x < Width && y >= 0 && y < Height;
        }

        /// <summary>Gets the value at (x, y). Throws if out of bounds.</summary>
        public T Get(int x, int y)
        {
            if (!IsInBounds(x, y))
                throw new ArgumentOutOfRangeException($"({x},{y}) is out of bounds for grid [{Width}x{Height}]");
            return _data[y * Width + x];
        }

        /// <summary>Sets the value at (x, y). Throws if out of bounds.</summary>
        public void Set(int x, int y, T value)
        {
            if (!IsInBounds(x, y))
                throw new ArgumentOutOfRangeException($"({x},{y}) is out of bounds for grid [{Width}x{Height}]");
            _data[y * Width + x] = value;
        }

        /// <summary>
        /// Gets the value at (x, y), returning the default for T if out of bounds.
        /// </summary>
        public T GetOrDefault(int x, int y, T fallback = default)
        {
            return IsInBounds(x, y) ? _data[y * Width + x] : fallback;
        }

        /// <summary>Fills every cell with the specified value.</summary>
        public void Fill(T value)
        {
            for (int i = 0; i < _data.Length; i++)
                _data[i] = value;
        }

        /// <summary>Iterates over every cell, invoking the callback with (x, y, value).</summary>
        public void ForEach(Action<int, int, T> callback)
        {
            for (int y = 0; y < Height; y++)
                for (int x = 0; x < Width; x++)
                    callback(x, y, _data[y * Width + x]);
        }
    }

    /// <summary>
    /// Tile types used in the dungeon grid. Matches the integer encoding
    /// from the Python map generator.
    /// </summary>
    public static class TileType
    {
        public const int Void    = 0;  // Unused / outside the map
        public const int Wall    = 1;  // Impassable wall
        public const int Floor   = 2;  // Walkable floor
        public const int Door    = 3;  // Door (may be locked)
        public const int Chest   = 4;  // Loot chest
        public const int Exit    = 5;  // Staircase to next floor
        public const int Player  = 6;  // Player spawn point
        public const int Enemy   = 7;  // Enemy spawn point
    }

    /// <summary>
    /// Manages the 2D tile grid for the current dungeon floor. Provides
    /// coordinate conversion, walkability queries, and neighbor lookups.
    /// </summary>
    public class GridManager : MonoBehaviour
    {
        #region Constants

        /// <summary>Size of each grid cell in Unity world units.</summary>
        public const float CellSize = 1f;

        #endregion

        #region Properties

        /// <summary>The underlying tile grid. Null until a floor is loaded.</summary>
        public Grid<int> TileGrid { get; private set; }

        /// <summary>Width of the current grid (in cells).</summary>
        public int Width => TileGrid != null ? TileGrid.Width : 0;

        /// <summary>Height of the current grid (in cells).</summary>
        public int Height => TileGrid != null ? TileGrid.Height : 0;

        #endregion

        #region Public Methods - Grid Setup

        /// <summary>
        /// Creates a new tile grid from the given dimensions and raw data array.
        /// The data array is expected in row-major order (y * width + x).
        /// </summary>
        /// <param name="width">Grid width in cells.</param>
        /// <param name="height">Grid height in cells.</param>
        /// <param name="tileData">Flat array of tile type integers.</param>
        public void InitializeGrid(int width, int height, int[] tileData)
        {
            TileGrid = new Grid<int>(width, height);

            int count = Mathf.Min(tileData.Length, width * height);
            for (int i = 0; i < count; i++)
            {
                int x = i % width;
                int y = i / width;
                TileGrid.Set(x, y, tileData[i]);
            }

            Debug.Log($"[GridManager] Grid initialized: {width}x{height}");
        }

        /// <summary>
        /// Creates a new tile grid from a 2D jagged array (array of rows).
        /// Each inner array is one row (y-index), columns are x-indices.
        /// </summary>
        public void InitializeGrid(int[][] rows)
        {
            if (rows == null || rows.Length == 0)
            {
                Debug.LogError("[GridManager] Cannot initialize from empty row data.");
                return;
            }

            int height = rows.Length;
            int width = rows[0].Length;
            TileGrid = new Grid<int>(width, height);

            for (int y = 0; y < height; y++)
            {
                int rowLen = Mathf.Min(rows[y].Length, width);
                for (int x = 0; x < rowLen; x++)
                {
                    TileGrid.Set(x, y, rows[y][x]);
                }
            }

            Debug.Log($"[GridManager] Grid initialized from rows: {width}x{height}");
        }

        #endregion

        #region Public Methods - Coordinate Conversion

        /// <summary>
        /// Converts a grid position to a Unity world position.
        /// Grid (0,0) maps to world origin.
        /// </summary>
        public Vector3 GridToWorld(int gridX, int gridY)
        {
            return new Vector3(gridX * CellSize, gridY * CellSize, 0f);
        }

        /// <summary>
        /// Converts a grid position (Vector2Int) to a Unity world position.
        /// </summary>
        public Vector3 GridToWorld(Vector2Int gridPos)
        {
            return GridToWorld(gridPos.x, gridPos.y);
        }

        /// <summary>
        /// Converts a Unity world position to the nearest grid coordinates.
        /// </summary>
        public Vector2Int WorldToGrid(Vector3 worldPos)
        {
            int x = Mathf.RoundToInt(worldPos.x / CellSize);
            int y = Mathf.RoundToInt(worldPos.y / CellSize);
            return new Vector2Int(x, y);
        }

        #endregion

        #region Public Methods - Queries

        /// <summary>
        /// Returns the tile type at the given grid coordinates.
        /// Returns TileType.Void if out of bounds.
        /// </summary>
        public int GetTileAt(int x, int y)
        {
            if (TileGrid == null) return TileType.Void;
            return TileGrid.GetOrDefault(x, y, TileType.Void);
        }

        /// <summary>
        /// Returns the tile type at the given grid position.
        /// </summary>
        public int GetTileAt(Vector2Int gridPos)
        {
            return GetTileAt(gridPos.x, gridPos.y);
        }

        /// <summary>
        /// Sets the tile type at the given grid coordinates.
        /// </summary>
        public void SetTileAt(int x, int y, int tileType)
        {
            if (TileGrid != null && TileGrid.IsInBounds(x, y))
            {
                TileGrid.Set(x, y, tileType);
            }
        }

        /// <summary>
        /// Checks if the tile at (x, y) is walkable (Floor, Door, Chest, Exit, Player, Enemy).
        /// Walls and Void are not walkable.
        /// </summary>
        public bool IsWalkable(int x, int y)
        {
            int tile = GetTileAt(x, y);
            return tile == TileType.Floor
                || tile == TileType.Door
                || tile == TileType.Chest
                || tile == TileType.Exit
                || tile == TileType.Player
                || tile == TileType.Enemy;
        }

        /// <summary>
        /// Checks if the tile at the given grid position is walkable.
        /// </summary>
        public bool IsWalkable(Vector2Int gridPos)
        {
            return IsWalkable(gridPos.x, gridPos.y);
        }

        /// <summary>
        /// Returns the 4-directional (up, down, left, right) neighbors of a cell
        /// that are within grid bounds.
        /// </summary>
        public List<Vector2Int> GetNeighbors4(int x, int y)
        {
            List<Vector2Int> neighbors = new List<Vector2Int>(4);

            if (TileGrid == null) return neighbors;

            // Up
            if (TileGrid.IsInBounds(x, y + 1))
                neighbors.Add(new Vector2Int(x, y + 1));
            // Down
            if (TileGrid.IsInBounds(x, y - 1))
                neighbors.Add(new Vector2Int(x, y - 1));
            // Left
            if (TileGrid.IsInBounds(x - 1, y))
                neighbors.Add(new Vector2Int(x - 1, y));
            // Right
            if (TileGrid.IsInBounds(x + 1, y))
                neighbors.Add(new Vector2Int(x + 1, y));

            return neighbors;
        }

        /// <summary>
        /// Returns the 4-directional neighbors of a cell.
        /// </summary>
        public List<Vector2Int> GetNeighbors4(Vector2Int pos)
        {
            return GetNeighbors4(pos.x, pos.y);
        }

        /// <summary>
        /// Returns the 4-directional walkable neighbors of a cell.
        /// </summary>
        public List<Vector2Int> GetWalkableNeighbors4(int x, int y)
        {
            List<Vector2Int> neighbors = GetNeighbors4(x, y);
            neighbors.RemoveAll(n => !IsWalkable(n.x, n.y));
            return neighbors;
        }

        /// <summary>
        /// Returns the 4-directional walkable neighbors of a cell.
        /// </summary>
        public List<Vector2Int> GetWalkableNeighbors4(Vector2Int pos)
        {
            return GetWalkableNeighbors4(pos.x, pos.y);
        }

        /// <summary>
        /// Returns the 8-directional (including diagonals) neighbors of a cell
        /// that are within grid bounds.
        /// </summary>
        public List<Vector2Int> GetNeighbors8(int x, int y)
        {
            List<Vector2Int> neighbors = new List<Vector2Int>(8);

            if (TileGrid == null) return neighbors;

            for (int dy = -1; dy <= 1; dy++)
            {
                for (int dx = -1; dx <= 1; dx++)
                {
                    if (dx == 0 && dy == 0) continue;
                    int nx = x + dx;
                    int ny = y + dy;
                    if (TileGrid.IsInBounds(nx, ny))
                        neighbors.Add(new Vector2Int(nx, ny));
                }
            }

            return neighbors;
        }

        /// <summary>
        /// Finds all grid positions that contain the specified tile type.
        /// </summary>
        public List<Vector2Int> FindTilesOfType(int tileType)
        {
            List<Vector2Int> results = new List<Vector2Int>();
            if (TileGrid == null) return results;

            TileGrid.ForEach((x, y, tile) =>
            {
                if (tile == tileType)
                    results.Add(new Vector2Int(x, y));
            });

            return results;
        }

        /// <summary>
        /// Returns the Manhattan distance between two grid positions.
        /// </summary>
        public static int ManhattanDistance(Vector2Int a, Vector2Int b)
        {
            return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
        }

        #endregion
    }
}
