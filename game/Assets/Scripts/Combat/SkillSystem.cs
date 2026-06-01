using System;
using System.Collections.Generic;
using UnityEngine;
using AbyssWalker.Core;
using AbyssWalker.Entity;

namespace AbyssWalker.Combat
{
    /// <summary>
    /// Types of effects a skill can apply.
    /// </summary>
    public enum SkillEffectType
    {
        Damage,
        Heal,
        Shield,
        Buff,
        Debuff,
        AreaOfEffect
    }

    /// <summary>
    /// Serializable data container for a skill definition.
    /// </summary>
    [System.Serializable]
    public class SkillData
    {
        public string id;
        public string skillName;
        public string description;
        public int damage;
        public int healAmount;
        public int range;
        public float cooldown;
        public int energyCost;
        public SkillEffectType effectType;
        public bool targetsSelf;

        /// <summary>
        /// Create a copy of this skill data (for runtime modification).
        /// </summary>
        public SkillData Clone()
        {
            return (SkillData)this.MemberwiseClone();
        }
    }

    /// <summary>
    /// Tracks cooldown state for a single skill slot.
    /// </summary>
    [System.Serializable]
    public class SkillCooldown
    {
        public string skillId;
        public float remainingCooldown;

        public bool IsReady => remainingCooldown <= 0f;
    }

    /// <summary>
    /// Manages skill definitions, cooldown tracking, and skill execution.
    /// </summary>
    public class SkillSystem : MonoBehaviour
    {
        [Header("Skill Database")]
        [SerializeField] private List<SkillData> skillDatabase = new List<SkillData>();

        [Header("References")]
        [SerializeField] private CombatManager combatManager;

        /// <summary>Cooldown timers keyed by skill ID.</summary>
        private readonly Dictionary<string, SkillCooldown> cooldowns = new Dictionary<string, SkillCooldown>();

        /// <summary>Available skills for the current player (by index).</summary>
        private readonly List<SkillData> playerSkills = new List<SkillData>();

        /// <summary>Raised when a skill is used. Passes (skillId, caster, target).</summary>
        public event Action<string, EntityStats, EntityStats> OnSkillUsed;

        /// <summary>Raised when a skill goes on cooldown. Passes (skillId, cooldownDuration).</summary>
        public event Action<string, float> OnSkillCooldownStarted;

        private void Awake()
        {
            if (combatManager == null)
            {
                combatManager = FindObjectOfType<CombatManager>();
            }

            InitializeDefaultSkills();
        }

        private void Update()
        {
            UpdateCooldowns();
        }

        /// <summary>
        /// Initialize the skill database with predefined skills.
        /// </summary>
        private void InitializeDefaultSkills()
        {
            if (skillDatabase.Count > 0) return; // Already configured via inspector

            skillDatabase.Add(new SkillData
            {
                id = "slash",
                skillName = "Slash",
                description = "A powerful melee slash dealing 2x attack damage.",
                damage = 0, // Uses multiplier instead
                range = 1,
                cooldown = 2.0f,
                energyCost = 10,
                effectType = SkillEffectType.Damage,
                targetsSelf = false
            });

            skillDatabase.Add(new SkillData
            {
                id = "heal",
                skillName = "Heal",
                description = "Restore 30 HP to self.",
                healAmount = 30,
                range = 0,
                cooldown = 5.0f,
                energyCost = 15,
                effectType = SkillEffectType.Heal,
                targetsSelf = true
            });

            skillDatabase.Add(new SkillData
            {
                id = "fireball",
                skillName = "Fireball",
                description = "Launch a fireball dealing 25 magic damage at range 3.",
                damage = 25,
                range = 3,
                cooldown = 3.0f,
                energyCost = 20,
                effectType = SkillEffectType.Damage,
                targetsSelf = false
            });

            skillDatabase.Add(new SkillData
            {
                id = "shield",
                skillName = "Shield",
                description = "Increase defense by 50% for 5 seconds.",
                damage = 0,
                range = 0,
                cooldown = 8.0f,
                energyCost = 25,
                effectType = SkillEffectType.Shield,
                targetsSelf = true
            });

            skillDatabase.Add(new SkillData
            {
                id = "whirlwind",
                skillName = "Whirlwind",
                description = "Spin attack hitting all enemies within 1 cell.",
                damage = 15,
                range = 1,
                cooldown = 4.0f,
                energyCost = 18,
                effectType = SkillEffectType.AreaOfEffect,
                targetsSelf = false
            });
        }

        /// <summary>
        /// Execute a skill by index from the player's available skills.
        /// </summary>
        /// <param name="skillIndex">Index into playerSkills list.</param>
        /// <param name="caster">The player casting the skill.</param>
        /// <param name="target">The target enemy (null for self-targeted skills).</param>
        public void ExecuteSkill(int skillIndex, Player caster, Enemy target)
        {
            if (skillIndex < 0 || skillIndex >= skillDatabase.Count)
            {
                Debug.LogWarning($"[SkillSystem] Invalid skill index: {skillIndex}");
                return;
            }

            SkillData skill = skillDatabase[skillIndex];
            ExecuteSkill(skill, caster, target);
        }

        /// <summary>
        /// Execute a skill by its ID.
        /// </summary>
        public void ExecuteSkill(string skillId, Player caster, Enemy target)
        {
            SkillData skill = skillDatabase.Find(s => s.id == skillId);
            if (skill == null)
            {
                Debug.LogWarning($"[SkillSystem] Skill not found: {skillId}");
                return;
            }

            ExecuteSkill(skill, caster, target);
        }

        /// <summary>
        /// Core skill execution logic: check cooldown, apply effects, start cooldown.
        /// </summary>
        private void ExecuteSkill(SkillData skill, Player caster, Enemy target)
        {
            if (caster == null || !caster.Stats.IsAlive) return;

            // Check cooldown
            if (cooldowns.TryGetValue(skill.id, out SkillCooldown cd) && !cd.IsReady)
            {
                Debug.Log($"[SkillSystem] {skill.skillName} is on cooldown ({cd.remainingCooldown:F1}s)");
                return;
            }

            // Check range for targeted skills
            if (!skill.targetsSelf && target != null)
            {
                int dist = Mathf.Abs(caster.GridPosition.x - target.GridPosition.x)
                         + Mathf.Abs(caster.GridPosition.y - target.GridPosition.y);

                if (dist > skill.range)
                {
                    Debug.Log($"[SkillSystem] Target out of range for {skill.skillName}");
                    return;
                }
            }

            // Apply effect
            switch (skill.effectType)
            {
                case SkillEffectType.Damage:
                    ApplyDamageSkill(skill, caster, target);
                    break;
                case SkillEffectType.Heal:
                    ApplyHealSkill(skill, caster);
                    break;
                case SkillEffectType.Shield:
                    ApplyShieldSkill(skill, caster);
                    break;
                case SkillEffectType.AreaOfEffect:
                    ApplyAOESkill(skill, caster);
                    break;
                case SkillEffectType.Buff:
                case SkillEffectType.Debuff:
                    // Placeholder for buff/debuff system
                    Debug.Log($"[SkillSystem] {skill.skillName} effect not yet implemented");
                    break;
            }

            // Start cooldown
            SetCooldown(skill.id, skill.cooldown);
            OnSkillUsed?.Invoke(skill.id, caster.Stats, target?.Stats);
        }

        /// <summary>
        /// Apply a damage skill to a single target.
        /// </summary>
        private void ApplyDamageSkill(SkillData skill, Player caster, Enemy target)
        {
            if (target == null || !target.Stats.IsAlive) return;

            // Use caster's attack as base if skill damage is 0 (e.g., Slash)
            int power = skill.damage > 0 ? skill.damage : caster.Stats.attack * 2;

            bool isCrit = DamageCalculator.RollCriticalHit(0.05f);
            combatManager?.ProcessMagicalAttack(caster.Stats, target.Stats, power, isCrit);
        }

        /// <summary>
        /// Apply a heal skill to the caster.
        /// </summary>
        private void ApplyHealSkill(SkillData skill, Player caster)
        {
            combatManager?.ProcessHeal(caster.Stats, caster.Stats, skill.healAmount);
        }

        /// <summary>
        /// Apply a shield buff to the caster (placeholder: temporarily increase defense).
        /// </summary>
        private void ApplyShieldSkill(SkillData skill, Player caster)
        {
            // Simplified: heal a small amount as a shield proxy
            // Full implementation would use a buff system with timed defense increase
            caster.Stats.Heal(10);
            Debug.Log("[SkillSystem] Shield activated (defense buff placeholder)");
        }

        /// <summary>
        /// Apply an AoE skill hitting all enemies in range.
        /// </summary>
        private void ApplyAOESkill(SkillData skill, Player caster)
        {
            EntityManager entityManager = FindObjectOfType<EntityManager>();
            if (entityManager == null) return;

            List<Enemy> targets = entityManager.GetEnemiesInRange(caster.GridPosition, skill.range);
            foreach (Enemy enemy in targets)
            {
                if (enemy.Stats.IsAlive)
                {
                    bool isCrit = DamageCalculator.RollCriticalHit(0.05f);
                    combatManager?.ProcessMagicalAttack(caster.Stats, enemy.Stats, skill.damage, isCrit);
                }
            }
        }

        /// <summary>
        /// Set a skill on cooldown.
        /// </summary>
        private void SetCooldown(string skillId, float duration)
        {
            if (!cooldowns.ContainsKey(skillId))
            {
                cooldowns[skillId] = new SkillCooldown { skillId = skillId };
            }

            cooldowns[skillId].remainingCooldown = duration;
            OnSkillCooldownStarted?.Invoke(skillId, duration);
        }

        /// <summary>
        /// Update all active cooldowns.
        /// </summary>
        private void UpdateCooldowns()
        {
            foreach (var kvp in cooldowns)
            {
                if (kvp.Value.remainingCooldown > 0f)
                {
                    kvp.Value.remainingCooldown -= Time.deltaTime;
                }
            }
        }

        /// <summary>
        /// Check if a skill is ready to use (not on cooldown).
        /// </summary>
        public bool IsSkillReady(string skillId)
        {
            if (cooldowns.TryGetValue(skillId, out SkillCooldown cd))
            {
                return cd.IsReady;
            }
            return true;
        }

        /// <summary>
        /// Get the remaining cooldown for a skill.
        /// </summary>
        public float GetRemainingCooldown(string skillId)
        {
            if (cooldowns.TryGetValue(skillId, out SkillCooldown cd))
            {
                return Mathf.Max(0f, cd.remainingCooldown);
            }
            return 0f;
        }

        /// <summary>
        /// Get a skill definition by ID.
        /// </summary>
        public SkillData GetSkillData(string skillId)
        {
            return skillDatabase.Find(s => s.id == skillId);
        }

        /// <summary>
        /// Get the full skill database.
        /// </summary>
        public IReadOnlyList<SkillData> GetAllSkills()
        {
            return skillDatabase;
        }

        /// <summary>
        /// Add a custom skill to the database at runtime.
        /// </summary>
        public void RegisterSkill(SkillData skill)
        {
            if (skillDatabase.Exists(s => s.id == skill.id))
            {
                Debug.LogWarning($"[SkillSystem] Skill '{skill.id}' already registered.");
                return;
            }
            skillDatabase.Add(skill);
        }
    }
}
