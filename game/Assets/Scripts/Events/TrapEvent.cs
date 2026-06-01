using UnityEngine;
using System;
using AbyssWalker.Entity;

namespace AbyssWalker.Events
{
    /// <summary>
    /// Handles trap event logic: hidden traps that deal damage or apply debuffs.
    /// Can be detected early if the player has high enough perception.
    /// </summary>
    public class TrapEvent : MonoBehaviour
    {
        [Header("Trap Settings")]
        [SerializeField] private int baseDetectionPerception = 5;

        [Header("Debuff Definitions")]
        [SerializeField] private DebuffDefinition[] availableDebuffs;

        [Header("UI Reference")]
        [SerializeField] private PopupUI trapPopup;

        public event Action<TrapResult> OnTrapTriggered;
        public event Action<EventData> OnTrapDetected;

        /// <summary>
        /// Checks if the player can detect this trap before stepping on it.
        /// Called when the player is adjacent to the trap tile.
        /// </summary>
        public bool CanDetectTrap(EventData eventData, int playerPerception)
        {
            // Higher perception = higher chance to detect
            // Each point of perception above base adds 15% detection chance
            float detectChance = (playerPerception - baseDetectionPerception) * 0.15f;
            detectChance = Mathf.Clamp01(detectChance);

            bool detected = UnityEngine.Random.Range(0f, 1f) < detectChance;

            if (detected)
            {
                OnTrapDetected?.Invoke(eventData);
            }

            return detected;
        }

        /// <summary>
        /// Triggers the trap when the player steps on it.
        /// </summary>
        public TrapResult TriggerTrap(EventData eventData, int playerDEF)
        {
            if (eventData == null || eventData.hasBeenTriggered)
            {
                return new TrapResult { wasTriggered = false };
            }

            eventData.hasBeenTriggered = true;
            eventData.isActive = false;

            TrapResult result = new TrapResult();
            result.wasTriggered = true;
            result.trapName = eventData.eventName;

            // Calculate damage with defense reduction
            int rawDamage = eventData.damageAmount > 0
                ? eventData.damageAmount
                : UnityEngine.Random.Range(5, 15);
            int mitigated = Mathf.Max(1, rawDamage - playerDEF / 3);
            result.damageDealt = mitigated;

            // Roll for debuff
            if (!string.IsNullOrEmpty(eventData.debuffType))
            {
                result.debuffApplied = eventData.debuffType;
                result.debuffDuration = 3; // Default 3 turns
            }
            else if (availableDebuffs != null && availableDebuffs.Length > 0)
            {
                DebuffDefinition debuff = availableDebuffs[UnityEngine.Random.Range(0, availableDebuffs.Length)];
                if (UnityEngine.Random.Range(0f, 1f) <= debuff.applyChance)
                {
                    result.debuffApplied = debuff.debuffName;
                    result.debuffDuration = debuff.duration;
                }
            }

            // Apply damage and debuff to player
            ApplyTrapEffect(result);

            ShowTrapPopup(result);

            OnTrapTriggered?.Invoke(result);

            return result;
        }

        private void ApplyTrapEffect(TrapResult result)
        {
            Player player = FindObjectOfType<Player>();
            if (player == null) return;

            player.TakeDamage(result.damageDealt);

            if (!string.IsNullOrEmpty(result.debuffApplied))
            {
                player.ApplyDebuff(result.debuffApplied, result.debuffDuration);
            }
        }

        private void ShowTrapPopup(TrapResult result)
        {
            if (trapPopup == null)
            {
                trapPopup = FindObjectOfType<PopupUI>();
            }

            if (trapPopup != null)
            {
                string body = $"You triggered a trap!\nDamage: -{result.damageDealt} HP";
                if (!string.IsNullOrEmpty(result.debuffApplied))
                {
                    body += $"\nDebuff: {result.debuffApplied} ({result.debuffDuration} turns)";
                }
                trapPopup.Show("Trap!", body, "Ouch!", null);
            }
        }
    }

    /// <summary>
    /// Result of triggering a trap.
    /// </summary>
    [System.Serializable]
    public class TrapResult
    {
        public bool wasTriggered;
        public string trapName;
        public int damageDealt;
        public string debuffApplied;
        public int debuffDuration;
    }

    /// <summary>
    /// Definition of a debuff that traps can apply.
    /// </summary>
    [System.Serializable]
    public class DebuffDefinition
    {
        public string debuffName;
        public string description;
        public int duration = 3;
        [Range(0f, 1f)]
        public float applyChance = 0.5f;
    }
}
