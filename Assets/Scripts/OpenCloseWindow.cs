using System;
using System.Collections;
using UnityEngine;

namespace ChristinaCreatesGames.UI
{
    public class OpenCloseWindow : MonoBehaviour
    {
        [Header("Window Setup")]
        [SerializeField] private GameObject window;
        [SerializeField] private RectTransform windowRectTransform;
        [SerializeField] private CanvasGroup windowCanvasGroup;

        public enum AnimateToDirection
        {
            Top,
            Bottom,
            Left,
            Right
        }

        [Header("Animation Setup")]
        [SerializeField] private AnimateToDirection openDirection = AnimateToDirection.Top;
        [SerializeField] private AnimateToDirection closeDirection = AnimateToDirection.Bottom;
        [Space]
        [SerializeField] private Vector2 distanceToAnimate = new Vector2(100, 100);
        [SerializeField] private AnimationCurve easingCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        [Range(0, 1f)][SerializeField] private float animationDuration = 0.5f;

        private bool _isOpen;
        private Vector2 _initialPosition;
        private Vector2 _currentPosition;

        private Vector2 _upOffset;
        private Vector2 _downOffset;
        private Vector2 _leftOffset;
        private Vector2 _rightOffset;

        private Coroutine _animateWindowCoroutine;

        [Header("Helpers")]
        [SerializeField] private bool displayGizmos = true;

        public static event Action OnOpenWindow;
        public static event Action OnCloseWindow;

        private enum DisplayGizmosAtLocation
        {
            Open,
            Close,
            Both,
            Situational,
            None
        }

        [SerializeField] private DisplayGizmosAtLocation gizmoHandler;
        [SerializeField] private Color gizmoOpenColor = Color.green;
        [SerializeField] private Color gizmoCloseColor = Color.red;
        [SerializeField] private Color gizmoInitalLocationColor = Color.grey;
        private Vector2 _windowOpenPositionForGizmos;
        private Vector2 _windowClosePositionForGizmos;
        private Vector2 _initialPositionForGizmos;

        private void OnValidate()
        {
            if (window != null)
            {
                windowRectTransform = window.GetComponent<RectTransform>();
                windowCanvasGroup = window.GetComponent<CanvasGroup>();
            }

            distanceToAnimate.x = Mathf.Max(0, distanceToAnimate.x);
            distanceToAnimate.y = Mathf.Max(0, distanceToAnimate.y);

            if (window != null)
            {
                _initialPosition = windowRectTransform.anchoredPosition;
                InitializeOffsetPositions();
                RecalculateGizmoPositions();
            }
        }

        #region AnimationFunctionality

        private void Start()
        {
            if (window != null)
            {
                _initialPosition = windowRectTransform.anchoredPosition;
                InitializeOffsetPositions();
            }
        }

        private void InitializeOffsetPositions()
        {
            _upOffset = new Vector2(0, distanceToAnimate.y);
            _downOffset = new Vector2(0, -distanceToAnimate.y);
            _rightOffset = new Vector2(distanceToAnimate.x, 0);
            _leftOffset = new Vector2(-distanceToAnimate.x, 0);
        }

        [ContextMenu("Toggle Open Close")]
        public void ToggleOpenClose()
        {
            if (_isOpen)
                CloseWindow();
            else
                OpenWindow();
        }

        [ContextMenu("Open Window")]
        public void OpenWindow()
        {
            if (_isOpen)
                return;

            _isOpen = true;
            OnOpenWindow?.Invoke();

            if (_animateWindowCoroutine != null)
                StopCoroutine(_animateWindowCoroutine);

            _animateWindowCoroutine = StartCoroutine(AnimateWindow(true));
        }

        [ContextMenu("Close Window")]
        public void CloseWindow()
        {
            if (!_isOpen)
                return;

            _isOpen = false;
            OnCloseWindow?.Invoke();

            if (_animateWindowCoroutine != null)
                StopCoroutine(_animateWindowCoroutine);

            _animateWindowCoroutine = StartCoroutine(AnimateWindow(false));
        }

        private Vector2 GetOffset(AnimateToDirection direction)
        {
            switch (direction)
            {
                case AnimateToDirection.Top:
                    return _upOffset;
                case AnimateToDirection.Bottom:
                    return _downOffset;
                case AnimateToDirection.Left:
                    return _leftOffset;
                case AnimateToDirection.Right:
                    return _rightOffset;
                default:
                    return Vector2.zero;
            }
        }

        private IEnumerator AnimateWindow(bool open)
        {
            if (open)
                window.SetActive(true);

            _currentPosition = windowRectTransform.anchoredPosition;
            float elapsedTime = 0;
            Vector2 targetPosition = open ? _currentPosition + GetOffset(openDirection) : _currentPosition + GetOffset(closeDirection);

            while (elapsedTime < animationDuration)
            {
                float evaluationAtTime = easingCurve.Evaluate(elapsedTime / animationDuration);
                windowRectTransform.anchoredPosition = Vector2.Lerp(_currentPosition, targetPosition, evaluationAtTime);

                windowCanvasGroup.alpha = open ? Mathf.Lerp(0f, 1f, evaluationAtTime) : Mathf.Lerp(1f, 0f, evaluationAtTime);
                elapsedTime += Time.deltaTime;
                yield return null;
            }

            windowRectTransform.anchoredPosition = targetPosition;
            windowCanvasGroup.alpha = open ? 1 : 0;
            windowCanvasGroup.interactable = open;
            windowCanvasGroup.blocksRaycasts = open;

            if (!open)
            {
                window.SetActive(false);
                windowRectTransform.anchoredPosition = _initialPosition;
            }
        }

        #endregion

        #region Visualisation

        private void OnDrawGizmosSelected()
        {
            if (!displayGizmos || window == null || windowRectTransform == null)
                return;

            Gizmos.color = gizmoInitalLocationColor;
            Gizmos.DrawWireCube(_initialPositionForGizmos, windowRectTransform.sizeDelta);

            switch (gizmoHandler)
            {
                case DisplayGizmosAtLocation.Open:
                    DrawCube(_windowOpenPositionForGizmos, true);
                    break;

                case DisplayGizmosAtLocation.Close:
                    DrawCube(_windowClosePositionForGizmos, false);
                    break;

                case DisplayGizmosAtLocation.Both:
                    DrawCube(_windowClosePositionForGizmos, false);
                    DrawCube(_windowOpenPositionForGizmos, true);
                    break;

                case DisplayGizmosAtLocation.Situational:
                    if (_isOpen)
                        DrawCube(_windowClosePositionForGizmos, false);
                    else
                        DrawCube(_windowOpenPositionForGizmos, true);
                    break;

                default:
                case DisplayGizmosAtLocation.None:
                    break;
            }

            if (gizmoHandler != DisplayGizmosAtLocation.None)
                DrawIndicators();
        }

        private void DrawCube(Vector2 windowPosition, bool opens)
        {
            Gizmos.color = opens ? gizmoOpenColor : gizmoCloseColor;
            Gizmos.DrawWireCube(windowPosition, windowRectTransform.sizeDelta);
        }

        private void DrawIndicators()
        {
            Gizmos.color = gizmoOpenColor;
            Gizmos.DrawLine(_initialPositionForGizmos, _windowOpenPositionForGizmos);

            Gizmos.color = gizmoCloseColor;
            Gizmos.DrawLine(_windowOpenPositionForGizmos, _windowClosePositionForGizmos);
        }

        private void RecalculateGizmoPositions()
        {
            InitializeOffsetPositions();

            _initialPositionForGizmos = new Vector2(windowRectTransform.anchoredPosition.x, windowRectTransform.anchoredPosition.y) + windowRectTransform.rect.center;
            _windowOpenPositionForGizmos = _initialPositionForGizmos + GetOffset(openDirection);
            _windowClosePositionForGizmos = _windowOpenPositionForGizmos + GetOffset(closeDirection);
        }

        #endregion
    }
}
