using System;
using UnityEngine;
using UnityEngine.UI;

namespace TrippleQ.Tutorial
{
    /// <summary>
    /// Presenter (plain class): binds TutorialService <-> ITutorialOverlayView.
    /// Direction (H2): user clicks the real Button, presenter listens to Button.onClick to advance step.
    /// </summary>
    public sealed class TutorialOverlayPresenter : IDisposable
    {
        private readonly TutorialService _service;
        private readonly ITutorialOverlayView _view;
        private readonly bool _autoStopIfTargetMissing;

        private RectTransform _currentTarget;
        private Button _currentButton; // bound click source

        public TutorialOverlayPresenter(
            TutorialService service,
            ITutorialOverlayView view,
            bool autoStopIfTargetMissing = true)
        {
            _service = service ?? throw new ArgumentNullException(nameof(service));
            _view = view ?? throw new ArgumentNullException(nameof(view));
            _autoStopIfTargetMissing = autoStopIfTargetMissing;

            _service.StepChanged += OnStepChanged;
            _service.Stopped += OnStopped;

            _view.TargetClicked += OnHoleClicked;
        }

        public void Dispose()
        {
            UnbindCurrentButton();

            _service.StepChanged -= OnStepChanged;
            _service.Stopped -= OnStopped;

            _view.TargetClicked -= OnHoleClicked;
        }

        private void OnHoleClicked()
        {
            _service.CompleteCurrentAndNext();
        }

        /// <summary>
        /// Optional tick: keep overlay aligned if target moves/layout changes.
        /// Call from controller LateUpdate.
        /// </summary>
        public void Tick()
        {
            if (!_service.IsPlaying) return;
            if (_currentTarget == null) return;

            _view.SyncToTarget(_currentTarget);
        }

        private void OnStepChanged(TutorialStep step, RectTransform target)
        {
            _currentTarget = target;

            if (target == null || !target.gameObject.activeInHierarchy)
            {
                if (_autoStopIfTargetMissing)
                {
                    Debug.LogWarning($"[Tutorial] Target missing/inactive at step '{step?.Id}'. Stop tutorial.");
                    _service.Stop();
                    return;
                }
            }

            // Show overlay highlight + text
            _view.Show(step?.Description ?? string.Empty, target);

            // H2: Bind to the REAL Button click
            BindToTargetButton(target, step);
        }

        private void OnStopped()
        {
            UnbindCurrentButton();
            _currentTarget = null;
            _view.Hide();
            _view.EnableHoleClick(false);
        }

        // ---------------- H2 binding ----------------

        private void BindToTargetButton(RectTransform target, TutorialStep step)
        {
            UnbindCurrentButton();

            if (target == null)
                return;

            // Prefer Button on the same object; fallback to parent.
            var btn = target.GetComponent<Button>();
            if (btn == null)
                btn = target.GetComponentInParent<Button>();

            if (btn == null)
            {
                Debug.LogWarning($"[Tutorial] Step '{step?.Id}' target has no Button. Fallback to hole click.");
                _view.EnableHoleClick(true);   // <-- bật fallback
                return;
            }

            _view.EnableHoleClick(false);
            _currentButton = btn;
            Debug.LogError("xx da den day");
            _currentButton.onClick.AddListener(OnTargetButtonClicked);
        }

        private void UnbindCurrentButton()
        {
            if (_currentButton == null) return;

            _currentButton.onClick.RemoveListener(OnTargetButtonClicked);
            _currentButton = null;
        }

        private void OnTargetButtonClicked()
        {
            // Important: This will run AFTER the button's own listeners, depending on order.
            // It's OK for most tutorials: user action happens, then tutorial advances.
            _service.CompleteCurrentAndNext();
        }

        // If later you want overlay click to advance as fallback, you can enable this.
        // private void OnOverlayTargetClicked() => _service.CompleteCurrentAndNext();
    }
}
