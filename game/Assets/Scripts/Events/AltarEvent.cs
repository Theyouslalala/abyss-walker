using UnityEngine;
using System;
using AbyssWalker.Entity;

namespace AbyssWalker.Events
{
    /// <summary>
    /// Handles altar event logic: presents blessing choices and applies the selected one.
    /// </summary>
    public class AltarEvent : MonoBehaviour
    {
        [Header("Fallback Blessings (used if event has none defined)")]
        [SerializeField] private BlessingOption[] fallbackBlessings;

        [Header("UI References")]
        [SerializeField] private PopupUI altarPopup;

        public event Action<BlessingOption> OnBlessingChosen;

        private EventData currentEvent;
        private BlessingOption[] currentBlessings;

        /// <summary>
        /// Opens the altar interface with blessing choices.
        /// </summary>
        public void OpenAltar(EventData eventData)
        {
            if (eventData == null) return;

            currentEvent = eventData;
            eventData.hasBeenTriggered = true;

            // Use event-defined blessings or fallback
            currentBlessings = (eventData.blessingOptions != null && eventData.blessingOptions.Length > 0)
                ? eventData.blessingOptions
                : GetRandomFallbackBlessings();

            ShowAltarUI();
        }

        /// <summary>
        /// Selects a blessing by index and applies it to the player.
        /// </summary>
        public void ChooseBlessing(int index)
        {
            if (currentBlessings == null || index < 0 || index >= currentBlessings.Length)
            {
                Debug.LogWarning("Invalid blessing index");
                return;
            }

            BlessingOption chosen = currentBlessings[index];
            ApplyBlessing(chosen);

            OnBlessingChosen?.Invoke(chosen);

            if (altarPopup != null)
            {
                altarPopup.Show("Altar of Blessing",
                    $"You received: {chosen.blessingName}\n{chosen.description}",
                    "Accept", null);
            }

            currentEvent = null;
            currentBlessings = null;
        }

        private void ApplyBlessing(BlessingOption blessing)
        {
            Player player = FindObjectOfType<Player>();
            if (player == null) return;

            switch (blessing.blessingType)
            {
                case BlessingType.HPBoost:
                    player.IncreaseMaxHP(blessing.value);
                    player.Heal(blessing.value);
                    break;

                case BlessingType.ATKBoost:
                    player.IncreaseATK(blessing.value);
                    break;

                case BlessingType.DEFBoost:
                    player.IncreaseDEF(blessing.value);
                    break;

                case BlessingType.NewSkill:
                    player.LearnSkill(blessing.blessingId);
                    break;

                case BlessingType.SpeedBoost:
                    player.IncreaseSpeed(blessing.value);
                    break;

                case BlessingType.CritBoost:
                    player.IncreaseCritChance(blessing.value);
                    break;
            }
        }

        private void ShowAltarUI()
        {
            if (altarPopup == null)
            {
                altarPopup = FindObjectOfType<PopupUI>();
            }
            if (altarPopup == null) return;

            var sb = new System.Text.StringBuilder();
            sb.AppendLine("Choose a blessing:\n");

            for (int i = 0; i < currentBlessings.Length; i++)
            {
                BlessingOption b = currentBlessings[i];
                sb.AppendLine($"[{i + 1}] {b.blessingName}");
                sb.AppendLine($"    {b.description}\n");
            }

            altarPopup.Show("Altar of Blessing", sb.ToString(), "Cancel",
                () => { currentEvent = null; currentBlessings = null; });
        }

        private BlessingOption[] GetRandomFallbackBlessings()
        {
            if (fallbackBlessings == null || fallbackBlessings.Length == 0)
            {
                return GenerateDefaultBlessings();
            }

            // Pick 3 random blessings from fallback pool
            int count = Mathf.Min(3, fallbackBlessings.Length);
            BlessingOption[] selected = new BlessingOption[count];
            BlessingOption[] shuffled = (BlessingOption[])fallbackBlessings.Clone();

            for (int i = 0; i < count; i++)
            {
                int rand = UnityEngine.Random.Range(i, shuffled.Length);
                (shuffled[i], shuffled[rand]) = (shuffled[rand], shuffled[i]);
                selected[i] = shuffled[i];
            }

            return selected;
        }

        private BlessingOption[] GenerateDefaultBlessings()
        {
            return new BlessingOption[]
            {
                new BlessingOption
                {
                    blessingId = "hp_boost",
                    blessingName = "Vitality Blessing",
                    description = "Max HP +20, fully heal",
                    blessingType = BlessingType.HPBoost,
                    value = 20
                },
                new BlessingOption
                {
                    blessingId = "atk_boost",
                    blessingName = "Warrior Blessing",
                    description = "ATK +5 permanently",
                    blessingType = BlessingType.ATKBoost,
                    value = 5
                },
                new BlessingOption
                {
                    blessingId = "def_boost",
                    blessingName = "Guardian Blessing",
                    description = "DEF +5 permanently",
                    blessingType = BlessingType.DEFBoost,
                    value = 5
                }
            };
        }
    }
}
