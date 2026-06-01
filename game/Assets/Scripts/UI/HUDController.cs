using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace AbyssWalker.UI
{
    /// <summary>
    /// Controls the in-game HUD: player HP, floor indicator, enemy HP bars,
    /// skill cooldowns, and currency display.
    /// </summary>
    public class HUDController : MonoBehaviour
    {
        [Header("Player Info")]
        [SerializeField] private Slider playerHPBar;
        [SerializeField] private TextMeshProUGUI playerHPText;
        [SerializeField] private Image playerHPFill;

        [Header("Floor Indicator")]
        [SerializeField] private TextMeshProUGUI floorText;

        [Header("Currency")]
        [SerializeField] private TextMeshProUGUI goldText;
        [SerializeField] private TextMeshProUGUI soulText;

        [Header("Skill Cooldowns")]
        [SerializeField] private SkillCooldownSlot[] skillSlots;

        [Header("Enemy HP")]
        [SerializeField] private EnemyHPBar[] enemyHPBars;
        [SerializeField] private int maxTrackedEnemies = 3;

        [Header("HP Bar Colors")]
        [SerializeField] private Color hpHighColor = new Color(0.2f, 0.8f, 0.2f);
        [SerializeField] private Color hpMidColor = new Color(0.9f, 0.9f, 0.1f);
        [SerializeField] private Color hpLowColor = new Color(0.9f, 0.2f, 0.2f);
        [SerializeField] private float hpLowThreshold = 0.25f;
        [SerializeField] private float hpMidThreshold = 0.5f;

        private PlayerController trackedPlayer;

        private void Start()
        {
            // Initialize all enemy HP bars as hidden
            if (enemyHPBars != null)
            {
                foreach (var bar in enemyHPBars)
                {
                    if (bar != null) bar.gameObject.SetActive(false);
                }
            }
        }

        private void Update()
        {
            if (trackedPlayer == null)
            {
                trackedPlayer = FindObjectOfType<PlayerController>();
                if (trackedPlayer == null) return;
            }

            UpdatePlayerHP();
            UpdateCurrency();
        }

        /// <summary>
        /// Sets the floor display text.
        /// </summary>
        public void SetFloor(int floor)
        {
            if (floorText != null)
            {
                floorText.text = $"Floor {floor}";
            }
        }

        /// <summary>
        /// Updates a skill cooldown slot.
        /// </summary>
        public void UpdateSkillCooldown(int slotIndex, float cooldownProgress, Sprite icon)
        {
            if (skillSlots == null || slotIndex < 0 || slotIndex >= skillSlots.Length) return;
            skillSlots[slotIndex].UpdateCooldown(cooldownProgress, icon);
        }

        /// <summary>
        /// Updates the enemy HP bar at the given slot index.
        /// </summary>
        public void UpdateEnemyHP(int slotIndex, string enemyName, float currentHP, float maxHP)
        {
            if (enemyHPBars == null || slotIndex < 0 || slotIndex >= enemyHPBars.Length) return;

            EnemyHPBar bar = enemyHPBars[slotIndex];
            if (bar == null) return;

            bar.gameObject.SetActive(true);
            bar.SetHP(enemyName, currentHP, maxHP);
        }

        /// <summary>
        /// Hides the enemy HP bar at the given slot index.
        /// </summary>
        public void HideEnemyHP(int slotIndex)
        {
            if (enemyHPBars == null || slotIndex < 0 || slotIndex >= enemyHPBars.Length) return;
            enemyHPBars[slotIndex]?.gameObject.SetActive(false);
        }

        /// <summary>
        /// Hides all enemy HP bars.
        /// </summary>
        public void HideAllEnemyHP()
        {
            if (enemyHPBars == null) return;
            foreach (var bar in enemyHPBars)
            {
                if (bar != null) bar.gameObject.SetActive(false);
            }
        }

        private void UpdatePlayerHP()
        {
            if (trackedPlayer == null) return;

            float current = trackedPlayer.GetCurrentHP();
            float max = trackedPlayer.GetMaxHP();
            float ratio = max > 0 ? current / max : 0;

            if (playerHPBar != null)
            {
                playerHPBar.value = ratio;
            }

            if (playerHPText != null)
            {
                playerHPText.text = $"{Mathf.CeilToInt(current)} / {Mathf.CeilToInt(max)}";
            }

            if (playerHPFill != null)
            {
                playerHPFill.color = GetHPColor(ratio);
            }
        }

        private void UpdateCurrency()
        {
            if (trackedPlayer == null) return;

            if (goldText != null)
            {
                goldText.text = trackedPlayer.GetGold().ToString();
            }

            if (soulText != null)
            {
                int souls = Meta.ProgressManager.Instance != null
                    ? Meta.ProgressManager.Instance.GetSouls()
                    : 0;
                soulText.text = souls.ToString();
            }
        }

        private Color GetHPColor(float ratio)
        {
            if (ratio <= hpLowThreshold) return hpLowColor;
            if (ratio <= hpMidThreshold) return hpMidColor;
            return hpHighColor;
        }
    }

    /// <summary>
    /// A single skill cooldown display slot in the HUD.
    /// </summary>
    [System.Serializable]
    public class SkillCooldownSlot
    {
        public Image iconImage;
        public Image cooldownOverlay;
        public TextMeshProUGUI cooldownText;

        public void UpdateCooldown(float progress, Sprite icon)
        {
            if (iconImage != null && icon != null)
            {
                iconImage.sprite = icon;
            }

            if (cooldownOverlay != null)
            {
                cooldownOverlay.fillAmount = progress;
            }

            if (cooldownText != null)
            {
                if (progress > 0)
                {
                    cooldownText.gameObject.SetActive(true);
                    cooldownText.text = Mathf.CeilToInt(progress * 10f).ToString(); // rough seconds
                }
                else
                {
                    cooldownText.gameObject.SetActive(false);
                }
            }
        }
    }

    /// <summary>
    /// Displays an enemy's HP bar in the HUD.
    /// </summary>
    [System.Serializable]
    public class EnemyHPBar
    {
        public GameObject gameObject;
        public Slider hpSlider;
        public TextMeshProUGUI nameText;
        public TextMeshProUGUI hpText;

        public void SetHP(string enemyName, float currentHP, float maxHP)
        {
            if (hpSlider != null)
            {
                hpSlider.value = maxHP > 0 ? currentHP / maxHP : 0;
            }
            if (nameText != null)
            {
                nameText.text = enemyName;
            }
            if (hpText != null)
            {
                hpText.text = $"{Mathf.CeilToInt(currentHP)} / {Mathf.CeilToInt(maxHP)}";
            }
        }
    }
}
