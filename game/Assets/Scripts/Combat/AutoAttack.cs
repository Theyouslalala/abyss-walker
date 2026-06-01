using UnityEngine;
using AbyssWalker.Core;
using AbyssWalker.Entity;

namespace AbyssWalker.Combat
{
    /// <summary>
    /// MonoBehaviour that handles automatic attack behavior.
    /// Detects enemies within attack range (1 grid cell by default)
    /// and attacks them at a rate determined by attack speed.
    /// Attach to the player GameObject alongside Player.cs.
    /// </summary>
    public class AutoAttack : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private int attackRange = 1;
        [SerializeField] private float baseCooldown = 1.0f;

        [Header("References")]
        [SerializeField] private CombatManager combatManager;

        private Player player;
        private EntityManager entityManager;
        private Animator animator;
        private float cooldownTimer;

        /// <summary>Raised when an auto-attack fires. Passes the target Enemy.</summary>
        public event System.Action<Enemy> OnAutoAttackFired;

        /// <summary>Whether auto-attack is currently enabled.</summary>
        public bool IsEnabled { get; set; } = true;

        private void Awake()
        {
            player = GetComponent<Player>();
            animator = GetComponent<Animator>();
        }

        private void Start()
        {
            if (combatManager == null)
            {
                combatManager = FindObjectOfType<CombatManager>();
            }
            entityManager = FindObjectOfType<EntityManager>();
        }

        private void Update()
        {
            if (!IsEnabled || player == null || !player.Stats.IsAlive) return;

            cooldownTimer -= Time.deltaTime;

            if (cooldownTimer <= 0f)
            {
                if (TryAutoAttack())
                {
                    // Cooldown scales with attack speed stat
                    cooldownTimer = baseCooldown / Mathf.Max(0.1f, player.Stats.speed);
                }
            }
        }

        /// <summary>
        /// Attempt to find and attack the nearest enemy within range.
        /// </summary>
        /// <returns>True if an attack was performed.</returns>
        private bool TryAutoAttack()
        {
            if (entityManager == null) return false;

            Enemy target = entityManager.GetNearestEnemy(player.GridPosition, attackRange);
            if (target == null || !target.Stats.IsAlive) return false;

            PerformAttack(target);
            return true;
        }

        /// <summary>
        /// Execute an attack against the given target.
        /// Triggers visual feedback via Animator.
        /// </summary>
        private void PerformAttack(Enemy target)
        {
            if (combatManager != null)
            {
                combatManager.ProcessAttack(player.Stats, target.Stats);
            }

            // Trigger attack animation
            animator?.SetTrigger("AutoAttack");

            // Flip player sprite to face the target
            SpriteRenderer sr = GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                sr.flipX = target.transform.position.x < transform.position.x;
            }

            OnAutoAttackFired?.Invoke(target);

            Debug.Log($"[AutoAttack] Attacked {target.Type} at {target.GridPosition}");
        }

        /// <summary>
        /// Set the attack range (e.g., when equipping a ranged weapon).
        /// </summary>
        public void SetAttackRange(int range)
        {
            attackRange = Mathf.Max(1, range);
        }

        /// <summary>
        /// Set the base attack cooldown (e.g., when changing weapon speed).
        /// </summary>
        public void SetBaseCooldown(float cd)
        {
            baseCooldown = Mathf.Max(0.1f, cd);
        }
    }
}
