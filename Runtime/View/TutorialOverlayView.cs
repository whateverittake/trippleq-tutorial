using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TrippleQ.Tutorial
{
    public sealed class TutorialOverlayView : MonoBehaviour, ITutorialOverlayView
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

        [Header("Text (Background root that auto-sizes via LayoutGroup + ContentSizeFitter)")]
        [Tooltip("Root of the text bubble/background. This object should have LayoutGroup + ContentSizeFitter.")]
        [SerializeField] private RectTransform _textRoot;
        [SerializeField] private TMP_Text _description;
        [SerializeField] private Vector2 _textOffset = new Vector2(0, -140);
        [SerializeField] private float _textSafeMargin = 12f;

        [Header("Input")]
        [SerializeField] private Graphic _raycastBlocker;

        private RectTransform _target;
        private bool _holeClickEnabled;

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

            if (_canvas == null) _canvas = GetComponentInParent<Canvas>();
            if (_canvasRect == null && _canvas != null) _canvasRect = _canvas.transform as RectTransform;

            if (_raycastBlocker != null)
                _raycastBlocker.raycastTarget = true;

            gameObject.SetActive(true);

            // IMPORTANT: Force layout rebuild so BG (ContentSizeFitter) updates immediately
            if (_textRoot != null)
            {
                // TMP sometimes needs mesh update before layout size is correct
                if (_description != null)
                    _description.ForceMeshUpdate();

                LayoutRebuilder.ForceRebuildLayoutImmediate(_textRoot);
            }

            if (_target != null)
                SyncToTarget(_target);
        }

        public void Hide()
        {
            _target = null;
            gameObject.SetActive(false);
        }

        public void SyncToTarget(RectTransform target)
        {
            if (target == null || _canvasRect == null) return;

            // 1) Get target world corners
            var worldCorners = new Vector3[4];
            target.GetWorldCorners(worldCorners);

            // Convert corners to canvas local (supports rotated targets)
            Vector2 min = new Vector2(float.MaxValue, float.MaxValue);
            Vector2 max = new Vector2(float.MinValue, float.MinValue);

            foreach (var corner in worldCorners)
            {
                Vector2 localPos = WorldToCanvasLocal(corner);
                min = Vector2.Min(min, localPos);
                max = Vector2.Max(max, localPos);
            }

            // Apply padding
            min -= Vector2.one * _padding;
            max += Vector2.one * _padding;

            // Clamp to canvas bounds (center-origin)
            var canvasSize = _canvasRect.rect.size;
            Vector2 half = canvasSize * 0.5f;

            min.x = Mathf.Clamp(min.x, -half.x, half.x);
            min.y = Mathf.Clamp(min.y, -half.y, half.y);
            max.x = Mathf.Clamp(max.x, -half.x, half.x);
            max.y = Mathf.Clamp(max.y, -half.y, half.y);

            // 2) Highlight frame
            if (_highlightFrame != null)
            {
                _highlightFrame.anchoredPosition = (min + max) * 0.5f;
                _highlightFrame.sizeDelta = (max - min);
            }

            // 3) Dim panels (cutout)
            SetPanel(_dimTop, -half.x, half.x, max.y, half.y);
            SetPanel(_dimBottom, -half.x, half.x, -half.y, min.y);
            SetPanel(_dimLeft, -half.x, min.x, min.y, max.y);
            SetPanel(_dimRight, max.x, half.x, min.y, max.y);

            // 4) Text bubble position ONLY (size is handled by layout components)
            if (_textRoot != null)
            {
                // Rebuild once more here in case text changed after Show() or step switched quickly
                if (_description != null)
                    _description.ForceMeshUpdate();

                LayoutRebuilder.ForceRebuildLayoutImmediate(_textRoot);

                Vector2 center = (min + max) * 0.5f;
                Vector2 pos = center + _textOffset;

                // Clamp text bubble inside canvas using current layout size
                var r = _canvasRect.rect;
                float w = _textRoot.rect.width;
                float h = _textRoot.rect.height;

                float halfW = w * 0.5f;
                float halfH = h * 0.5f;

                // add a small safe margin so it doesn't touch edges
                float mx = _textSafeMargin;
                float my = _textSafeMargin;

                pos.x = Mathf.Clamp(pos.x, r.xMin + halfW + mx, r.xMax - halfW - mx);
                pos.y = Mathf.Clamp(pos.y, r.yMin + halfH + my, r.yMax - halfH - my);

                _textRoot.anchoredPosition = pos;
            }
        }

        private Camera GetEventCamera()
        {
            if (_canvas == null) return null;
            return _canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : _canvas.worldCamera;
        }

        private Vector2 WorldToCanvasLocal(Vector3 worldPos)
        {
            var cam = GetEventCamera();
            Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(cam, worldPos);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(_canvasRect, screenPoint, cam, out var localPoint);
            return localPoint;
        }

        private static void SetPanel(RectTransform panel, float xMin, float xMax, float yMin, float yMax)
        {
            if (panel == null) return;

            float w = Mathf.Max(0, xMax - xMin);
            float h = Mathf.Max(0, yMax - yMin);

            bool active = (w > 0.1f && h > 0.1f);
            if (panel.gameObject.activeSelf != active)
                panel.gameObject.SetActive(active);

            if (!active) return;

            panel.anchoredPosition = new Vector2((xMin + xMax) * 0.5f, (yMin + yMax) * 0.5f);
            panel.sizeDelta = new Vector2(w, h);
        }

        public void EnableHoleClick(bool enable)
        {
            _holeClickEnabled = enable;
        }
    }
}
