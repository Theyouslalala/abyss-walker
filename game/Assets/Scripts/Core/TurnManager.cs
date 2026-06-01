using System;
using System.Collections;
using UnityEngine;

namespace AbyssWalker.Core
{
    /// <summary>
    /// Manages the turn-based game loop. Each turn cycles through three phases:
    /// PlayerTurn -> EnemyTurn -> EnvironmentTurn, then increments the turn counter.
    /// </summary>
    public class TurnManager : MonoBehaviour
    {
        #region Enums

        /// <summary>The current phase of a turn cycle.</summary>
        public enum TurnPhase
        {
            None,
            PlayerTurn,
            EnemyTurn,
            EnvironmentTurn
        }

        #endregion

        #region Events

        /// <summary>Fired at the start of any turn phase. Passes the new phase.</summary>
        public event Action<TurnPhase> OnTurnPhaseStart;

        /// <summary>Fired at the end of any turn phase. Passes the ending phase.</summary>
        public event Action<TurnPhase> OnTurnPhaseEnd;

        /// <summary>Fired when the overall turn counter increments (after a full cycle).</summary>
        public event Action<int> OnTurnCycleComplete;

        #endregion

        #region Inspector Fields

        [SerializeField] private float _phaseTransitionDelay = 0.1f;

        #endregion

        #region Properties

        /// <summary>The current turn phase.</summary>
        public TurnPhase CurrentPhase { get; private set; } = TurnPhase.None;

        /// <summary>Number of completed turn cycles (starts at 0, increments after EnvironmentTurn).</summary>
        public int TurnCount { get; private set; } = 0;

        /// <summary>Whether the turn system is currently processing a phase.</summary>
        public bool IsProcessing { get; private set; } = false;

        /// <summary>Whether we are waiting for the player to submit an action.</summary>
        public bool WaitingForPlayerInput { get; private set; } = false;

        #endregion

        #region Private Fields

        private bool _playerActionSubmitted = false;
        private Coroutine _turnCoroutine;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            // Subscribe to relevant game events
            EventManager em = null;
            if (GameManager.Instance != null)
            {
                em = GameManager.Instance.EventManager;
            }

            if (em != null)
            {
                em.OnPlayerMove += HandlePlayerMove;
                em.OnCombatEnd += HandleCombatEnd;
            }
        }

        private void OnDestroy()
        {
            EventManager em = null;
            if (GameManager.Instance != null)
            {
                em = GameManager.Instance.EventManager;
            }

            if (em != null)
            {
                em.OnPlayerMove -= HandlePlayerMove;
                em.OnCombatEnd -= HandleCombatEnd;
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Resets the turn counter and phase to their initial values.
        /// </summary>
        public void ResetTurns()
        {
            StopCurrentCoroutine();
            TurnCount = 0;
            CurrentPhase = TurnPhase.None;
            WaitingForPlayerInput = false;
            IsProcessing = false;
            _playerActionSubmitted = false;
        }

        /// <summary>
        /// Begins the next turn cycle, starting with the PlayerTurn phase.
        /// </summary>
        public void StartNextTurn()
        {
            if (GameManager.Instance != null && !GameManager.Instance.IsPlaying)
            {
                return;
            }

            StopCurrentCoroutine();
            _turnCoroutine = StartCoroutine(RunTurnCycle());
        }

        /// <summary>
        /// Signals that the player has completed their action this turn.
        /// </summary>
        public void SubmitPlayerAction()
        {
            if (CurrentPhase == TurnPhase.PlayerTurn)
            {
                _playerActionSubmitted = true;
            }
        }

        /// <summary>
        /// Skips the current player turn (e.g. the player chose to wait).
        /// </summary>
        public void SkipPlayerTurn()
        {
            SubmitPlayerAction();
        }

        #endregion

        #region Private Methods - Coroutine

        /// <summary>
        /// Runs a full turn cycle: PlayerTurn -> EnemyTurn -> EnvironmentTurn.
        /// </summary>
        private IEnumerator RunTurnCycle()
        {
            IsProcessing = true;

            // --- Player Turn ---
            yield return RunPhase(TurnPhase.PlayerTurn);

            // --- Enemy Turn ---
            yield return RunPhase(TurnPhase.EnemyTurn);

            // --- Environment Turn ---
            yield return RunPhase(TurnPhase.EnvironmentTurn);

            // Cycle complete
            TurnCount++;
            OnTurnCycleComplete?.Invoke(TurnCount);

            IsProcessing = false;
            _turnCoroutine = null;

            // Automatically start next cycle if still playing
            if (GameManager.Instance != null && GameManager.Instance.IsPlaying)
            {
                StartNextTurn();
            }
        }

        /// <summary>
        /// Runs a single phase: fires start event, waits for completion, fires end event.
        /// </summary>
        private IEnumerator RunPhase(TurnPhase phase)
        {
            CurrentPhase = phase;
            OnTurnPhaseStart?.Invoke(phase);

            if (_phaseTransitionDelay > 0f)
            {
                yield return new WaitForSeconds(_phaseTransitionDelay);
            }

            switch (phase)
            {
                case TurnPhase.PlayerTurn:
                    yield return WaitForPlayer();
                    break;

                case TurnPhase.EnemyTurn:
                    yield return RunEnemyTurn();
                    break;

                case TurnPhase.EnvironmentTurn:
                    yield return RunEnvironmentTurn();
                    break;
            }

            OnTurnPhaseEnd?.Invoke(phase);
        }

        /// <summary>
        /// Blocks until the player submits an action.
        /// </summary>
        private IEnumerator WaitForPlayer()
        {
            WaitingForPlayerInput = true;
            _playerActionSubmitted = false;

            while (!_playerActionSubmitted)
            {
                yield return null;
            }

            WaitingForPlayerInput = false;
        }

        /// <summary>
        /// Runs all enemy actions. Currently a stub; enemies act through the AI server.
        /// </summary>
        private IEnumerator RunEnemyTurn()
        {
            // TODO: Request AI decisions from the Python server and apply them
            // For now, yield one frame so the phase completes
            yield return null;
        }

        /// <summary>
        /// Runs environment effects (traps, hazards, etc.). Currently a stub.
        /// </summary>
        private IEnumerator RunEnvironmentTurn()
        {
            // TODO: Process environmental effects
            yield return null;
        }

        /// <summary>Handle player move event as a turn action submission.</summary>
        private void HandlePlayerMove(int fromX, int fromY, int toX, int toY)
        {
            SubmitPlayerAction();
        }

        /// <summary>Handle combat end event as a turn action submission.</summary>
        private void HandleCombatEnd(int winnerId, int loserId, int damageDealt)
        {
            SubmitPlayerAction();
        }

        /// <summary>Stops the running turn coroutine if one exists.</summary>
        private void StopCurrentCoroutine()
        {
            if (_turnCoroutine != null)
            {
                StopCoroutine(_turnCoroutine);
                _turnCoroutine = null;
            }
        }

        #endregion
    }
}
