using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using AbyssWalker.Core;

namespace AbyssWalker.Entity
{
    /// <summary>
    /// Central registry for all active entities (player + enemies).
    /// Handles spawning, despawning, and spatial queries on the grid.
    /// </summary>
    public class EntityManager : MonoBehaviour
    {
        [Header("Prefabs")]
        [SerializeField] private GameObject skeletonPrefab;
        [SerializeField] private GameObject goblinPrefab;
        [SerializeField] private GameObject shadowMagePrefab;
        [SerializeField] private GameObject bossPrefab;

        [Header("References")]
        [SerializeField] private GridManager gridManager;

        private readonly Dictionary<int, Enemy> activeEnemies = new Dictionary<int, Enemy>();
        private Player player;
        private int nextEnemyId = 1;

        /// <summary>Reference to the player entity.</summary>
        public Player Player => player;

        /// <summary>All currently active enemies.</summary>
        public IReadOnlyDictionary<int, Enemy> ActiveEnemies => activeEnemies;

        /// <summary>Total number of living enemies.</summary>
        public int EnemyCount => activeEnemies.Values.Count(e => e != null && e.Stats.IsAlive);

        private void Awake()
        {
            player = FindObjectOfType<Player>();
        }

        private void Start()
        {
            if (gridManager == null)
            {
                gridManager = FindObjectOfType<GridManager>();
            }
        }

        /// <summary>
        /// Spawn an enemy of the given type at a grid position.
        /// Returns the spawned Enemy component, or null if the prefab is missing.
        /// </summary>
        /// <param name="type">Enemy type to spawn.</param>
        /// <param name="gridPos">Grid position to spawn at.</param>
        /// <param name="floorLevel">Current floor level for stat scaling.</param>
        public Enemy SpawnEnemy(EnemyType type, Vector2Int gridPos, int floorLevel = 1)
        {
            GameObject prefab = GetPrefabForType(type);
            if (prefab == null)
            {
                Debug.LogWarning($"[EntityManager] No prefab for enemy type: {type}");
                return null;
            }

            Vector3 worldPos = gridManager != null
                ? gridManager.GridToWorld(gridPos)
                : new Vector3(gridPos.x, gridPos.y, 0);

            GameObject instance = Instantiate(prefab, worldPos, Quaternion.identity);
            Enemy enemy = instance.GetComponent<Enemy>();

            if (enemy == null)
            {
                Debug.LogError($"[EntityManager] Prefab for {type} has no Enemy component.");
                Destroy(instance);
                return null;
            }

            enemy.EnemyId = nextEnemyId++;
            enemy.Initialize(type, gridPos, floorLevel);
            enemy.OnEnemyDeath += HandleEnemyDeath;
            activeEnemies[enemy.EnemyId] = enemy;

            return enemy;
        }

        /// <summary>
        /// Remove and destroy a specific enemy.
        /// </summary>
        public void DespawnEnemy(int enemyId)
        {
            if (activeEnemies.TryGetValue(enemyId, out Enemy enemy))
            {
                enemy.OnEnemyDeath -= HandleEnemyDeath;
                activeEnemies.Remove(enemyId);

                if (enemy != null)
                {
                    Destroy(enemy.gameObject);
                }
            }
        }

        /// <summary>
        /// Remove and destroy all enemies.
        /// </summary>
        public void DespawnAllEnemies()
        {
            foreach (var kvp in activeEnemies)
            {
                if (kvp.Value != null)
                {
                    kvp.Value.OnEnemyDeath -= HandleEnemyDeath;
                    Destroy(kvp.Value.gameObject);
                }
            }
            activeEnemies.Clear();
            nextEnemyId = 1;
        }

        /// <summary>
        /// Get the enemy occupying a specific grid cell, or null if empty.
        /// </summary>
        public Enemy GetEnemyAt(Vector2Int gridPos)
        {
            foreach (var kvp in activeEnemies)
            {
                if (kvp.Value != null
                    && kvp.Value.Stats.IsAlive
                    && kvp.Value.GridPosition == gridPos)
                {
                    return kvp.Value;
                }
            }
            return null;
        }

        /// <summary>
        /// Get the nearest living enemy within a given Manhattan distance range.
        /// </summary>
        /// <param name="fromPos">Origin grid position.</param>
        /// <param name="maxRange">Maximum Manhattan distance (inclusive).</param>
        public Enemy GetNearestEnemy(Vector2Int fromPos, int maxRange)
        {
            Enemy nearest = null;
            int bestDist = int.MaxValue;

            foreach (var kvp in activeEnemies)
            {
                Enemy e = kvp.Value;
                if (e == null || !e.Stats.IsAlive) continue;

                int dist = Mathf.Abs(e.GridPosition.x - fromPos.x)
                         + Mathf.Abs(e.GridPosition.y - fromPos.y);

                if (dist <= maxRange && dist < bestDist)
                {
                    bestDist = dist;
                    nearest = e;
                }
            }

            return nearest;
        }

        /// <summary>
        /// Get all living enemies within a given Manhattan distance range.
        /// </summary>
        public List<Enemy> GetEnemiesInRange(Vector2Int fromPos, int maxRange)
        {
            List<Enemy> result = new List<Enemy>();

            foreach (var kvp in activeEnemies)
            {
                Enemy e = kvp.Value;
                if (e == null || !e.Stats.IsAlive) continue;

                int dist = Mathf.Abs(e.GridPosition.x - fromPos.x)
                         + Mathf.Abs(e.GridPosition.y - fromPos.y);

                if (dist <= maxRange)
                {
                    result.Add(e);
                }
            }

            return result;
        }

        /// <summary>
        /// Get an enemy by its unique ID.
        /// </summary>
        public Enemy GetEnemyById(int enemyId)
        {
            activeEnemies.TryGetValue(enemyId, out Enemy enemy);
            return enemy;
        }

        /// <summary>
        /// Callback when an enemy dies naturally (HP reached 0).
        /// </summary>
        private void HandleEnemyDeath(int enemyId, int expReward)
        {
            DespawnEnemy(enemyId);
        }

        private GameObject GetPrefabForType(EnemyType type)
        {
            return type switch
            {
                EnemyType.Skeleton => skeletonPrefab,
                EnemyType.Goblin => goblinPrefab,
                EnemyType.ShadowMage => shadowMagePrefab,
                EnemyType.Boss => bossPrefab,
                _ => skeletonPrefab,
            };
        }
    }
}
