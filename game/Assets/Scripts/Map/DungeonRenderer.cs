using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using AbyssWalker.Core;
using AbyssWalker.Network;

namespace AbyssWalker.Map
{
    /// <summary>
    /// Renders the dungeon floor using a Unity Tilemap. Receives map data from the
    /// Python AI server (via SocketClient) and paints tiles accordingly.
    /// </summary>
    [RequireComponent(typeof(Tilemap))]
    public class DungeonRenderer : MonoBehaviour
    {
        #region Inspector Fields

        [Header("Tilemap References")]
        [SerializeField] private Tilemap _tilemap;

        [Header("Tile Assets")]
        [Tooltip("Tile used for walls.")]
        [SerializeField] private TileBase _wallTile;

        [Tooltip("Tile used for walkable floor.")]
        [SerializeField] private TileBase _floorTile;

        [Tooltip("Tile used for doors.")]
        [SerializeField] private TileBase _doorTile;

        [Tooltip("Tile used for loot chests.")]
        [SerializeField] private TileBase _chestTile;

        [Tooltip("Tile used for the exit staircase.")]
        [SerializeField] private TileBase _exitTile;

        [Tooltip("Tile used for the player spawn point (editor/debug only).")]
        [SerializeField] private TileBase _playerSpawnTile;

        [Tooltip("Tile used for enemy spawn points (editor/debug only).")]
        [SerializeField] private TileBase _enemySpawnTile;

        [Header("Rendering Options")]
        [Tooltip("If true, spawn-point tiles (Player/Enemy) are rendered as floor after initialization.")]
        [SerializeField] private bool _clearSpawnTilesOnRender = true;

        #endregion

        #region Private Fields

        private GridManager _gridManager;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            if (_tilemap == null)
                _tilemap = GetComponent<Tilemap>();

            _gridManager = FindObjectOfType<GridManager>();
        }

        private void Start()
        {
            // Subscribe to network events for map data
            SocketClient client = null;
            if (GameManager.Instance != null)
            {
                client = GameManager.Instance.SocketClient;
                _gridManager = GameManager.Instance.GridManager ?? _gridManager;
            }

            if (client != null)
            {
                client.OnMapDataReceived += HandleMapDataReceived;
            }
        }

        private void OnDestroy()
        {
            SocketClient client = null;
            if (GameManager.Instance != null)
            {
                client = GameManager.Instance.SocketClient;
            }

            if (client != null)
            {
                client.OnMapDataReceived -= HandleMapDataReceived;
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Clears the tilemap and renders the dungeon from the current GridManager data.
        /// </summary>
        public void RenderFromGridManager()
        {
            if (_gridManager == null || _gridManager.TileGrid == null)
            {
                Debug.LogWarning("[DungeonRenderer] GridManager has no tile data to render.");
                return;
            }

            ClearTilemap();

            _gridManager.TileGrid.ForEach((x, y, tileType) =>
            {
                TileBase tile = GetTileForType(tileType);
                if (tile != null)
                {
                    Vector3Int cellPos = new Vector3Int(x, y, 0);
                    _tilemap.SetTile(cellPos, tile);
                }
            });

            if (_clearSpawnTilesOnRender)
            {
                ClearSpawnTiles();
            }

            Debug.Log("[DungeonRenderer] Tilemap rendered from GridManager data.");
        }

        /// <summary>
        /// Renders a dungeon from a MapDataMessage received over the network.
        /// Initializes the GridManager and paints the tilemap.
        /// </summary>
        /// <param name="mapData">Map data from the AI server.</param>
        public void RenderFromMapData(MapDataMessage mapData)
        {
            if (mapData == null || mapData.Tiles == null)
            {
                Debug.LogError("[DungeonRenderer] Cannot render null map data.");
                return;
            }

            int width = mapData.Width;
            int height = mapData.Height;

            // Flatten 2D tiles array into 1D if needed
            int[] flatTiles = FlattenTileData(mapData.Tiles, width, height);

            if (_gridManager != null)
            {
                _gridManager.InitializeGrid(width, height, flatTiles);
            }

            RenderFromGridManager();

            if (GameManager.Instance != null && GameManager.Instance.EventManager != null)
            {
                GameManager.Instance.EventManager.RaiseFloorLoaded(GameManager.Instance.CurrentFloor);
            }
        }

        /// <summary>
        /// Requests the AI server to generate a new floor and renders it.
        /// </summary>
        /// <param name="floorNumber">The floor number to generate.</param>
        public void RequestNewFloor(int floorNumber)
        {
            SocketClient client = null;
            if (GameManager.Instance != null)
            {
                client = GameManager.Instance.SocketClient;
            }

            if (client != null && client.IsConnected)
            {
                GameStateSerializer serializer = new GameStateSerializer();
                string request = serializer.CreateFloorRequestMessage(floorNumber);
                client.Send(request);
                Debug.Log($"[DungeonRenderer] Requested floor {floorNumber} from AI server.");
            }
            else
            {
                Debug.LogWarning("[DungeonRenderer] No connection to AI server. Cannot request new floor.");
            }
        }

        /// <summary>
        /// Updates a single tile on the tilemap.
        /// </summary>
        /// <param name="gridX">Grid X coordinate.</param>
        /// <param name="gridY">Grid Y coordinate.</param>
        /// <param name="newTileType">New tile type (see TileType constants).</param>
        public void UpdateTile(int gridX, int gridY, int newTileType)
        {
            if (_gridManager != null)
            {
                _gridManager.SetTileAt(gridX, gridY, newTileType);
            }

            TileBase tile = GetTileForType(newTileType);
            Vector3Int cellPos = new Vector3Int(gridX, gridY, 0);

            if (tile != null)
            {
                _tilemap.SetTile(cellPos, tile);
            }
            else
            {
                _tilemap.SetTile(cellPos, null);
            }
        }

        /// <summary>
        /// Clears all tiles from the tilemap.
        /// </summary>
        public void ClearTilemap()
        {
            if (_tilemap != null)
            {
                _tilemap.ClearAllTiles();
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Maps a tile type integer to the corresponding TileBase asset.
        /// </summary>
        private TileBase GetTileForType(int tileType)
        {
            switch (tileType)
            {
                case TileType.Wall:   return _wallTile;
                case TileType.Floor:  return _floorTile;
                case TileType.Door:   return _doorTile;
                case TileType.Chest:  return _chestTile;
                case TileType.Exit:   return _exitTile;
                case TileType.Player: return _playerSpawnTile ?? _floorTile;
                case TileType.Enemy:  return _enemySpawnTile ?? _floorTile;
                default:              return null; // Void
            }
        }

        /// <summary>
        /// Replaces Player/Enemy spawn tiles with plain floor tiles after initial render.
        /// </summary>
        private void ClearSpawnTiles()
        {
            if (_gridManager == null || _gridManager.TileGrid == null) return;

            _gridManager.TileGrid.ForEach((x, y, tileType) =>
            {
                if (tileType == TileType.Player || tileType == TileType.Enemy)
                {
                    Vector3Int cellPos = new Vector3Int(x, y, 0);
                    if (_floorTile != null)
                    {
                        _tilemap.SetTile(cellPos, _floorTile);
                    }
                }
            });
        }

        /// <summary>
        /// Flattens a 2D jagged tile array into a 1D array (row-major order).
        /// </summary>
        private int[] FlattenTileData(int[][] tiles2D, int width, int height)
        {
            int[] flat = new int[width * height];
            for (int y = 0; y < Mathf.Min(tiles2D.Length, height); y++)
            {
                int rowLen = Mathf.Min(tiles2D[y].Length, width);
                for (int x = 0; x < rowLen; x++)
                {
                    flat[y * width + x] = tiles2D[y][x];
                }
            }
            return flat;
        }

        /// <summary>
        /// Handles map data received from the AI server.
        /// </summary>
        private void HandleMapDataReceived(MapDataMessage mapData)
        {
            RenderFromMapData(mapData);
        }

        #endregion
    }
}
