using System;
using UnityEngine;

namespace TrippleQ.Tutorial
{
    /// <summary>
    /// One tutorial step: highlight a target + show description.
    /// Actions are runtime-only (not serialized).
    /// </summary>
    public sealed class TutorialStep
    {
        public readonly string Id;
        public readonly string Description;

        /// <summary>Executed when step becomes active.</summary>
        public readonly Action OnEnter;

        /// <summary>Executed when user clicks target and step completes.</summary>
        public readonly Action OnCompleted;

        public TutorialStep(string id, string description, Action onEnter = null, Action onCompleted = null)
        {
            Id = id;
            Description = description ?? string.Empty;
            OnEnter = onEnter;
            OnCompleted = onCompleted;
        }
    }
}
