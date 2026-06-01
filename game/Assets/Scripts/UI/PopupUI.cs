using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections;

namespace AbyssWalker.UI
{
    /// <summary>
    /// Generic popup UI for events, loot, shop, and confirmations.
    /// Supports show/hide animations and button callbacks.
    /// </summary>
    public class PopupUI : MonoBehaviour
    {
        [Header("Panel References")]
        [SerializeField] private GameObject popupPanel;
        [SerializeField] private CanvasGroup canvasGroup;

        [Header("Content")]
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI bodyText;
        [SerializeField] private Image iconImage;

        [Header("Buttons")]
        [SerializeField] private Button primaryButton;
        [SerializeField] private TextMeshProUGUI primaryButtonText;
        [SerializeField] private Button secondaryButton;
        [SerializeField] private TextMeshProUGUI secondaryButtonText;

        [Header("Animation")]
        [SerializeField] private float fadeDuration = 0.2f;
        [SerializeField] private float scaleFromZero = 0.8f;
        [SerializeField] private AnimationCurve showCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        [SerializeField] private AnimationCurve hideCurve = AnimationCurve.EaseInOut(0, 1, 1, 0);

        private Action primaryCallback;
        private Action secondaryCallback;
        private Coroutine animationCoroutine;

        private void Awake()
        {
            if (popupPanel != null)
            {
                popupPanel.SetActive(false);
            }
        }

        /// <summary>
        /// Shows the popup with a title, body text, and button labels.
        /// Pass null for secondaryLabel to hide the secondary button.
        /// </summary>
        public void Show(string title, string body, string primaryLabel = "OK",
            Action onPrimary = null, string secondaryLabel = null, Action onSecondary = null)
        {
            primaryCallback = onPrimary;
            secondaryCallback = onSecondary;

            if (titleText != null) titleText.text = title;
            if (bodyText != null) bodyText.text = body;

            // Primary button
            if (primaryButtonText != null) primaryButtonText.text = primaryLabel;
            if (primaryButton != null)
            {
                primaryButton.onClick.RemoveAllListeners();
                primaryButton.onClick.AddListener(OnPrimaryClicked);
            }

            // Secondary button
            if (secondaryButton != null)
            {
                bool hasSecondary = !string.IsNullOrEmpty(secondaryLabel);
                secondaryButton.gameObject.SetActive(hasSecondary);

                if (hasSecondary)
                {
                    if (secondaryButtonText != null) secondaryButtonText.text = secondaryLabel;
                    secondaryButton.onClick.RemoveAllListeners();
                    secondaryButton.onClick.AddListener(OnSecondaryClicked);
                }
            }

            PlayShowAnimation();
        }

        /// <summary>
        /// Shows the popup with a sprite icon.
        /// </summary>
        public void ShowWithIcon(string title, string body, Sprite icon,
            string primaryLabel = "OK", Action onPrimary = null)
        {
            if (iconImage != null)
            {
                iconImage.sprite = icon;
                iconImage.gameObject.SetActive(icon != null);
            }

            Show(title, body, primaryLabel, onPrimary);
        }

        /// <summary>
        /// Hides the popup.
        /// </summary>
        public void Hide()
        {
            PlayHideAnimation();
        }

        /// <summary>
        /// Returns whether the popup is currently visible.
        /// </summary>
        public bool IsVisible()
        {
            return popupPanel != null && popupPanel.activeSelf;
        }

        private void OnPrimaryClicked()
        {
            Action cb = primaryCallback;
            Hide();
            cb?.Invoke();
        }

        private void OnSecondaryClicked()
        {
            Action cb = secondaryCallback;
            Hide();
            cb?.Invoke();
        }

        // ── Animations ──

        private void PlayShowAnimation()
        {
            if (animationCoroutine != null)
            {
                StopCoroutine(animationCoroutine);
            }

            popupPanel.SetActive(true);
            animationCoroutine = StartCoroutine(AnimateShow());
        }

        private void PlayHideAnimation()
        {
            if (animationCoroutine != null)
            {
                StopCoroutine(animationCoroutine);
            }

            animationCoroutine = StartCoroutine(AnimateHide());
        }

        private IEnumerator AnimateShow()
        {
            float elapsed = 0f;
            Transform panelTransform = popupPanel.transform;

            if (canvasGroup != null) canvasGroup.alpha = 0f;
            panelTransform.localScale = Vector3.one * scaleFromZero;

            while (elapsed < fadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = showCurve.Evaluate(Mathf.Clamp01(elapsed / fadeDuration));

                if (canvasGroup != null) canvasGroup.alpha = t;
                panelTransform.localScale = Vector3.Lerp(
                    Vector3.one * scaleFromZero, Vector3.one, t);

                yield return null;
            }

            if (canvasGroup != null) canvasGroup.alpha = 1f;
            panelTransform.localScale = Vector3.one;

            animationCoroutine = null;
        }

        private IEnumerator AnimateHide()
        {
            float elapsed = 0f;
            Transform panelTransform = popupPanel.transform;

            while (elapsed < fadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = hideCurve.Evaluate(Mathf.Clamp01(elapsed / fadeDuration));

                if (canvasGroup != null) canvasGroup.alpha = t;
                panelTransform.localScale = Vector3.Lerp(
                    Vector3.one * scaleFromZero, Vector3.one, t);

                yield return null;
            }

            popupPanel.SetActive(false);
            animationCoroutine = null;
        }
    }
}
