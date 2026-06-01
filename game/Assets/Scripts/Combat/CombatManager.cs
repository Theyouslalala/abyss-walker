using System;
using UnityEngine;
using AbyssWalker.Core;
using AbyssWalker.Entity;

namespace AbyssWalker.Combat
{
    /// <summary>
    /// Manages combat flow: damage calculation, attack processing, critical hits,
    /// and combat event logging.
    /// </summary>
    public class CombatManager : MonoBehaviour
    {
        [Header("Combat Settings")]
        [SerializeField] private float baseCritChance = 0.05f;
        [SerializeField] private float critDamageMultiplier = 2.0f;

        /// <summary>
        /// Raised when any attack occurs. Passes (attacker, defender, damage, wasCrit).
        /// </summary>
        public event Action<EntityStats, EntityStats, int, bool> OnAttackDealt;

        /// <summary>
        /// Raised when an entity dies from combat. Passes the dead entity's stats.
        /// </summary>
        public event Action<EntityStats> OnEntityKilled;

        /// <summary>
        /// Process a physical attack from attacker to defender.
        /// Calculates damage, applies critical hit, and triggers events.
        /// </summary>
        /// <param name="attacker">The attacking entity's stats.</param>
        /// <param name="defender">The defending entity's stats.</param>
        public void ProcessAttack(EntityStats attacker, EntityStats defender)
        {
            if (attacker == null || defender == null || !defender.IsAlive) return;

            bool isCrit = DamageCalculator.RollCriticalHit(baseCritChance);
            int damage = DamageCalculator.CalculatePhysicalDamage(
                attacker.attack, defender.defense, isCrit, critDamageMultiplier);

            defender.TakeDamage(damage);
            OnAttackDealt?.Invoke(attacker, defender, damage, isCrit);

            if (!defender.IsAlive)
            {
                OnEntityKilled?.Invoke(defender);
            }

            Debug.Log($"[Combat] Attack: {damage} dmg" +
                      (isCrit ? " (CRIT!)" : "") +
                      $" | Defender HP: {defender.hp}/{defender.maxHp}");
        }

        /// <summary>
        /// Process a magical attack (e.g. from a skill) from attacker to defender.
        /// Uses magical damage calculation (ignores physical defense partially).
        /// </summary>
        public void ProcessMagicalAttack(EntityStats attacker, EntityStats defender,
                                         int magicPower, bool isCrit)
        {
            if (attacker == null || defender == null || !defender.IsAlive) return;

            int damage = DamageCalculator.CalculateMagicalDamage(
                magicPower, defender.defense, isCrit, critDamageMultiplier);

            defender.TakeDamage(damage);
            OnAttackDealt?.Invoke(attacker, defender, damage, isCrit);

            if (!defender.IsAlive)
            {
                OnEntityKilled?.Invoke(defender);
            }
        }

        /// <summary>
        /// Process healing from a source to a target.
        /// </summary>
        public void ProcessHeal(EntityStats healer, EntityStats target, int healAmount)
        {
            if (target == null || !target.IsAlive) return;

            target.Heal(healAmount);
        }
    }
}
