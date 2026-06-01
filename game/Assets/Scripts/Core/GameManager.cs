using UnityEngine;
using UnityEngine.SceneManagement;
using AbyssWalker.Combat;

namespace AbyssWalker.Core
{
    /// <summary>
    /// Manages overall game state, floor progression, and references to all subsystem managers.
    /// Uses the Singleton pattern to ensure a single instance persists across scenes.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        #region Singleton

        private static GameManager _instance;

        /// <summary>Singleton access. Creates one if none exists in the scene.</summary>
        public static GameManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<GameManager>();
                    if (_instance == null)
                    {
                        var go = new GameObject("[GameManager]");
                        _instance = go.AddComponent<GameManager>();
                    }
                }
                return _instance;
            }
        }

        #endregion

        #region Enums

        /// <summary>Possible states the game can be in.</summary>
        public enum GameState
        {
            Menu,
            Playing,
            Paused,
            GameOver,
            Victory
        }

        #endregion

        #region Inspector Fields

        [Header("Manager References")]
        [SerializeField] private TurnManager _turnManager;
        [SerializeField] private GridManager _gridManager;
        [SerializeField] private DungeonRenderer _dungeonRenderer;
        [SerializeField] private CombatManager _combatManager;
        [SerializeField] private EventManager _eventManager;
        [SerializeField] private SocketClient _socketClient;

        #endregion

        #region Properties

        /// <summary>Current game state.</summary>
        public GameState CurrentState { get; private set; } = GameState.Menu;

        /// <summary>Current dungeon floor number (1-based).</summary>
        public int CurrentFloor { get; private set; } = 1;

        /// <summary>Maximum number of floors in the dungeon.</summary>
        public int MaxFloors { get; set; } = 10;

        /// <summary>Whether the game is currently in a playable state.</summary>
        public bool IsPlaying => CurrentState == GameState.Playing;

        // --- Manager Accessors ---

        /// <summary>The turn-based system manager.</summary>
        public TurnManager TurnManager => _turnManager;

        /// <summary>The grid/pathfinding manager.</summary>
        public GridManager GridManager => _gridManager;

        /// <summary>The dungeon tilemap renderer.</summary>
        public DungeonRenderer DungeonRenderer => _dungeonRenderer;

        /// <summary>The combat resolution manager.</summary>
        public CombatManager CombatManager => _combatManager;

        /// <summary>The central event bus.</summary>
        public EventManager EventManager => _eventManager;

        /// <summary>The TCP socket client for the Python AI server.</summary>
        public SocketClient SocketClient => _socketClient;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);

            AutoFindManagers();
        }

        private void Start()
        {
            if (_eventManager != null)
            {
                _eventManager.OnPlayerDeath += HandlePlayerDeath;
                _eventManager.OnFloorCleared += HandleFloorCleared;
            }
        }

        private void OnDestroy()
        {
            if (_eventManager != null)
            {
                _eventManager.OnPlayerDeath -= HandlePlayerDeath;
                _eventManager.OnFloorCleared -= HandleFloorCleared;
            }

            if (_instance == this)
            {
                _instance = null;
            }
        }

        #endregion

        #region Public Methods - Game Flow

        /// <summary>
        /// Starts a new game from floor 1. Resets all managers and connects to the AI server.
        /// </summary>
        public void StartNewGame()
        {
            CurrentFloor = 1;
            SetState(GameState.Playing);

            if (_socketClient != null)
            {
                _socketClient.Connect();
            }

            if (_turnManager != null)
            {
                _turnManager.ResetTurns();
                _turnManager.StartNextTurn();
            }

            Debug.Log("[GameManager] New game started.");
        }

        /// <summary>Pauses the game. Turns and input processing are suspended.</summary>
        public void PauseGame()
        {
            if (CurrentState != GameState.Playing) return;

            SetState(GameState.Paused);
            Time.timeScale = 0f;
            Debug.Log("[GameManager] Game paused.");
        }

        /// <summary>Resumes the game from a paused state.</summary>
        public void ResumeGame()
        {
            if (CurrentState != GameState.Paused) return;

            SetState(GameState.Playing);
            Time.timeScale = 1f;
            Debug.Log("[GameManager] Game resumed.");
        }

        /// <summary>
        /// Triggers the game-over state.
        /// </summary>
        public void GameOver()
        {
            SetState(GameState.GameOver);
            Time.timeScale = 0f;
            Debug.Log("[GameManager] Game Over.");
        }

        /// <summary>
        /// Triggers the victory state when the player clears the final floor.
        /// </summary>
        public void Victory()
        {
            SetState(GameState.Victory);
            Time.timeScale = 0f;
            Debug.Log("[GameManager] Victory!");
        }

        /// <summary>
        /// Advances to the next dungeon floor. Increments the floor counter and
        /// requests new map data from the AI server.
        /// </summary>
        public void AdvanceToNextFloor()
        {
            if (CurrentFloor >= MaxFloors)
            {
                Victory();
                return;
            }

            CurrentFloor++;
            Debug.Log($"[GameManager] Advancing to floor {CurrentFloor}.");

            if (_turnManager != null)
            {
                _turnManager.ResetTurns();
            }

            if (_dungeonRenderer != null)
            {
                _dungeonRenderer.RequestNewFloor(CurrentFloor);
            }
        }

        /// <summary>Reloads the current scene and returns to the menu state.</summary>
        public void ReturnToMenu()
        {
            Time.timeScale = 1f;
            CurrentFloor = 1;
            SetState(GameState.Menu);
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        #endregion

        #region Private Methods

        private void SetState(GameState newState)
        {
            GameState previous = CurrentState;
            CurrentState = newState;
            Debug.Log($"[GameManager] State: {previous} -> {newState}");
        }

        private void HandlePlayerDeath()
        {
            GameOver();
        }

        private void HandleFloorCleared()
        {
            AdvanceToNextFloor();
        }

        /// <summary>
        /// Attempts to find manager components in the scene if they are not assigned in the Inspector.
        /// </summary>
        private void AutoFindManagers()
        {
            if (_turnManager == null) _turnManager = FindObjectOfType<TurnManager>();
            if (_gridManager == null) _gridManager = FindObjectOfType<GridManager>();
            if (_dungeonRenderer == null) _dungeonRenderer = FindObjectOfType<DungeonRenderer>();
            if (_combatManager == null) _combatManager = FindObjectOfType<CombatManager>();
            if (_eventManager == null) _eventManager = FindObjectOfType<EventManager>();
            if (_socketClient == null) _socketClient = FindObjectOfType<SocketClient>();
        }

        #endregion
    }
}
