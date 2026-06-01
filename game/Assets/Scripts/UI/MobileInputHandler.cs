using UnityEngine;
using System;

namespace AbyssWalker.UI
{
    /// <summary>
    /// Handles mobile touch input for movement and skills.
    /// Supports swipe-to-move (4 directions), tap-to-skill, and virtual joystick option.
    /// </summary>
    public class MobileInputHandler : MonoBehaviour
    {
        public static MobileInputHandler Instance { get; private set; }

        [Header("Swipe Settings")]
        [SerializeField] private float swipeThreshold = 50f;
        [SerializeField] private float swipeTimeLimit = 0.5f;

        [Header("Virtual Joystick")]
        [SerializeField] private bool useVirtualJoystick = false;
        [SerializeField] private RectTransform joystickArea;
        [SerializeField] private RectTransform joystickKnob;
        [SerializeField] private float joystickDeadZone = 0.2f;

        [Header("Tap Settings")]
        [SerializeField] private float tapTimeLimit = 0.3f;

        // ── Events ──
        public event Action<Vector2Int> OnSwipeMove;
        public event Action OnTapSkill;
        public event Action<Vector2> OnJoystickMove;
        public event Action OnJoystickRelease;

        // ── Internal State ──
        private Vector2 touchStartPos;
        private float touchStartTime;
        private bool isSwiping;
        private bool isJoystickActive;
        private int activeFingerId = -1;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Update()
        {
            // Handle editor mouse input for testing
#if UNITY_EDITOR
            HandleMouseInput();
#else
            HandleTouchInput();
#endif
        }

        /// <summary>
        /// Returns the current joystick direction (normalized).
        /// Returns Vector2.zero if not using joystick or not active.
        /// </summary>
        public Vector2 GetJoystickDirection()
        {
            if (!useVirtualJoystick || !isJoystickActive) return Vector2.zero;
            Vector2 delta = (Vector2)joystickKnob.anchoredPosition / (joystickArea.sizeDelta * 0.5f);
            return delta.magnitude > joystickDeadZone ? delta.normalized : Vector2.zero;
        }

        // ── Touch Handling ──

        private void HandleTouchInput()
        {
            if (Input.touchCount == 0) return;

            Touch touch = Input.GetTouch(0);

            switch (touch.phase)
            {
                case TouchPhase.Began:
                    OnTouchBegan(touch.position, touch.fingerId);
                    break;

                case TouchPhase.Moved:
                case TouchPhase.Stationary:
                    if (useVirtualJoystick && isJoystickActive && touch.fingerId == activeFingerId)
                    {
                        UpdateJoystick(touch.position);
                    }
                    break;

                case TouchPhase.Ended:
                case TouchPhase.Canceled:
                    if (touch.fingerId == activeFingerId)
                    {
                        OnTouchEnded(touch.position);
                    }
                    break;
            }

            // Pinch to zoom (two fingers)
            if (Input.touchCount == 2)
            {
                HandlePinchZoom();
            }
        }

        private void OnTouchBegan(Vector2 position, int fingerId)
        {
            touchStartPos = position;
            touchStartTime = Time.time;
            isSwiping = true;
            activeFingerId = fingerId;

            if (useVirtualJoystick && IsInsideJoystickArea(position))
            {
                isJoystickActive = true;
                UpdateJoystick(position);
            }
        }

        private void OnTouchEnded(Vector2 position)
        {
            float elapsed = Time.time - touchStartTime;

            if (useVirtualJoystick && isJoystickActive)
            {
                isJoystickActive = false;
                activeFingerId = -1;

                if (joystickKnob != null)
                {
                    joystickKnob.anchoredPosition = Vector2.zero;
                }

                OnJoystickRelease?.Invoke();
                return;
            }

            if (!isSwiping) return;

            Vector2 delta = position - touchStartPos;

            if (elapsed <= tapTimeLimit && delta.magnitude < swipeThreshold)
            {
                // It is a tap
                OnTapSkill?.Invoke();
            }
            else if (elapsed <= swipeTimeLimit && delta.magnitude >= swipeThreshold)
            {
                // It is a swipe
                Vector2Int direction = GetSwipeDirection(delta);
                OnSwipeMove?.Invoke(direction);
            }

            isSwiping = false;
            activeFingerId = -1;
        }

        private void UpdateJoystick(Vector2 touchPosition)
        {
            if (joystickArea == null || joystickKnob == null) return;

            Vector2 areaCenter = joystickArea.position;
            Vector2 delta = touchPosition - areaCenter;

            float maxDistance = joystickArea.sizeDelta.x * 0.5f;
            delta = Vector2.ClampMagnitude(delta, maxDistance);

            joystickKnob.position = areaCenter + delta;

            Vector2 normalized = delta / maxDistance;
            OnJoystickMove?.Invoke(normalized);
        }

        // ── Mouse Input (Editor Testing) ──

        private void HandleMouseInput()
        {
            if (Input.GetMouseButtonDown(0))
            {
                Vector2 pos = Input.mousePosition;
                touchStartPos = pos;
                touchStartTime = Time.time;
                isSwiping = true;

                if (useVirtualJoystick && IsInsideJoystickArea(pos))
                {
                    isJoystickActive = true;
                    UpdateJoystick(pos);
                }
            }

            if (Input.GetMouseButton(0) && useVirtualJoystick && isJoystickActive)
            {
                UpdateJoystick(Input.mousePosition);
            }

            if (Input.GetMouseButtonUp(0))
            {
                float elapsed = Time.time - touchStartTime;
                Vector2 delta = (Vector2)Input.mousePosition - touchStartPos;

                if (useVirtualJoystick && isJoystickActive)
                {
                    isJoystickActive = false;
                    joystickKnob.anchoredPosition = Vector2.zero;
                    OnJoystickRelease?.Invoke();
                }
                else if (elapsed <= tapTimeLimit && delta.magnitude < swipeThreshold)
                {
                    OnTapSkill?.Invoke();
                }
                else if (elapsed <= swipeTimeLimit && delta.magnitude >= swipeThreshold)
                {
                    Vector2Int direction = GetSwipeDirection(delta);
                    OnSwipeMove?.Invoke(direction);
                }

                isSwiping = false;
            }
        }

        // ── Helpers ──

        private Vector2Int GetSwipeDirection(Vector2 delta)
        {
            if (Mathf.Abs(delta.x) > Mathf.Abs(delta.y))
            {
                return delta.x > 0 ? Vector2Int.right : Vector2Int.left;
            }
            else
            {
                return delta.y > 0 ? Vector2Int.up : Vector2Int.down;
            }
        }

        private bool IsInsideJoystickArea(Vector2 screenPosition)
        {
            if (joystickArea == null) return false;
            return RectTransformUtility.RectangleContainsScreenPoint(joystickArea, screenPosition);
        }

        private void HandlePinchZoom()
        {
            Touch touch0 = Input.GetTouch(0);
            Touch touch1 = Input.GetTouch(1);

            Vector2 prevPos0 = touch0.position - touch0.deltaPosition;
            Vector2 prevPos1 = touch1.position - touch1.deltaPosition;

            float prevMagnitude = (prevPos0 - prevPos1).magnitude;
            float currentMagnitude = (touch0.position - touch1.position).magnitude;

            float difference = currentMagnitude - prevMagnitude;

            // Notify camera system of zoom
            // CameraController can subscribe to this if needed
            if (Mathf.Abs(difference) > 0.1f)
            {
                // Handled by camera system
            }
        }
    }
}
