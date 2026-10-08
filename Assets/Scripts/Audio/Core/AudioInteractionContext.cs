// AudioInteractionContext.cs
// Responsibility: Central data structure carrying all information about one audio interaction request.
//                 Gameplay sends this; the service reads it.
// Called By: AudioInteractionService, any gameplay script (ThirdPersonController bridge, Weapon bridge, etc.)
// Calls: Nothing (pure data)

using UnityEngine;

namespace RPGAudio
{
    /// <summary>
    /// Carries the full context of a single audio interaction request.
    /// Not every field is required for every interaction — unused fields stay at default values.
    ///
    /// Example usage:
    ///   var ctx = AudioInteractionContext.Footstep(SurfaceType.Grass, transform.position, speed: 3f);
    ///   AudioInteractionService.Instance.Play(ctx);
    /// </summary>
    public class AudioInteractionContext
    {
        // ── Required ──────────────────────────────────────────────────────────────
        /// <summary>What kind of interaction is happening (Footstep, BulletImpact, etc.)</summary>
        public InteractionType Interaction { get; set; }

        /// <summary>The physical surface this interaction targets.</summary>
        public SurfaceType Surface { get; set; } = SurfaceType.Default;

        /// <summary>World-space position where the audio should play.</summary>
        public Vector3 Position { get; set; }

        // ── Optional contextual data ───────────────────────────────────────────
        /// <summary>Normalized intensity [0..1]. Controls volume scale and clip tier selection.</summary>
        public float Intensity { get; set; } = 1f;

        /// <summary>Movement speed of the instigator (used for footstep timing / variation).</summary>
        public float Speed { get; set; }

        /// <summary>Impact force magnitude (for physical collision events).</summary>
        public float Force { get; set; }

        /// <summary>Distance from the listener (for culling; calculated automatically if left 0).</summary>
        public float Distance { get; set; }

        /// <summary>Direction of movement or projectile travel.</summary>
        public Vector3 Direction { get; set; }

        /// <summary>The GameObject that initiated the interaction (player, enemy, etc.).</summary>
        public GameObject SourceObject { get; set; }

        /// <summary>The GameObject that was hit or interacted with.</summary>
        public GameObject TargetObject { get; set; }

        /// <summary>Optional environment tag (e.g. "Indoor", "Cave") for reverb / variation.</summary>
        public string EnvironmentTag { get; set; } = string.Empty;

        /// <summary>Priority override [0 = lowest, 255 = highest]. 0 uses profile default.</summary>
        public int PriorityOverride { get; set; } = 0;

        // ── Factory helpers ────────────────────────────────────────────────────
        /// <summary>Create a footstep context quickly.</summary>
        public static AudioInteractionContext Footstep(
            SurfaceType surface, Vector3 position, float speed = 0f, float intensity = 0.8f)
        {
            return new AudioInteractionContext
            {
                Interaction = InteractionType.Footstep,
                Surface     = surface,
                Position    = position,
                Speed       = speed,
                Intensity   = Mathf.Clamp01(intensity)
            };
        }

        /// <summary>Create a landing context quickly.</summary>
        public static AudioInteractionContext Landing(
            SurfaceType surface, Vector3 position, float intensity = 1f)
        {
            return new AudioInteractionContext
            {
                Interaction = InteractionType.Landing,
                Surface     = surface,
                Position    = position,
                Intensity   = Mathf.Clamp01(intensity)
            };
        }

        /// <summary>Create a bullet-impact context quickly.</summary>
        public static AudioInteractionContext BulletImpact(
            SurfaceType surface, Vector3 position, float force = 1f)
        {
            return new AudioInteractionContext
            {
                Interaction = InteractionType.BulletImpact,
                Surface     = surface,
                Position    = position,
                Force       = force,
                Intensity   = Mathf.Clamp01(force)
            };
        }

        /// <summary>Create a generic interaction context.</summary>
        public static AudioInteractionContext Create(
            InteractionType type, SurfaceType surface, Vector3 position, float intensity = 1f)
        {
            return new AudioInteractionContext
            {
                Interaction = type,
                Surface     = surface,
                Position    = position,
                Intensity   = Mathf.Clamp01(intensity)
            };
        }

        public override string ToString()
        {
            return $"[Context] {Interaction} on {Surface} | pos={Position} | intensity={Intensity:F2}";
        }
    }
}
