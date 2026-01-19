using System.Collections.Generic;
using UnityEngine;

namespace TrippleQ.Tutorial
{
    /// <summary>
    /// Scene binder/controller:
    /// - holds serialized ref to View
    /// - creates Service + Presenter
    /// - ticks presenter for keeping overlay synced
    /// </summary>
    public class TutorialOverlayController : MonoBehaviour
    {
        [Header("Refs")]
        [SerializeField] private TutorialBootstrap _bootstrap;
        [SerializeField] private TutorialOverlayView _view;

        [Header("Behavior")]
        [SerializeField] private bool _autoStopIfTargetMissing = true;
        [SerializeField] private bool _tickInLateUpdate = true;

        private TutorialOverlayPresenter _presenter;

        private void Awake()
        {
            if (_view == null)
            {
                Debug.LogError("[TutorialOverlayController] Missing view.", this);
                return;
            }

            if (_bootstrap == null)
            {
                Debug.LogError("[TutorialOverlayController] Missing bootstrap reference.", this);
                return;
            }

            // bootstrap may already be ready, or will be ready after StartInit
            if (_bootstrap.Service != null)
                Bind(_bootstrap.Service);
            else
                _bootstrap.OnServiceReady += Bind;
        }

        private void Bind(TutorialService svc)
        {
            _bootstrap.OnServiceReady -= Bind;

            _presenter?.Dispose();
            _presenter = new TutorialOverlayPresenter(svc, _view, _autoStopIfTargetMissing);
        }

        private void Update()
        {
            if (_tickInLateUpdate) return;
            _presenter?.Tick();

            if (_view != null)
                _view.TickAnim(Time.unscaledDeltaTime);
        }

        private void LateUpdate()
        {
            if (!_tickInLateUpdate) return;
            _presenter?.Tick();

            if (_view != null)
                _view.TickAnim(Time.unscaledDeltaTime);
        }

        private void OnDestroy()
        {
            if (_bootstrap != null) _bootstrap.OnServiceReady -= Bind;
            _presenter?.Dispose();
            _presenter = null;
        }

        // Convenience forwarders (optional): call bootstrap.Service directly cũng được
        public void Stop() => _bootstrap?.Service?.Stop();

        public void Play(System.Collections.Generic.IReadOnlyList<TutorialStep> steps,
                         System.Collections.Generic.IReadOnlyList<RectTransform> targets)
        {
            _bootstrap?.Service?.Play(steps, targets);
        }
    }
}
