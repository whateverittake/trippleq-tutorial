using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace TrippleQ.Tutorial
{
    public sealed class TutorialOverlayView : MonoBehaviour, ITutorialOverlayView, IPointerClickHandler
    {
        public event Action TargetClicked;

        [Header("Canvas")]
        [SerializeField] private Canvas _canvas;
        [SerializeField] private RectTransform _canvasRect;

        [Header("Dim Panels (cutout without shader)")]
        [SerializeField] private RectTransform _dimTop;
        [SerializeField] private RectTransform _dimBottom;
        [SerializeField] private RectTransform _dimLeft;
        [SerializeField] private RectTransform _dimRight;

        [Header("Highlight")]
        [SerializeField] private RectTransform _highlightFrame;
        [SerializeField] private float _padding = 16f;

        [Header("Text")]
        [SerializeField] private RectTransform _textRoot;
        [SerializeField] private TMP_Text _description;
        [SerializeField] private Vector2 _textOffset = new Vector2(0, -140);

        [Header("Input")]
        [SerializeField] private Graphic _raycastBlocker;

        [Header("Hand")]
        [SerializeField] private RectTransform _hand;
        [SerializeField] private Vector2 _handOffset = new Vector2(80f, -40f);
        [SerializeField] private float _handBobAmplitude = 12f;
        [SerializeField] private float _handBobSpeed = 3.5f;
        [SerializeField] private Vector2 _handBobDirection = new Vector2(0f, 1f);
        [SerializeField] private bool _showHand = true;

        [Header("Text Blink")]
        [SerializeField] private bool _blinkText = true;
        [SerializeField] private float _textBlinkSpeed = 2.5f;
        [SerializeField] private float _textBlinkMinAlpha = 0.45f;
        [SerializeField] private float _textBlinkMaxAlpha = 1f;
        [SerializeField] private float _textMinWidth = 260f;
        [SerializeField] private float _textMaxWidth = 520f;
        [SerializeField] private int _maxTextLines = 2;
        [SerializeField] private float _textHorizontalPadding = 24f; // nếu có bubble bg

        private RectTransform _target;
        private bool _holeClickEnabled;
        private Rect _holeRectLocal; // in canvas local space

        private Vector2 _handBasePos;
        private float _animTime;

        private bool _animEnabled;

        private void Reset()
        {
            _canvas = GetComponentInParent<Canvas>();
            _canvasRect = _canvas != null ? _canvas.transform as RectTransform : null;
        }

        public void Show(string description, RectTransform target)
        {
            _target = target;

            if (_description != null)
                _description.text = description ?? string.Empty;

            if (_textRoot != null)
                UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(_textRoot);

            // Force layout rebuild so background (ContentSizeFitter) updates size immediately
            if (_textRoot != null)
    UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(_textRoot);

            if (_canvas == null) _canvas = GetComponentInParent<Canvas>();
            if (_canvasRect == null && _canvas != null) _canvasRect = _canvas.transform as RectTransform;

            if (_raycastBlocker != null) _raycastBlocker.raycastTarget = true;

            gameObject.SetActive(true);

            if (_target != null)
                SyncToTarget(_target);

            _animTime = 0f;

            if (_hand != null)
                _hand.gameObject.SetActive(_showHand);

            _animEnabled = true;
        }

        public void Hide()
        {
            _target = null;
            gameObject.SetActive(false);
            if (_hand != null) _hand.gameObject.SetActive(false);
            _animEnabled=false;
        }

        public void TickAnim(float deltaTime)
        {
            if (!_animEnabled) return;
            _animTime += deltaTime;

            // Hand bob
            if (_hand != null && _hand.gameObject.activeInHierarchy && _showHand)
            {
                float s = Mathf.Sin(_animTime * _handBobSpeed);
                Vector2 dir = _handBobDirection.sqrMagnitude < 0.0001f ? Vector2.up : _handBobDirection.normalized;

                _hand.anchoredPosition = _handBasePos + dir * (s * _handBobAmplitude);
            }

            // Text blink (alpha pulse)
            if (_blinkText && _description != null && _description.gameObject.activeInHierarchy)
            {
                float t = (Mathf.Sin(_animTime * _textBlinkSpeed) + 1f) * 0.5f; // 0..1
                float a = Mathf.Lerp(_textBlinkMinAlpha, _textBlinkMaxAlpha, t);

                var c = _description.color;
                c.a = a;
                _description.color = c;
            }
        }

        public void SyncToTarget(RectTransform target)
        {
            _target = target;

            if (_canvasRect == null)
                return;

            if (target == null)
            {
                // Clear
                _holeRectLocal = default;
                return;
            }

            // 1) Get target world corners
            var world = new Vector3[4];
            target.GetWorldCorners(world);

            // 2) Convert world corners -> canvas local
            Vector2 min = WorldToCanvasLocal(_canvasRect, world[0]);
            Vector2 max = WorldToCanvasLocal(_canvasRect, world[2]);

            // 3) Apply padding
            min -= Vector2.one * _padding;
            max += Vector2.one * _padding;

            // 4) Ensure min <= max
            if (min.x > max.x) (min.x, max.x) = (max.x, min.x);
            if (min.y > max.y) (min.y, max.y) = (max.y, min.y);

            // 5) Clamp to canvas rect bounds (robust to pivot/pos/scale)
            var r = _canvasRect.rect;
            float xMinC = r.xMin;
            float xMaxC = r.xMax;
            float yMinC = r.yMin;
            float yMaxC = r.yMax;

            min.x = Mathf.Clamp(min.x, xMinC, xMaxC);
            min.y = Mathf.Clamp(min.y, yMinC, yMaxC);
            max.x = Mathf.Clamp(max.x, xMinC, xMaxC);
            max.y = Mathf.Clamp(max.y, yMinC, yMaxC);

            // 6) Save hole rect (canvas local) for hole-click fallback
            _holeRectLocal = Rect.MinMaxRect(min.x, min.y, max.x, max.y);

            // 7) Optional highlight frame
            if (_highlightFrame != null)
            {
                var center = (min + max) * 0.5f;
                var sizeHighlight = (max - min);

                _highlightFrame.anchoredPosition = center;
                _highlightFrame.sizeDelta = sizeHighlight;
            }

            // 8) Cutout using 4 dim panels around the hole
            SetPanel(_dimTop, xMinC, xMaxC, max.y, yMaxC);
            SetPanel(_dimBottom, xMinC, xMaxC, yMinC, min.y);
            SetPanel(_dimLeft, xMinC, min.x, min.y, max.y);
            SetPanel(_dimRight, max.x, xMaxC, min.y, max.y);

            // 9) Text placement
            if (_target != null)
                SyncToTarget(_target);

            if (_textRoot != null)
                UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(_textRoot);

            // Hand placement: anchor to hole center + offset
            if (_hand != null)
            {
                var center = (min + max) * 0.5f;
                _handBasePos = center + _handOffset;
                _hand.anchoredPosition = _handBasePos; // base pos (bob will be applied in TickAnim)
            }
        }

        void SetTextWidth(float width)
        {
            var s = _textRoot.sizeDelta;
            s.x = width;
            _textRoot.sizeDelta = s;
        }

        private Camera GetEventCamera()
        {
            if (_canvas == null) return null;
            // Overlay mode trả về null, Camera modes trả về worldCamera
            return _canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : _canvas.worldCamera;
        }

        private Vector2 WorldToCanvasLocal(Vector3 worldPos)
        {
            var cam = GetEventCamera();
            // Chuyển World -> Screen Space
            Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(cam, worldPos);
            // Chuyển Screen Space -> Canvas Local Space
            RectTransformUtility.ScreenPointToLocalPointInRectangle(_canvasRect, screenPoint, cam, out var localPoint);
            return localPoint;
        }

        private static void SetPanel(RectTransform panel, float xMin, float xMax, float yMin, float yMax)
        {
            if (panel == null) return;

            float w = Mathf.Max(0, xMax - xMin);
            float h = Mathf.Max(0, yMax - yMin);

            // Tắt panel nếu kích thước quá nhỏ để tối ưu hiệu năng
            bool active = (w > 0.1f && h > 0.1f);
            if (panel.gameObject.activeSelf != active)
                panel.gameObject.SetActive(active);

            if (!active) return;

            // Tính tâm và kích thước dựa trên hệ tọa độ Center-Center
            panel.anchoredPosition = new Vector2((xMin + xMax) * 0.5f, (yMin + yMax) * 0.5f);
            panel.sizeDelta = new Vector2(w, h);
        }

        public void EnableHoleClick(bool enable)
        {
            _holeClickEnabled = enable;
            _raycastBlocker.gameObject.SetActive(enable);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (!_holeClickEnabled) return;
            if (_canvasRect == null) return;

            var cam = eventData.pressEventCamera; // null for overlay
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _canvasRect, eventData.position, cam, out var localPoint);

            bool inside = _holeRectLocal.Contains(localPoint);

            Debug.Log($"[TutorialOverlayView] OnPointerClick at {eventData.position}, local={localPoint}, inside hole: {inside}", this);
            if (!inside) return;

            TargetClicked?.Invoke();
        }

        private static Vector2 WorldToCanvasLocal(
                RectTransform canvasRect,
                Vector3 worldPos,
                Camera eventCamera = null)
        {
            // Convert world -> screen
            Vector2 screenPos = RectTransformUtility.WorldToScreenPoint(eventCamera, worldPos);

            // Convert screen -> canvas local
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRect,
                screenPos,
                eventCamera,
                out Vector2 localPoint
            );

            return localPoint;
        }
    }
}