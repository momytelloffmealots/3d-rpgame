// AudioInteractionData.cs
// Responsibility: Serializable data for one interaction type within a profile.
//                 Contains clips, volume/pitch ranges, intensity curve, concurrency, cooldown.
// Called By: AudioInteractionProfile
// Calls: Nothing (pure data)

using UnityEngine;

namespace RPGAudio
{
    /// <summary>
    /// Holds audio data for a SINGLE interaction type (e.g. Footstep) within a surface profile.
    /// One AudioInteractionProfile contains an array of these entries.
    /// </summary>
    [System.Serializable]
    public class AudioInteractionData
    {
        [Tooltip("Which interaction type this data handles.")]
        public InteractionType interactionType = InteractionType.Footstep;

        [Tooltip("Audio clips for this interaction. One is chosen per play based on variation settings.")]
        public AudioClip[] clips;

        [Header("Volume")]
        [Tooltip("Base volume for this interaction.")]
        [Range(0f, 1f)]
        public float baseVolume = 1f;

        [Tooltip("Random volume variation added to baseVolume each play. Range: [-volumeVariation, +volumeVariation].")]
        [Range(0f, 0.5f)]
        public float volumeVariation = 0.1f;

        [Header("Pitch")]
        [Tooltip("Minimum pitch multiplier.")]
        [Range(0.5f, 2f)]
        public float pitchMin = 0.9f;

        [Tooltip("Maximum pitch multiplier.")]
        [Range(0.5f, 2f)]
        public float pitchMax = 1.1f;

        [Header("Intensity")]
        [Tooltip("Curve mapping intensity [0..1] → final volume scale. Allows soft/heavy footsteps.")]
        public AnimationCurve intensityCurve = AnimationCurve.Linear(0f, 0.3f, 1f, 1f);

        [Header("Spatial Settings")]
        [Tooltip("Min distance at which attenuation begins.")]
        public float spatialMinDistance = 1f;

        [Tooltip("Max distance at which sound is inaudible.")]
        public float spatialMaxDistance = 20f;

        [Tooltip("3D spatial blend. 0 = 2D, 1 = full 3D.")]
        [Range(0f, 1f)]
        public float spatialBlend = 1f;

        [Header("Concurrency / Cooldown")]
        [Tooltip("Maximum simultaneous instances of this specific interaction. 0 = unlimited.")]
        public int maxConcurrentInstances = 4;

        [Tooltip("Minimum time (seconds) before this interaction can play again from the same source.")]
        public float cooldown = 0f;

        [Header("Priority")]
        [Tooltip("Voice priority [0=lowest, 255=highest]. Higher priority voices survive under voice limits.")]
        [Range(0, 255)]
        public int priority = 128;

        [Header("Mixer Group")]
        [Tooltip("Mixer group override. If empty, uses the profile's default group.")]
        public string mixerGroupName = "";

        // ── Runtime helpers ────────────────────────────────────────────────────

        /// <summary>
        /// Computes final volume factoring in intensity and base volume.
        /// </summary>
        public float GetFinalVolume(float intensity)
        {
            float intensityScale = intensityCurve.Evaluate(Mathf.Clamp01(intensity));
            return Mathf.Clamp01(baseVolume * intensityScale);
        }

        /// <summary>Returns true if this entry has at least one clip assigned.</summary>
        public bool HasClips => clips != null && clips.Length > 0;
    }
}
