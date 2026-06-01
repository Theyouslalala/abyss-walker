using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections.Generic;

namespace AbyssWalker.UI
{
    /// <summary>
    /// Controls the main menu flow: title screen, class selection,
    /// permanent upgrades screen, and settings.
    /// </summary>
    public class MenuController : MonoBehaviour
    {
        [Header("Panels")]
        [SerializeField] private GameObject mainMenuPanel;
        [SerializeField] private GameObject classSelectPanel;
        [SerializeField] private GameObject upgradesPanel;
        [SerializeField] private GameObject settingsPanel;
        [SerializeField] private GameObject runSummaryPanel;

        [Header("Main Menu Buttons")]
        [SerializeField] private Button newGameButton;
        [SerializeField] private Button continueButton;
        [SerializeField] private Button upgradesButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button quitButton;

        [Header("Class Selection")]
        [SerializeField] private Transform classButtonContainer;
        [SerializeField] private GameObject classButtonPrefab;
        [SerializeField] private TextMeshProUGUI classDescriptionText;
        [SerializeField] private Image classIconImage;
        [SerializeField] private TextMeshProUGUI classStatsText;
        [SerializeField] private Button startRunButton;
        [SerializeField] private Button classBackButton;

        [Header("Upgrades Screen")]
        [SerializeField] private Transform upgradeContainer;
        [SerializeField] private GameObject upgradeRowPrefab;
        [SerializeField] private TextMeshProUGUI soulsDisplayText;
        [SerializeField] private Button upgradesBackButton;

        [Header("Settings")]
        [SerializeField] private Slider masterVolumeSlider;
        [SerializeField] private Slider sfxVolumeSlider;
        [SerializeField] private Slider musicVolumeSlider;
        [SerializeField] private Button settingsBackButton;
        [SerializeField] private Button deleteSaveButton;

        [Header("Run Summary")]
        [SerializeField] private TextMeshProUGUI summaryText;
        [SerializeField] private Button summaryContinueButton;

        [Header("Game Scene")]
        [SerializeField] private string gameSceneName = "Game";

        private string selectedClassName = "Warrior";
        private Meta.ProgressManager progressManager;
        private Meta.UnlockSystem unlockSystem;

        private void Start()
        {
            progressManager = Meta.ProgressManager.Instance;
            unlockSystem = FindObjectOfType<Meta.UnlockSystem>();

            SetupButtons();
            ShowMainMenu();
        }

        private void SetupButtons()
        {
            newGameButton?.onClick.AddListener(OnNewGameClicked);
            continueButton?.onClick.AddListener(OnContinueClicked);
            upgradesButton?.onClick.AddListener(OnUpgradesClicked);
            settingsButton?.onClick.AddListener(OnSettingsClicked);
            quitButton?.onClick.AddListener(OnQuitClicked);

            startRunButton?.onClick.AddListener(OnStartRunClicked);
            classBackButton?.onClick.AddListener(ShowMainMenu);

            upgradesBackButton?.onClick.AddListener(ShowMainMenu);
            settingsBackButton?.onClick.AddListener(ShowMainMenu);
            deleteSaveButton?.onClick.AddListener(OnDeleteSaveClicked);

            summaryContinueButton?.onClick.AddListener(ShowMainMenu);

            masterVolumeSlider?.onValueChanged.AddListener(OnMasterVolumeChanged);
            sfxVolumeSlider?.onValueChanged.AddListener(OnSFXVolumeChanged);
            musicVolumeSlider?.onValueChanged.AddListener(OnMusicVolumeChanged);
        }

        // ── Panel Navigation ──

        public void ShowMainMenu()
        {
            SetActivePanel(mainMenuPanel);

            // Show continue button only if save exists
            if (continueButton != null)
            {
                continueButton.gameObject.SetActive(progressManager != null && progressManager.HasSaveData());
            }
        }

        public void ShowClassSelect()
        {
            SetActivePanel(classSelectPanel);
            PopulateClassSelection();
        }

        public void ShowUpgrades()
        {
            SetActivePanel(upgradesPanel);
            PopulateUpgrades();
        }

        public void ShowSettings()
        {
            SetActivePanel(settingsPanel);
            LoadSettingsUI();
        }

        public void ShowRunSummary(Meta.RunSummary summary)
        {
            SetActivePanel(runSummaryPanel);

            if (summaryText != null)
            {
                summaryText.text = $"Run Complete!\n\n" +
                    $"Class: {summary.className}\n" +
                    $"Floor Reached: {summary.floorReached}\n" +
                    $"Enemies Killed: {summary.enemiesKilled}\n" +
                    $"Bosses Killed: {summary.bossesKilled}\n" +
                    $"Gold Earned: {summary.goldEarned}\n" +
                    $"Abyss Souls Earned: {summary.soulsEarned}\n" +
                    $"Play Time: {summary.GetPlayTimeFormatted()}\n" +
                    $"{(summary.isVictory ? "VICTORY!" : "You have fallen...")}";
            }
        }

        // ── Button Handlers ──

        private void OnNewGameClicked()
        {
            ShowClassSelect();
        }

        private void OnContinueClicked()
        {
            // Load saved class and start
            SceneManager.LoadScene(gameSceneName);
        }

        private void OnUpgradesClicked()
        {
            ShowUpgrades();
        }

        private void OnSettingsClicked()
        {
            ShowSettings();
        }

        private void OnQuitClicked()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private void OnStartRunClicked()
        {
            Meta.RunManager rm = Meta.RunManager.Instance;
            if (rm != null)
            {
                rm.SetSelectedClass(selectedClassName);
            }

            SceneManager.LoadScene(gameSceneName);
        }

        private void OnDeleteSaveClicked()
        {
            if (progressManager != null)
            {
                progressManager.DeleteSave();
                ShowMainMenu();
            }
        }

        // ── Class Selection ──

        private void PopulateClassSelection()
        {
            if (unlockSystem == null || classButtonContainer == null) return;

            List<Meta.ClassDefinition> classes = unlockSystem.GetAvailableClasses();

            // Clear existing buttons
            foreach (Transform child in classButtonContainer)
            {
                Destroy(child.gameObject);
            }

            foreach (var classDef in classes)
            {
                GameObject btnObj = Instantiate(classButtonPrefab, classButtonContainer);
                Button btn = btnObj.GetComponent<Button>();
                TextMeshProUGUI btnText = btnObj.GetComponentInChildren<TextMeshProUGUI>();

                if (btnText != null) btnText.text = classDef.className;

                string capturedName = classDef.className;
                btn?.onClick.AddListener(() => SelectClass(capturedName, classDef));
            }

            // Select first class by default
            if (classes.Count > 0)
            {
                SelectClass(classes[0].className, classes[0]);
            }
        }

        private void SelectClass(string className, Meta.ClassDefinition classDef)
        {
            selectedClassName = className;

            if (classDescriptionText != null)
            {
                classDescriptionText.text = classDef.description;
            }

            if (classIconImage != null && classDef.classIcon != null)
            {
                classIconImage.sprite = classDef.classIcon;
            }

            if (classStatsText != null)
            {
                classStatsText.text = $"HP: {classDef.baseHP}  |  ATK: {classDef.baseATK}  |  " +
                    $"DEF: {classDef.baseDEF}  |  SPD: {classDef.baseSpeed}\n" +
                    $"Crit: {classDef.baseCrit}%  |  Perception: {classDef.basePerception}";
            }
        }

        // ── Upgrades Screen ──

        private void PopulateUpgrades()
        {
            if (progressManager == null || upgradeContainer == null) return;

            // Clear existing
            foreach (Transform child in upgradeContainer)
            {
                Destroy(child.gameObject);
            }

            UpdateSoulsDisplay();

            string[] upgradeIds = {
                "hp_upgrade", "atk_upgrade", "def_upgrade",
                "speed_upgrade", "crit_upgrade", "potion_heal_upgrade"
            };

            foreach (string id in upgradeIds)
            {
                CreateUpgradeRow(id);
            }
        }

        private void CreateUpgradeRow(string upgradeId)
        {
            if (upgradeRowPrefab == null || upgradeContainer == null) return;

            GameObject row = Instantiate(upgradeRowPrefab, upgradeContainer);

            TextMeshProUGUI nameText = row.transform.Find("NameText")?.GetComponent<TextMeshProUGUI>();
            TextMeshProUGUI levelText = row.transform.Find("LevelText")?.GetComponent<TextMeshProUGUI>();
            TextMeshProUGUI costText = row.transform.Find("CostText")?.GetComponent<TextMeshProUGUI>();
            Button buyButton = row.transform.Find("BuyButton")?.GetComponent<Button>();

            int currentLevel = progressManager.GetCurrentUpgradeLevel(upgradeId);
            int maxLevel = progressManager.GetUpgradeMaxLevel(upgradeId);
            int cost = progressManager.GetUpgradeCost(upgradeId, currentLevel);

            if (nameText != null)
                nameText.text = progressManager.GetUpgradeName(upgradeId);

            if (levelText != null)
                levelText.text = $"Lv.{currentLevel} / {maxLevel}";

            if (costText != null)
                costText.text = cost >= 0 ? $"{cost} Souls" : "MAX";

            if (buyButton != null)
            {
                bool canBuy = cost >= 0 && progressManager.GetSouls() >= cost;
                buyButton.interactable = canBuy;

                string capturedId = upgradeId;
                buyButton.onClick.AddListener(() =>
                {
                    progressManager.PurchaseUpgrade(capturedId);
                    PopulateUpgrades(); // Refresh
                });
            }
        }

        private void UpdateSoulsDisplay()
        {
            if (soulsDisplayText != null && progressManager != null)
            {
                soulsDisplayText.text = $"Abyss Souls: {progressManager.GetSouls()}";
            }
        }

        // ── Settings ──

        private void LoadSettingsUI()
        {
            if (progressManager == null) return;
            var save = progressManager.SaveData;

            if (masterVolumeSlider != null) masterVolumeSlider.value = save.masterVolume;
            if (sfxVolumeSlider != null) sfxVolumeSlider.value = save.sfxVolume;
            if (musicVolumeSlider != null) musicVolumeSlider.value = save.musicVolume;
        }

        private void OnMasterVolumeChanged(float value)
        {
            if (progressManager != null) progressManager.SaveData.masterVolume = value;
            AudioListener.volume = value;
        }

        private void OnSFXVolumeChanged(float value)
        {
            if (progressManager != null) progressManager.SaveData.sfxVolume = value;
        }

        private void OnMusicVolumeChanged(float value)
        {
            if (progressManager != null) progressManager.SaveData.musicVolume = value;
        }

        // ── Helpers ──

        private void SetActivePanel(GameObject activePanel)
        {
            GameObject[] panels = {
                mainMenuPanel, classSelectPanel, upgradesPanel,
                settingsPanel, runSummaryPanel
            };

            foreach (var panel in panels)
            {
                if (panel != null) panel.SetActive(panel == activePanel);
            }
        }
    }
}
