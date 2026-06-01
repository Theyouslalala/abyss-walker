using UnityEngine;

namespace AbyssWalker.Combat
{
    /// <summary>
    /// Static utility class for all damage calculations.
    /// Enforces the minimum 1 damage rule and handles critical hits.
    /// </summary>
    public static class DamageCalculator
    {
        /// <summary>
        /// Minimum damage that can ever be dealt, regardless of defense or modifiers.
        /// </summary>
        public const int MinDamage = 1;

        /// <summary>
        /// Calculate physical damage: attack - defense, minimum 1.
        /// Critical hits multiply the final result.
        /// </summary>
        /// <param name="attack">Attacker's attack stat.</param>
        /// <param name="defense">Defender's defense stat.</param>
        /// <param name="isCritical">Whether this hit is a critical.</param>
        /// <param name="critMultiplier">Critical damage multiplier (default 2x).</param>
        /// <returns>Final damage dealt.</returns>
        public static int CalculatePhysicalDamage(int attack, int defense,
                                                  bool isCritical = false,
                                                  float critMultiplier = 2.0f)
        {
            int baseDamage = Mathf.Max(MinDamage, attack - defense);

            if (isCritical)
            {
                baseDamage = Mathf.RoundToInt(baseDamage * critMultiplier);
            }

            return Mathf.Max(MinDamage, baseDamage);
        }

        /// <summary>
        /// Calculate magical damage: magicPower - (defense * 0.5), minimum 1.
        /// Magic partially ignores physical defense.
        /// </summary>
        /// <param name="magicPower">Spell's base damage.</param>
        /// <param name="defense">Defender's defense stat.</param>
        /// <param name="isCritical">Whether this hit is a critical.</param>
        /// <param name="critMultiplier">Critical damage multiplier (default 2x).</param>
        /// <returns>Final damage dealt.</returns>
        public static int CalculateMagicalDamage(int magicPower, int defense,
                                                 bool isCritical = false,
                                                 float critMultiplier = 2.0f)
        {
            int effectiveDefense = Mathf.RoundToInt(defense * 0.5f);
            int baseDamage = Mathf.Max(MinDamage, magicPower - effectiveDefense);

            if (isCritical)
            {
                baseDamage = Mathf.RoundToInt(baseDamage * critMultiplier);
            }

            return Mathf.Max(MinDamage, baseDamage);
        }

        /// <summary>
        /// Roll for a critical hit based on the given chance.
        /// </summary>
        /// <param name="critChance">Critical hit probability (0.0 to 1.0).</param>
        /// <returns>True if the hit is critical.</returns>
        public static bool RollCriticalHit(float critChance)
        {
            return Random.value < Mathf.Clamp01(critChance);
        }

        /// <summary>
        /// Apply a flat damage modifier (buff/debuff) to a base damage value.
        /// Positive values increase damage, negative values decrease it.
        /// Always enforces minimum 1 damage.
        /// </summary>
        /// <param name="baseDamage">The base damage before modifier.</param>
        /// <param name="modifier">Flat modifier to add/subtract.</param>
        /// <returns>Modified damage, minimum 1.</returns>
        public static int ApplyFlatModifier(int baseDamage, int modifier)
        {
            return Mathf.Max(MinDamage, baseDamage + modifier);
        }

        /// <summary>
        /// Apply a percentage damage modifier (buff/debuff) to a base damage value.
        /// 1.0 = no change, 1.5 = +50%, 0.5 = -50%.
        /// Always enforces minimum 1 damage.
        /// </summary>
        /// <param name="baseDamage">The base damage before modifier.</param>
        /// <param name="multiplier">Percentage multiplier (1.0 = neutral).</param>
        /// <returns>Modified damage, minimum 1.</returns>
        public static int ApplyPercentageModifier(int baseDamage, float multiplier)
        {
            return Mathf.Max(MinDamage, Mathf.RoundToInt(baseDamage * multiplier));
        }
    }
}
