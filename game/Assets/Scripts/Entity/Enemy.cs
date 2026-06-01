using System;
using System.Collections;
using UnityEngine;
using AbyssWalker.Combat;
using AbyssWalker.Core;

namespace AbyssWalker.Entity
{
    /// <summary>
    /// Enemy types with distinct behavior patterns.
    /// Maps to Python-side behavior trees in ai/enemies/enemy_ai.py.
    /// </summary>
    public enum EnemyType
    {
        Skeleton,
        Goblin,
        ShadowMage,
        Boss
    }

    /// <summary>
    /// Enemy controller that receives AI decisions from the Python server
    /// and executes actions (move, attack, ranged attack) on the grid.
    /// </summary>
    public class Enemy : MonoBehaviour
    {
        [Header("Stats")]
        [SerializeField] private EntityStats stats = new EntityStats();

        [Header("Enemy Config")]
        [SerializeField] private EnemyType enemyType = EnemyType.Skeleton;
        [SerializeField] private int sightRange = 3;
        [SerializeField] private int attackRange = 1;
        [SerializeField] private int rangedAttackRange = 3;

        [Header("Movement")]
        [SerializeField] private float moveSpeed = 4f;

        [Header("Loot")]
        [SerializeField] private int expReward = 25;
        [SerializeField] private string[] possibleLoot;

        /// <summary>Unique identifier for this enemy instance.</summary>
        public int EnemyId { get; set; }

        /// <summary>Public read-only access to the enemy's stats.</summary>
        public EntityStats Stats => stats;

        /// <summary>The type of this enemy (Skeleton, Goblin, etc.).</summary>
        public EnemyType Type => enemyType;

        /// <summary>Current grid position of the enemy.</summary>
        public Vector2Int GridPosition { get; private set; }

        /// <summary>Raised when this enemy dies. Passes (EnemyId, expReward).</summary>
        public event Action<int, int> OnEnemyDeath;

        private bool isMoving;
        private Animator animator;
        private SpriteRenderer spriteRenderer;

        private void Awake()
        {
            animator = GetComponent<Animator>();
            spriteRenderer = GetComponent<SpriteRenderer>();
            stats.OnDeath += HandleDeath;
        }

        /// <summary>
        /// Initialize enemy with type-specific stats and grid position.
        /// </summary>
        public void Initialize(EnemyType type, Vector2Int startPos, int floorLevel = 1)
        {
            enemyType = type;
            GridPosition = startPos;

            // Scale stats by enemy type and floor level
            switch (type)
            {
                case EnemyType.Skeleton:
                    stats.maxHp = 30 + floorLevel * 10;
                    stats.attack = 8 + floorLevel * 2;
                    stats.defense = 3 + floorLevel;
                    stats.speed = 1.0f;
                    break;
                case EnemyType.Goblin:
                    stats.maxHp = 20 + floorLevel * 8;
                    stats.attack = 12 + floorLevel * 3;
                    stats.defense = 2 + floorLevel;
                    stats.speed = 1.5f;
                    break;
                case EnemyType.ShadowMage:
                    stats.maxHp = 25 + floorLevel * 8;
                    stats.attack = 15 + floorLevel * 4;
                    stats.defense = 2 + floorLevel;
                    stats.speed = 0.8f;
                    attackRange = rangedAttackRange;
                    break;
                case EnemyType.Boss:
                    stats.maxHp = 200 + floorLevel * 50;
                    stats.attack = 20 + floorLevel * 5;
                    stats.defense = 10 + floorLevel * 3;
                    stats.speed = 1.2f;
                    expReward = 100 + floorLevel * 30;
                    break;
            }

            stats.hp = stats.maxHp;
            stats.level = floorLevel;
            transform.position = FindObjectOfType<GridManager>()?.GridToWorld(startPos)
                                 ?? new Vector3(startPos.x, startPos.y, 0);
        }

        /// <summary>
        /// Execute an action received from the Python AI server.
        /// Called by EntityManager after receiving ai_decision messages.
        /// </summary>
        /// <param name="actionType">Action type string: "move", "attack", "ranged_attack", "idle".</param>
        /// <param name="targetPos">Target grid position for movement.</param>
        /// <param name="target">Target entity for attacks.</param>
        public void ExecuteAction(string actionType, Vector2Int targetPos = default, Player target = null)
        {
            if (!stats.IsAlive) return;

            switch (actionType)
            {
                case "move":
                    StartCoroutine(SmoothMove(targetPos));
                    break;
                case "attack":
                    AttackPlayer(target);
                    break;
                case "ranged_attack":
                    RangedAttackPlayer(target);
                    break;
                case "idle":
                default:
                    break;
            }
        }

        /// <summary>
        /// Smoothly move to a target grid cell.
        /// </summary>
        private IEnumerator SmoothMove(Vector2Int targetGrid)
        {
            if (isMoving) yield break;

            GridManager gridManager = FindObjectOfType<GridManager>();
            if (gridManager == null || !gridManager.IsWalkable(targetGrid)) yield break;

            // Don't move onto player or other enemies
            EntityManager entityManager = FindObjectOfType<EntityManager>();
            if (entityManager != null && entityManager.GetEnemyAt(targetGrid) != null) yield break;

            isMoving = true;
            Vector3 startPos = transform.position;
            Vector3 endPos = gridManager.GridToWorld(targetGrid);
            float t = 0f;

            // Flip sprite based on movement direction
            if (spriteRenderer != null && endPos.x != startPos.x)
            {
                spriteRenderer.flipX = endPos.x < startPos.x;
            }

            while (t < 1f)
            {
                t += moveSpeed * Time.deltaTime;
                transform.position = Vector3.Lerp(startPos, endPos, Mathf.Clamp01(t));
                yield return null;
            }

            transform.position = endPos;
            GridPosition = targetGrid;
            isMoving = false;
        }

        /// <summary>
        /// Melee attack the player. Range: 1 grid cell.
        /// </summary>
        private void AttackPlayer(Player player)
        {
            if (player == null || !player.Stats.IsAlive) return;

            int dist = Mathf.Abs(GridPosition.x - player.GridPosition.x)
                     + Mathf.Abs(GridPosition.y - player.GridPosition.y);

            if (dist <= attackRange)
            {
                CombatManager combatManager = FindObjectOfType<CombatManager>();
                combatManager?.ProcessAttack(stats, player.Stats);
                animator?.SetTrigger("Attack");
            }
        }

        /// <summary>
        /// Ranged attack the player. Used by ShadowMage type.
        /// </summary>
        private void RangedAttackPlayer(Player player)
        {
            if (player == null || !player.Stats.IsAlive) return;

            int dist = Mathf.Abs(GridPosition.x - player.GridPosition.x)
                     + Mathf.Abs(GridPosition.y - player.GridPosition.y);

            if (dist <= rangedAttackRange)
            {
                CombatManager combatManager = FindObjectOfType<CombatManager>();
                combatManager?.ProcessAttack(stats, player.Stats);
                animator?.SetTrigger("RangedAttack");
            }
        }

        /// <summary>
        /// Handle enemy death: drop loot, award EXP, notify systems via EventManager.
        /// </summary>
        private void HandleDeath(EntityStats deadStats)
        {
            // Award EXP to the player
            Player player = FindObjectOfType<Player>();
            player?.Stats.AddExp(expReward);

            // Drop loot
            if (possibleLoot != null && possibleLoot.Length > 0)
            {
                string loot = possibleLoot[UnityEngine.Random.Range(0, possibleLoot.Length)];
                player?.AddItem(loot);
            }

            // Notify systems
            OnEnemyDeath?.Invoke(EnemyId, expReward);
            EventManager em = FindObjectOfType<EventManager>();
            em?.RaiseEnemyDefeated(EnemyId);

            animator?.SetTrigger("Death");
            Destroy(gameObject, 0.5f);
        }

        private void OnDestroy()
        {
            stats.OnDeath -= HandleDeath;
        }
    }
}
