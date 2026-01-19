using System;
using UnityEngine;

namespace TrippleQ.Tutorial
{
    public interface ITutorialOverlayView
    {
        /// <summary>Fired when user clicks inside the current target area.</summary>
        event Action TargetClicked;

        void Show(string description, RectTransform target);
        void Hide();

        /// <summary>Keep visuals aligned if target moves/layout changes.</summary>
        void SyncToTarget(RectTransform target);

        void EnableHoleClick(bool enable); // NEW
    }
}
