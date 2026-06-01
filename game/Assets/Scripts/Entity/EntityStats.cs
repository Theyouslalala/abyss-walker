using System;
using UnityEngine;

namespace AbyssWalker.Entity
{
    /// <summary>
    /// Serializable stats container for all entities (player and enemies).
    /// Handles HP, attack, defense, leveling, and raises events on state changes.
    /// </summary>
    [System.Serializable]
    public class EntityStats
    {
        [Header("Health")]
        public int hp;
        public int maxHp;

        [Header("Combat")]
        public int attack;
        public int defense;
        public float speed;

        [Header("Progression")]
        public int level;
        public int exp;
        public int expToNextLevel;

        /// <summary>Raised when HP reaches zero. Passes this EntityStats instance.</summary>
        public event Action<EntityStats> OnDeath;

        /// <summary>Raised whenever current HP changes. Passes (currentHp, maxHp).</summary>
        public event Action<int, int> OnHealthChanged;

        /// <summary>Raised when the entity levels up. Passes new level.</summary>
        public event Action<int> OnLevelUp;

        /// <summary>Whether the entity is currently alive.</summary>
        public bool IsAlive => hp > 0;

        /// <summary>
        /// Initialize stats with default values for a given level.
        /// </summary>
        public void Initialize(int startLevel = 1)
        {
            level = startLevel;
            exp = 0;
            expToNextLevel = CalculateExpToLevel(level);
            maxHp = 100 + (level - 1) * 20;
            hp = maxHp;
            attack = 10 + (level - 1) * 3;
            defense = 5 + (level - 1) * 2;
            speed = 1.0f;
        }

        /// <summary>
        /// Apply damage to this entity. Defense should already be factored in by the caller.
        /// </summary>
        /// <param name="damage">Final damage amount (minimum 1).</param>
        public void TakeDamage(int damage)
        {
            if (!IsAlive) return;

            int actualDamage = Mathf.Max(1, damage);
            hp = Mathf.Max(0, hp - actualDamage);
            OnHealthChanged?.Invoke(hp, maxHp);

            if (hp <= 0)
            {
                OnDeath?.Invoke(this);
            }
        }

        /// <summary>
        /// Restore HP. Cannot exceed maxHp.
        /// </summary>
        /// <param name="amount">Amount of HP to restore.</param>
        public void Heal(int amount)
        {
            if (!IsAlive) return;

            hp = Mathf.Min(maxHp, hp + Mathf.Max(0, amount));
            OnHealthChanged?.Invoke(hp, maxHp);
        }

        /// <summary>
        /// Add experience points and trigger level-up if threshold is reached.
        /// </summary>
        /// <param name="amount">EXP to add.</param>
        public void AddExp(int amount)
        {
            if (amount <= 0) return;

            exp += amount;
            while (exp >= expToNextLevel)
            {
                LevelUp();
            }
        }

        /// <summary>
        /// Perform a level-up: increase stats, reset EXP overflow, raise event.
        /// </summary>
        public void LevelUp()
        {
            level++;
            exp -= expToNextLevel;
            expToNextLevel = CalculateExpToLevel(level);

            int hpGain = 20;
            int atkGain = 3;
            int defGain = 2;

            maxHp += hpGain;
            hp += hpGain;
            attack += atkGain;
            defense += defGain;

            OnLevelUp?.Invoke(level);
            OnHealthChanged?.Invoke(hp, maxHp);
        }

        /// <summary>
        /// Calculate EXP needed to reach the next level.
        /// Uses a simple quadratic curve: 100 * level^1.5.
        /// </summary>
        private int CalculateExpToLevel(int lvl)
        {
            return Mathf.RoundToInt(100 * Mathf.Pow(lvl, 1.5f));
        }
    }
}
