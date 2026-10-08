// ThirdPersonAudioBridge.cs
// Responsibility: Bridges the ThirdPersonController's OnFootstep/OnLand animation events
//                 to the AudioInteractionService. Replaces the hard-coded audio in ThirdPersonController
//                 WITHOUT modifying ThirdPersonController.cs itself.
//
// HOW IT WORKS:
//   - Add this component to the PlayerArmature GameObject alongside ThirdPersonController.
//   - ThirdPersonController's OnFootstep/OnLand animation events call methods on this component.
//   - This component uses SurfaceDetector + AudioInteractionService to play the right audio.
//
// Called By: Unity Animator animation events (OnFootstep, OnLand)
// Calls: SurfaceDetector, AudioInteractionService

using UnityEngine;
using StarterAssets;

namespace RPGAudio
{
    /// <summary>
    /// Add this to the PlayerArmature to enable surface-aware footstep and landing audio.
    ///
    /// Setup:
    ///   1. Add to PlayerArmature (same object as ThirdPersonController).
    ///   2. In ThirdPersonController Inspector: CLEAR FootstepAudioClips and LandingAudioClip
    ///      (this bridge replaces them — leave them empty to avoid double-play).
    ///   3. Configure: surfaceRayDistance, surfaceLayerMask.
    ///   4. Ensure AudioInteractionService is in scene.
    ///
    /// The animation already calls OnFootstep/OnLand events — this bridge intercepts them.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class ThirdPersonAudioBridge : MonoBehaviour
    {
        [Header("Surface Detection")]
        [SerializeField, Tooltip("Max raycast distance for surface detection below feet.")]
        private float surfaceRayDistance = 1.5f;

        [SerializeField, Tooltip("Layers to consider as ground. Should match ThirdPersonController's GroundLayers.")]
        private LayerMask surfaceLayerMask = ~0;

        [Header("Intensity Mapping")]
        [SerializeField, Tooltip("Walk footstep intensity (quiet).")]
        [Range(0f, 1f)]
        private float walkIntensity = 0.5f;

        [SerializeField, Tooltip("Sprint footstep intensity (louder).")]
        [Range(0f, 1f)]
        private float sprintIntensity = 0.85f;

        [SerializeField, Tooltip("Landing intensity. Overridden by fall distance if calculateLandingIntensity is true.")]
        [Range(0f, 1f)]
        private float defaultLandingIntensity = 0.9f;

        [SerializeField, Tooltip("If true, landing intensity is calculated from vertical velocity at impact.")]
        private bool calculateLandingIntensity = true;

        [SerializeField, Tooltip("Vertical velocity mapped to intensity=1.0 (heavy landing).")]
        private float maxFallVelocityForIntensity = 15f;

        // ── Internal references ───────────────────────────────────────────────
        private CharacterController _controller;
        private StarterAssetsInputs  _inputs;
        private float               _previousVerticalVelocity;

        private void Awake()
        {
            _controller = GetComponent<CharacterController>();
            _inputs     = GetComponent<StarterAssetsInputs>();
        }

        private void Update()
        {
            // Track vertical velocity for landing intensity calculation
            _previousVerticalVelocity = _controller.velocity.y;
        }

        // ── Animation Event Receivers ──────────────────────────────────────────

        /// <summary>
        /// Called by the Animator's footstep animation events (same method name as ThirdPersonController).
        /// </summary>
        private void OnFootstep(AnimationEvent animationEvent)
        {
            // Only play on weighted clips (avoids double-play during transitions)
            if (animationEvent.animatorClipInfo.weight < 0.5f) return;

            if (AudioInteractionService.Instance == null) return;

            SurfaceType surface = SurfaceDetector.DetectBelow(
                transform.position,
                surfaceRayDistance,
                surfaceLayerMask);

            bool isSprinting = _inputs != null && _inputs.sprint;
            float intensity  = isSprinting ? sprintIntensity : walkIntensity;

            Vector3 feetPosition = transform.TransformPoint(_controller.center) + Vector3.down * (_controller.height * 0.5f);

            var ctx = AudioInteractionContext.Footstep(surface, feetPosition, _controller.velocity.magnitude, intensity);
            ctx.SourceObject = gameObject;

            AudioInteractionService.Instance.Play(ctx);
        }

        /// <summary>
        /// Called by the Animator's landing animation event.
        /// </summary>
        private void OnLand(AnimationEvent animationEvent)
        {
            if (animationEvent.animatorClipInfo.weight < 0.5f) return;

            if (AudioInteractionService.Instance == null) return;

            SurfaceType surface = SurfaceDetector.DetectBelow(
                transform.position,
                surfaceRayDistance,
                surfaceLayerMask);

            float intensity = defaultLandingIntensity;
            if (calculateLandingIntensity)
            {
                // Impact velocity = absolute previous downward velocity
                float impactVelocity = Mathf.Abs(_previousVerticalVelocity);
                intensity = Mathf.Clamp01(impactVelocity / maxFallVelocityForIntensity);
                intensity = Mathf.Max(intensity, 0.2f); // minimum audible landing
            }

            var ctx = AudioInteractionContext.Landing(surface, transform.position, intensity);
            ctx.SourceObject = gameObject;

            AudioInteractionService.Instance.Play(ctx);
        }

        private void OnDrawGizmosSelected()
        {
            // Visualize surface detection ray
            Gizmos.color = Color.yellow;
            Vector3 origin = transform.position + Vector3.up * 0.1f;
            Gizmos.DrawLine(origin, origin + Vector3.down * surfaceRayDistance);
        }
    }
}
