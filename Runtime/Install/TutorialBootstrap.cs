using System;
using UnityEngine;

namespace TrippleQ.Tutorial
{
    public sealed class TutorialBootstrap: MonoBehaviour
    {
        [SerializeField] private bool _autoInit = true;

        private TutorialService _svc;
        public TutorialService Service => _svc;

        public event Action<TutorialService> OnServiceReady;

        private void Awake()
        {
            if (_autoInit)
                StartInit();
        }

        public void StartInit()
        {
            if (_svc != null) return;

            _svc = new TutorialService();
            OnServiceReady?.Invoke(_svc);
        }

        private void OnDestroy()
        {
            _svc?.Stop();
            _svc = null;
        }
    }
}
