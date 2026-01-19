using System.Collections.Generic;
using UnityEngine;

namespace TrippleQ.Tutorial
{
    /// <summary>
    /// Simple driver to test tutorial flow in a sample scene.
    /// Controls:
    /// - T: Start tutorial
    /// - Y: Stop tutorial
    /// </summary>
    public class TutorialTestDriver : MonoBehaviour
    {
        [Header("Refs")]
        [SerializeField] private TutorialOverlayController _controller;

        [Header("Targets (order matters)")]
        [SerializeField] private RectTransform _target1;
        [SerializeField] private RectTransform _target2;
        [SerializeField] private RectTransform _target3;

        [Header("Step Text")]
        [SerializeField] private string _step1Text = "Tap this button to continue.";
        [SerializeField] private string _step2Text = "Great! Now tap the next one.";
        [SerializeField] private string _step3Text = "Awesome! Tutorial complete.";

        private void Awake()
        {
            if (_controller == null)
                Debug.LogError("[TutorialTestDriver] Missing TutorialOverlayController reference.", this);
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.T))
                StartTutorial();

            if (Input.GetKeyDown(KeyCode.Y))
                StopTutorial();
        }

        public void StopTutorial()
        {
            _controller?.Stop();
        }

        public void StartTutorial()
        {
            if (_controller == null) return;

            // Action1 -> Action2 demo:
            // Step1.OnEnter runs immediately when tutorial starts.
            // When user clicks Target1, tutorial advances -> Step2.OnEnter runs (action2).
            var steps = new List<TutorialStep>
            {
                new TutorialStep(
                    id: "Step1",
                    description: _step1Text,
                    onEnter: () => 
                    {
                        Debug.Log("[Tutorial] Enter Step1 (Action1)");
                    },
                    onCompleted: () => Debug.Log("[Tutorial] Complete Step1")
                ),
                new TutorialStep(
                    id: "Step2",
                    description: _step2Text,
                    onEnter: () => Debug.Log("[Tutorial] Enter Step2 (Action2)"),
                    onCompleted: () => Debug.Log("[Tutorial] Complete Step2")
                ),
                new TutorialStep(
                    id: "Step3",
                    description: _step3Text,
                    onEnter: () => Debug.Log("[Tutorial] Enter Step3"),
                    onCompleted: () => Debug.Log("[Tutorial] Complete Step3")
                ),
            };

            var targets = new List<RectTransform> { _target1, _target2, _target3 };

            _controller.Play(steps, targets);
        }
    }
}
