using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace TrippleQ.Tutorial
{
    /// <summary>
    /// Pure logic: owns steps + current index, controls advancing.
    /// No Unity UI dependency.
    /// </summary>
    public sealed class TutorialService
    {
        public event Action<TutorialStep, RectTransform> StepChanged; // (step, target)
        public event Action Stopped;

        public bool IsPlaying => _isPlaying;
        public int CurrentIndex => _index;

        private readonly List<TutorialStep> _steps = new();
        private readonly List<RectTransform> _targets = new();

        private ITutorialTargetResolver _resolver;
        private int _index = -1;
        private bool _isPlaying;

        public void SetResolver(ITutorialTargetResolver resolver) => _resolver = resolver;

        public void Play(IReadOnlyList<TutorialStep> steps, IReadOnlyList<RectTransform> targets)
        {
            if (steps == null) throw new ArgumentNullException(nameof(steps));
            if (targets == null) throw new ArgumentNullException(nameof(targets));
            if (steps.Count != targets.Count) throw new ArgumentException("steps.Count must equal targets.Count");

            _steps.Clear();
            _targets.Clear();
            _steps.AddRange(steps);
            _targets.AddRange(targets);

            _isPlaying = true;
            _index = -1;
            NextInternal();
        }

        public void Play(IReadOnlyList<TutorialStep> steps, IReadOnlyList<object> targetKeys)
        {
            if (_resolver == null) throw new InvalidOperationException("Resolver is null. Call SetResolver() first.");
            if (steps == null) throw new ArgumentNullException(nameof(steps));
            if (targetKeys == null) throw new ArgumentNullException(nameof(targetKeys));
            if (steps.Count != targetKeys.Count) throw new ArgumentException("steps.Count must equal targetKeys.Count");

            _steps.Clear();
            _targets.Clear();
            _steps.AddRange(steps);

            foreach (var key in targetKeys)
                _targets.Add(_resolver.Resolve(key));

            _isPlaying = true;
            _index = -1;
            NextInternal();
        }

        public void Stop()
        {
            if (!_isPlaying) return;
            _isPlaying = false;
            _index = -1;
            Stopped?.Invoke();
        }

        /// <summary>
        /// Called when user clicks the highlighted target.
        /// Completes current step then advances to next step.
        /// </summary>
        public void CompleteCurrentAndNext()
        {
            if (!_isPlaying) return;

            var step = GetCurrentStep();
            step?.OnCompleted?.Invoke();

            NextInternal();
        }

        public TutorialStep GetCurrentStep()
        {
            if (!_isPlaying) return null;
            if (_index < 0 || _index >= _steps.Count) return null;
            return _steps[_index];
        }

        public RectTransform GetCurrentTarget()
        {
            if (!_isPlaying) return null;
            if (_index < 0 || _index >= _targets.Count) return null;
            return _targets[_index];
        }

        private void NextInternal()
        {
            _index++;

            if (_index >= _steps.Count)
            {
                Stop();
                return;
            }

            var step = _steps[_index];
            var target = _targets[_index];

            // Action2 pattern: OnEnter of next step runs when it becomes active.
            step.OnEnter?.Invoke();
            StepChanged?.Invoke(step, target);
        }
    }
}
