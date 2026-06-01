using System;
using UnityEngine;

namespace AbyssWalker.Core
{
    /// <summary>
    /// Central event bus for the game. Provides C# events that any system can
    /// subscribe to for decoupled communication between managers.
    /// </summary>
    public class EventManager : MonoBehaviour
    {
        #region Movement Events

        /// <summary>Fired when the player moves. Passes (fromX, fromY, toX, toY).</summary>
        public event Action<int, int, int, int> OnPlayerMove;

        /// <summary>Fired when any enemy moves. Passes (enemyId, fromX, fromY, toX, toY).</summary>
        public event Action<int, int, int, int, int> OnEnemyMove;

        #endregion

        #region Combat Events

        /// <summary>Fired when combat begins. Passes (attackerId, defenderId).</summary>
        public event Action<int, int> OnCombatStart;

        /// <summary>Fired when combat ends. Passes (winnerId, loserId, damageDealt).</summary>
        public event Action<int, int, int> OnCombatEnd;

        /// <summary>Fired when any entity takes damage. Passes (entityId, damage, remainingHp).</summary>
        public event Action<int, int, int> OnEntityDamaged;

        /// <summary>Fired when an enemy is defeated. Passes (enemyId).</summary>
        public event Action<int> OnEnemyDefeated;

        #endregion

        #region Player Events

        /// <summary>Fired when the player dies.</summary>
        public event Action OnPlayerDeath;

        /// <summary>Fired when the player picks up an item. Passes (itemType, itemId).</summary>
        public event Action<string, int> OnItemPickup;

        /// <summary>Fired when the player's health changes. Passes (currentHp, maxHp).</summary>
        public event Action<int, int> OnPlayerHealthChanged;

        #endregion

        #region Map / Floor Events

        /// <summary>Fired when the current floor has been cleared (all enemies defeated or exit reached).</summary>
        public event Action OnFloorCleared;

        /// <summary>Fired when a new floor map has been loaded and rendered.</summary>
        public event Action<int> OnFloorLoaded;

        /// <summary>Fired when a door is opened. Passes (doorX, doorY).</summary>
        public event Action<int, int> OnDoorOpened;

        /// <summary>Fired when a chest is opened. Passes (chestX, chestY).</summary>
        public event Action<int, int> OnChestOpened;

        #endregion

        #region Game Flow Events

        /// <summary>Fired when the game state changes. Passes (newState).</summary>
        public event Action<GameManager.GameState> OnGameStateChanged;

        /// <summary>Fired when a turn phase starts. Passes the turn phase.</summary>
        public event Action<TurnManager.TurnPhase> OnTurnPhaseStarted;

        /// <summary>Fired when a turn phase ends. Passes the turn phase.</summary>
        public event Action<TurnManager.TurnPhase> OnTurnPhaseEnded;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            // Wire up TurnManager events if available
            if (GameManager.Instance != null && GameManager.Instance.TurnManager != null)
            {
                GameManager.Instance.TurnManager.OnTurnPhaseStart += HandleTurnPhaseStart;
                GameManager.Instance.TurnManager.OnTurnPhaseEnd += HandleTurnPhaseEnd;
            }
        }

        private void OnDestroy()
        {
            if (GameManager.Instance != null && GameManager.Instance.TurnManager != null)
            {
                GameManager.Instance.TurnManager.OnTurnPhaseStart -= HandleTurnPhaseStart;
                GameManager.Instance.TurnManager.OnTurnPhaseEnd -= HandleTurnPhaseEnd;
            }
        }

        #endregion

        #region Public Methods - Invoke Helpers

        /// <summary>Raise the player-moved event.</summary>
        public void RaisePlayerMove(int fromX, int fromY, int toX, int toY)
        {
            OnPlayerMove?.Invoke(fromX, fromY, toX, toY);
        }

        /// <summary>Raise the enemy-moved event.</summary>
        public void RaiseEnemyMove(int enemyId, int fromX, int fromY, int toX, int toY)
        {
            OnEnemyMove?.Invoke(enemyId, fromX, fromY, toX, toY);
        }

        /// <summary>Raise the combat-start event.</summary>
        public void RaiseCombatStart(int attackerId, int defenderId)
        {
            OnCombatStart?.Invoke(attackerId, defenderId);
        }

        /// <summary>Raise the combat-end event.</summary>
        public void RaiseCombatEnd(int winnerId, int loserId, int damageDealt)
        {
            OnCombatEnd?.Invoke(winnerId, loserId, damageDealt);
        }

        /// <summary>Raise the entity-damaged event.</summary>
        public void RaiseEntityDamaged(int entityId, int damage, int remainingHp)
        {
            OnEntityDamaged?.Invoke(entityId, damage, remainingHp);
        }

        /// <summary>Raise the enemy-defeated event.</summary>
        public void RaiseEnemyDefeated(int enemyId)
        {
            OnEnemyDefeated?.Invoke(enemyId);
        }

        /// <summary>Raise the player-death event.</summary>
        public void RaisePlayerDeath()
        {
            OnPlayerDeath?.Invoke();
        }

        /// <summary>Raise the item-pickup event.</summary>
        public void RaiseItemPickup(string itemType, int itemId)
        {
            OnItemPickup?.Invoke(itemType, itemId);
        }

        /// <summary>Raise the player-health-changed event.</summary>
        public void RaisePlayerHealthChanged(int currentHp, int maxHp)
        {
            OnPlayerHealthChanged?.Invoke(currentHp, maxHp);
        }

        /// <summary>Raise the floor-cleared event.</summary>
        public void RaiseFloorCleared()
        {
            OnFloorCleared?.Invoke();
        }

        /// <summary>Raise the floor-loaded event.</summary>
        public void RaiseFloorLoaded(int floorNumber)
        {
            OnFloorLoaded?.Invoke(floorNumber);
        }

        /// <summary>Raise the door-opened event.</summary>
        public void RaiseDoorOpened(int doorX, int doorY)
        {
            OnDoorOpened?.Invoke(doorX, doorY);
        }

        /// <summary>Raise the chest-opened event.</summary>
        public void RaiseChestOpened(int chestX, int chestY)
        {
            OnChestOpened?.Invoke(chestX, chestY);
        }

        /// <summary>Raise the game-state-changed event.</summary>
        public void RaiseGameStateChanged(GameManager.GameState newState)
        {
            OnGameStateChanged?.Invoke(newState);
        }

        /// <summary>
        /// Unsubscribes all listeners from every event. Call this on scene teardown
        /// to prevent stale references.
        /// </summary>
        public void ClearAllEvents()
        {
            OnPlayerMove = null;
            OnEnemyMove = null;
            OnCombatStart = null;
            OnCombatEnd = null;
            OnEntityDamaged = null;
            OnEnemyDefeated = null;
            OnPlayerDeath = null;
            OnItemPickup = null;
            OnPlayerHealthChanged = null;
            OnFloorCleared = null;
            OnFloorLoaded = null;
            OnDoorOpened = null;
            OnChestOpened = null;
            OnGameStateChanged = null;
            OnTurnPhaseStarted = null;
            OnTurnPhaseEnded = null;
        }

        #endregion

        #region Private Methods

        private void HandleTurnPhaseStart(TurnManager.TurnPhase phase)
        {
            OnTurnPhaseStarted?.Invoke(phase);
        }

        private void HandleTurnPhaseEnd(TurnManager.TurnPhase phase)
        {
            OnTurnPhaseEnded?.Invoke(phase);
        }

        #endregion
    }
}
