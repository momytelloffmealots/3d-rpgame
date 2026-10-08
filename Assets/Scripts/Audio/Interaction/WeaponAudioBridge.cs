// WeaponAudioBridge.cs
// Responsibility: Integrates the Easy Weapons system with the Audio Framework for
//                 bullet impact audio. Attach to raycast hit points or use via static call.
//                 Does NOT modify Weapon.cs or Projectile.cs.
//
// Called By: External scripts, or add a custom component to weapon prefabs
// Calls: SurfaceDetector, AudioInteractionService

using UnityEngine;

namespace RPGAudio
{
    /// <summary>
    /// Provides static utility methods for weapon-related audio interactions.
    ///
    /// Usage from any gameplay script (e.g. a custom extension to Weapon.cs):
    ///   WeaponAudioBridge.PlayBulletImpact(hitInfo, firePosition);
    ///   WeaponAudioBridge.PlayExplosion(position, radius);
    ///
    /// You can also add this as a component to a weapon and subscribe to EasyWeapons events,
    /// but the static API is the simplest integration point.
    /// </summary>
    public class WeaponAudioBridge : MonoBehaviour
    {
        // ── Static API (call from anywhere) ──────────────────────────────────

        /// <summary>
        /// Play a bullet impact sound at the hit point, detected from the hit surface.
        /// </summary>
        /// <param name="hit">The RaycastHit from the bullet ray.</param>
        /// <param name="force">Force/damage to map to intensity [0..1]. Use weapon power.</param>
        public static void PlayBulletImpact(RaycastHit hit, float force = 80f)
        {
            if (AudioInteractionService.Instance == null) return;

            SurfaceType surface = SurfaceDetector.Detect(hit);
            float intensity     = Mathf.Clamp01(force / 100f);

            var ctx = AudioInteractionContext.BulletImpact(surface, hit.point, intensity);
            AudioInteractionService.Instance.Play(ctx);
        }

        /// <summary>
        /// Play a weapon impact (melee) sound. Use on collision detection.
        /// </summary>
        public static void PlayWeaponImpact(Vector3 position, SurfaceType surface, float force = 1f)
        {
            if (AudioInteractionService.Instance == null) return;

            var ctx = AudioInteractionContext.Create(InteractionType.WeaponImpact, surface, position,
                Mathf.Clamp01(force));
            AudioInteractionService.Instance.Play(ctx);
        }

        /// <summary>
        /// Play an explosion sound.
        /// </summary>
        public static void PlayExplosion(Vector3 position, float radius = 5f)
        {
            if (AudioInteractionService.Instance == null) return;

            // Map radius to intensity (large explosions = intensity 1)
            float intensity = Mathf.Clamp01(radius / 20f);
            var ctx = AudioInteractionContext.Create(InteractionType.Explosion, SurfaceType.Default, position, intensity);
            AudioInteractionService.Instance.Play(ctx);
        }

        /// <summary>
        /// Play a projectile pass-by whoosh.
        /// </summary>
        public static void PlayProjectilePass(Vector3 position, Vector3 direction, float speed)
        {
            if (AudioInteractionService.Instance == null) return;

            var ctx = AudioInteractionContext.Create(
                InteractionType.ProjectilePass, SurfaceType.Default, position,
                Mathf.Clamp01(speed / 50f));
            ctx.Direction = direction;
            ctx.Speed     = speed;
            AudioInteractionService.Instance.Play(ctx);
        }

        // ── Component API (attach and call from UnityEvents or send message) ──

        [Header("Optional - Component Mode")]
        [SerializeField, Tooltip("Surface to use when auto-detecting isn't possible.")]
        private SurfaceType manualSurface = SurfaceType.Default;

        [SerializeField, Tooltip("Interaction type for component-mode use.")]
        private InteractionType interactionType = InteractionType.BulletImpact;

        [SerializeField, Tooltip("Intensity for component-mode playback.")]
        [Range(0f, 1f)]
        private float intensity = 1f;

        /// <summary>
        /// Plays the configured interaction. Can be wired to UnityEvents.
        /// </summary>
        public void PlayConfiguredInteraction()
        {
            if (AudioInteractionService.Instance == null) return;

            var ctx = AudioInteractionContext.Create(
                interactionType, manualSurface, transform.position, intensity);
            ctx.SourceObject = gameObject;
            AudioInteractionService.Instance.Play(ctx);
        }

        /// <summary>
        /// Plays bullet impact using this component's position and auto-detected surface (via collision).
        /// Attach to Projectile prefab and call from OnCollisionEnter.
        /// </summary>
        public void PlayImpactOnCollision(Collision collision)
        {
            if (AudioInteractionService.Instance == null) return;

            SurfaceType surface = SurfaceDetector.Detect(collision);

            var ctx = AudioInteractionContext.BulletImpact(
                surface,
                collision.contacts[0].point,
                collision.relativeVelocity.magnitude / 20f);
            ctx.SourceObject = gameObject;

            AudioInteractionService.Instance.Play(ctx);
        }
    }
}
