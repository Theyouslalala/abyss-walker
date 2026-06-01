using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AbyssWalker.Combat;
using AbyssWalker.Core;

namespace AbyssWalker.Entity
{
    /// <summary>
    /// Player controller with grid-based movement, auto-attack, skill usage, and inventory.
    /// Attach to the player GameObject.
    /// </summary>
    public class Player : MonoBehaviour
    {
        [Header("Stats")]
        [SerializeField] private EntityStats stats = new EntityStats();

        [Header("Movement")]
        [SerializeField] private float moveSpeed = 5f;
        [SerializeField] private float moveThreshold = 0.05f;

        [Header("Combat")]
        private float critChance = 0.05f;

        [Header("Equipment & Inventory")]
        private string equippedWeaponId;
        private string equippedArmorId;
        private string equippedAccessoryId;
        private List<string> learnedSkills = new List<string>();
        private int potions;
        private int gold;
        private int perception = 3;

        [Header("References")]
        [SerializeField] private GridManager gridManager;

        /// <summary>Public read-only access to the player's stats.</summary>
        public EntityStats Stats => stats;

        /// <summary>Current grid position of the player.</summary>
        public Vector2Int GridPosition { get; private set; }

        /// <summary>Player inventory holding item references.</summary>
        public List<string> Inventory { get; private set; } = new List<string>();

        /// <summary>Raised when the player moves to a new grid cell. Passes new grid position.</summary>
        public event Action<Vector2Int> OnPlayerMoved;

        private bool isMoving;
        private Animator animator;

        private void Awake()
        {
            animator = GetComponent<Animator>();
            stats.Initialize();

            stats.OnDeath += HandleDeath;
        }

        private void Start()
        {
            if (gridManager == null)
            {
                gridManager = FindObjectOfType<GridManager>();
            }

            // Snap to grid on spawn
            if (gridManager != null)
            {
                GridPosition = gridManager.WorldToGrid(transform.position);
                transform.position = gridManager.GridToWorld(GridPosition);
            }
        }

        private void Update()
        {
            if (!stats.IsAlive) return;

            HandleMovementInput();
        }

        /// <summary>
        /// Read directional input and initiate grid-based movement.
        /// Supports WASD / Arrow keys.
        /// </summary>
        private void HandleMovementInput()
        {
            if (isMoving) return;

            Vector2Int direction = Vector2Int.zero;

            if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow))
                direction = Vector2Int.up;
            else if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow))
                direction = Vector2Int.down;
            else if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow))
                direction = Vector2Int.left;
            else if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow))
                direction = Vector2Int.right;

            if (direction != Vector2Int.zero)
            {
                TryMove(direction);
            }
        }

        /// <summary>
        /// Attempt to move one cell in the given direction.
        /// Checks walkability via GridManager and resolves enemy collisions.
        /// </summary>
        private void TryMove(Vector2Int direction)
        {
            Vector2Int newPos = GridPosition + direction;

            if (gridManager == null || !gridManager.IsWalkable(newPos)) return;

            // Check if an enemy occupies the target cell
            EntityManager entityManager = FindObjectOfType<EntityManager>();
            if (entityManager != null)
            {
                Enemy blockingEnemy = entityManager.GetEnemyAt(newPos);
                if (blockingEnemy != null)
                {
                    // Can't walk into enemy; attack instead
                    PerformAutoAttack(blockingEnemy);
                    return;
                }
            }

            StartCoroutine(SmoothMove(newPos));
        }

        /// <summary>
        /// Smoothly interpolate from current position to the target grid cell.
        /// </summary>
        private IEnumerator SmoothMove(Vector2Int targetGrid)
        {
            isMoving = true;
            Vector3 startPos = transform.position;
            Vector3 endPos = gridManager.GridToWorld(targetGrid);
            float t = 0f;

            animator?.SetTrigger("Move");

            while (t < 1f)
            {
                t += moveSpeed * Time.deltaTime;
                transform.position = Vector3.Lerp(startPos, endPos, Mathf.Clamp01(t));
                yield return null;
            }

            transform.position = endPos;
            GridPosition = targetGrid;
            isMoving = false;

            OnPlayerMoved?.Invoke(GridPosition);
        }

        /// <summary>
        /// Activate a skill by index from the SkillSystem.
        /// Called from UI button or keybind.
        /// </summary>
        /// <param name="skillIndex">Index into the available skills list.</param>
        public void UseSkill(int skillIndex)
        {
            if (!stats.IsAlive) return;

            SkillSystem skillSystem = FindObjectOfType<SkillSystem>();
            if (skillSystem == null) return;

            EntityManager entityManager = FindObjectOfType<EntityManager>();
            Enemy target = entityManager?.GetNearestEnemy(GridPosition, 3);

            if (target != null)
            {
                skillSystem.ExecuteSkill(skillIndex, this, target);
                animator?.SetTrigger("Skill");
            }
        }

        /// <summary>
        /// Add an item to the player's inventory.
        /// </summary>
        public void AddItem(string itemId)
        {
            Inventory.Add(itemId);
        }

        /// <summary>
        /// Remove an item from the player's inventory.
        /// </summary>
        public bool RemoveItem(string itemId)
        {
            return Inventory.Remove(itemId);
        }

        // ========== Stats Accessors (used by HUD, Events, etc.) ==========

        public int GetCurrentHP() => stats.hp;
        public int GetMaxHP() => stats.maxHp;
        public int GetGold() => gold;
        public int GetPerception() => perception;
        public int GetDEF() => stats.defense;

        public void AddGold(int amount) { gold += Mathf.Max(0, amount); }
        public bool SpendGold(int amount)
        {
            if (gold < amount) return false;
            gold -= amount;
            return true;
        }

        public void AddPotion() { potions++; }

        public void EquipWeapon(string id, string name, int atkBonus)
        {
            equippedWeaponId = id;
            stats.attack += atkBonus;
        }

        public void EquipArmor(string id, string name, int defBonus)
        {
            equippedArmorId = id;
            stats.defense += defBonus;
        }

        public void EquipAccessory(string id, string name, int value)
        {
            equippedAccessoryId = id;
        }

        public void LearnSkill(string skillId)
        {
            if (!learnedSkills.Contains(skillId))
                learnedSkills.Add(skillId);
        }

        public void IncreaseMaxHP(int amount)
        {
            stats.maxHp += Mathf.Max(0, amount);
        }

        public void Heal(int amount) { stats.Heal(amount); }

        public void IncreaseATK(int amount) { stats.attack += Mathf.Max(0, amount); }
        public void IncreaseDEF(int amount) { stats.defense += Mathf.Max(0, amount); }
        public void IncreaseSpeed(float amount) { stats.speed += Mathf.Max(0f, amount); }
        public void IncreaseCritChance(float amount) { critChance += Mathf.Max(0f, amount); }

        public void TakeDamage(int damage) { stats.TakeDamage(damage); }

        public void ApplyDebuff(string debuffType, int duration)
        {
            // Stub — debuff system can be expanded later
        }

        /// <summary>
        /// Handle player death: disable controls, trigger death animation,
        /// and notify the EventManager so GameManager can transition to GameOver.
        /// </summary>
        private void HandleDeath(EntityStats deadStats)
        {
            animator?.SetTrigger("Death");
            EventManager em = FindObjectOfType<EventManager>();
            em?.RaisePlayerDeath();
        }

        private void OnDestroy()
        {
            stats.OnDeath -= HandleDeath;
        }
    }
}
