using UnityEngine;
using System;

namespace AbyssWalker.Meta
{
    /// <summary>
    /// Tracks the current run's state and handles run lifecycle:
    /// start, progress, end, and reward calculation.
    /// </summary>
    public class RunManager : MonoBehaviour
    {
        public static RunManager Instance { get; private set; }

        [Header("Soul Reward Configuration")]
        [SerializeField] private int soulsPerFloor = 2;
        [SerializeField] private int soulsPer100Gold = 1;
        [SerializeField] private int soulsPerBoss = 5;
        [SerializeField] private int soulsPer10Kills = 1;

        [Header("References")]
        [SerializeField] private string selectedClassName = "Warrior";

        // ── Run State ──
        public RunState CurrentRun { get; private set; }

        public bool IsRunActive { get; private set; }

        public event Action<RunState> OnRunStarted;
        public event Action<RunSummary> OnRunEnded;
        public event Action<int> OnFloorReached;
        public event Action<int> OnEnemyKilled;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        /// <summary>
        /// Starts a new run with the specified class.
        /// </summary>
        public void StartRun(string className)
        {
            selectedClassName = className;

            CurrentRun = new RunState
            {
                className = className,
                startTime = Time.time,
                currentFloor = 1,
                enemiesKilled = 0,
                goldEarned = 0,
                soulsEarned = 0,
                eventsTriggered = 0,
                bossesKilled = 0
            };

            IsRunActive = true;
            OnRunStarted?.Invoke(CurrentRun);

            Debug.Log($"[RunManager] Run started with class: {className}");
        }

        /// <summary>
        /// Records reaching a new floor.
        /// </summary>
        public void ReachFloor(int floor)
        {
            if (!IsRunActive) return;

            CurrentRun.currentFloor = floor;
            CurrentRun.furthestFloor = Mathf.Max(CurrentRun.furthestFloor, floor);

            OnFloorReached?.Invoke(floor);
        }

        /// <summary>
        /// Records an enemy kill.
        /// </summary>
        public void RecordKill(bool isBoss = false)
        {
            if (!IsRunActive) return;

            CurrentRun.enemiesKilled++;
            if (isBoss) CurrentRun.bossesKilled++;

            OnEnemyKilled?.Invoke(CurrentRun.enemiesKilled);
        }

        /// <summary>
        /// Records gold earned during the run.
        /// </summary>
        public void RecordGold(int amount)
        {
            if (!IsRunActive) return;
            CurrentRun.goldEarned += amount;
        }

        /// <summary>
        /// Records an event being triggered.
        /// </summary>
        public void RecordEvent()
        {
            if (!IsRunActive) return;
            CurrentRun.eventsTriggered++;
        }

        /// <summary>
        /// Ends the current run, calculates rewards, and saves progress.
        /// </summary>
        public RunSummary EndRun(bool isVictory = false)
        {
            if (!IsRunActive) return null;

            IsRunActive = false;
            CurrentRun.endTime = Time.time;
            CurrentRun.isVictory = isVictory;

            // Calculate soul rewards
            int soulsReward = CalculateSoulReward();
            CurrentRun.soulsEarned = soulsReward;

            // Build summary
            RunSummary summary = new RunSummary
            {
                className = CurrentRun.className,
                floorReached = CurrentRun.currentFloor,
                enemiesKilled = CurrentRun.enemiesKilled,
                goldEarned = CurrentRun.goldEarned,
                soulsEarned = soulsReward,
                bossesKilled = CurrentRun.bossesKilled,
                eventsTriggered = CurrentRun.eventsTriggered,
                playTimeSeconds = CurrentRun.endTime - CurrentRun.startTime,
                isVictory = isVictory
            };

            // Apply rewards and update stats
            ApplyRunResults(summary);

            OnRunEnded?.Invoke(summary);

            Debug.Log($"[RunManager] Run ended. Floor: {summary.floorReached}, " +
                      $"Souls earned: {summary.soulsEarned}");

            return summary;
        }

        /// <summary>
        /// Returns the currently selected class name.
        /// </summary>
        public string GetSelectedClass() => selectedClassName;

        /// <summary>
        /// Sets the selected class for the next run.
        /// </summary>
        public void SetSelectedClass(string className) => selectedClassName = className;

        private int CalculateSoulReward()
        {
            int souls = 0;

            // Souls for floors cleared
            souls += CurrentRun.currentFloor * soulsPerFloor;

            // Souls for gold collected
            souls += (CurrentRun.goldEarned / 100) * soulsPer100Gold;

            // Souls for bosses
            souls += CurrentRun.bossesKilled * soulsPerBoss;

            // Souls for kill milestones
            souls += (CurrentRun.enemiesKilled / 10) * soulsPer10Kills;

            // Minimum 1 soul per run
            return Mathf.Max(1, souls);
        }

        private void ApplyRunResults(RunSummary summary)
        {
            ProgressManager pm = ProgressManager.Instance;
            if (pm == null) return;

            // Add souls
            pm.AddSouls(summary.soulsEarned);

            // Update statistics
            pm.SaveData.totalRuns++;
            if (!summary.isVictory) pm.SaveData.totalDeaths++;
            pm.SaveData.furthestFloor = Mathf.Max(pm.SaveData.furthestFloor, summary.floorReached);
            pm.SaveData.totalEnemiesKilled += summary.enemiesKilled;
            pm.SaveData.totalGoldEarned += summary.goldEarned;
            pm.SaveData.totalPlayTimeSeconds += summary.playTimeSeconds;
            pm.SaveData.totalEventsTriggered += summary.eventsTriggered;
            pm.SaveData.totalBossesKilled += summary.bossesKilled;

            // Check unlocks
            UnlockSystem unlockSystem = FindObjectOfType<UnlockSystem>();
            unlockSystem?.CheckUnlocks();

            // Save
            pm.SaveProgress();
        }
    }

    /// <summary>
    /// Tracks the current run's live state.
    /// </summary>
    [System.Serializable]
    public class RunState
    {
        public string className;
        public int currentFloor;
        public int furthestFloor;
        public int enemiesKilled;
        public int goldEarned;
        public int soulsEarned;
        public int eventsTriggered;
        public int bossesKilled;
        public float startTime;
        public float endTime;
        public bool isVictory;
    }

    /// <summary>
    /// Summary of a completed run, used for display and reward calculation.
    /// </summary>
    [System.Serializable]
    public class RunSummary
    {
        public string className;
        public int floorReached;
        public int enemiesKilled;
        public int goldEarned;
        public int soulsEarned;
        public int bossesKilled;
        public int eventsTriggered;
        public float playTimeSeconds;
        public bool isVictory;

        public string GetPlayTimeFormatted()
        {
            System.TimeSpan ts = System.TimeSpan.FromSeconds(playTimeSeconds);
            return $"{ts.Hours:D2}:{ts.Minutes:D2}:{ts.Seconds:D2}";
        }
    }
}
