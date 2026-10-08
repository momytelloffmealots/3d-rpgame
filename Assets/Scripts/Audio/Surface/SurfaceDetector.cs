// SurfaceDetector.cs
// Responsibility: Detects the surface type at a world position using a prioritized detection chain.
//                 Does NOT play audio — pure detection logic.
// Called By: AudioInteractionService, gameplay bridges (footstep, landing)
// Calls: SurfaceIdentifier (checks component), PhysicMaterial, Layer, Tag mapping

using UnityEngine;

namespace RPGAudio
{
    /// <summary>
    /// Detects what surface is at a given point using a priority chain:
    ///   1. SurfaceIdentifier component (highest priority)
    ///   2. Physic Material name → SurfaceType
    ///   3. Layer name → SurfaceType
    ///   4. Tag → SurfaceType
    ///   5. Default (fallback)
    ///
    /// Usage:
    ///   SurfaceType surface = SurfaceDetector.Detect(hitInfo);
    ///   SurfaceType surface = SurfaceDetector.DetectBelow(transform.position, layerMask);
    /// </summary>
    public static class SurfaceDetector
    {
        // ── Layer / PhysicsMaterial name → SurfaceType mappings ──────────────
        // These are the convention-based fallbacks.
        // Names are case-insensitive for flexibility.

        private static readonly string[] GrassKeywords    = { "grass", "lawn", "turf" };
        private static readonly string[] DirtKeywords     = { "dirt", "soil", "earth", "mud_dry" };
        private static readonly string[] StoneKeywords    = { "stone", "rock", "concrete", "pavement", "asphalt" };
        private static readonly string[] WoodKeywords     = { "wood", "wooden", "plank", "timber" };
        private static readonly string[] MetalKeywords    = { "metal", "iron", "steel", "aluminum" };
        private static readonly string[] SandKeywords     = { "sand", "desert", "beach" };
        private static readonly string[] SnowKeywords     = { "snow", "ice_snow" };
        private static readonly string[] WaterKeywords    = { "water", "puddle", "river", "lake" };
        private static readonly string[] IceKeywords      = { "ice", "frozen" };
        private static readonly string[] MudKeywords      = { "mud", "swamp", "bog" };
        private static readonly string[] GlassKeywords    = { "glass", "window" };
        private static readonly string[] MagicKeywords    = { "magic", "arcane", "ethereal" };

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Detects surface from an existing RaycastHit (highest accuracy, use when you already have the hit).
        /// </summary>
        public static SurfaceType Detect(RaycastHit hit)
        {
            if (hit.collider == null) return SurfaceType.Default;

            GameObject obj = hit.collider.gameObject;

            // Priority 1: SurfaceIdentifier component
            var identifier = obj.GetComponentInParent<SurfaceIdentifier>();
            if (identifier != null) return identifier.Surface;

            // Priority 2: PhysicsMaterial
            if (hit.collider.sharedMaterial != null)
            {
                SurfaceType fromPhysic = FromName(hit.collider.sharedMaterial.name);
                if (fromPhysic != SurfaceType.Default) return fromPhysic;
            }

            // Priority 3: Layer
            SurfaceType fromLayer = FromName(LayerMask.LayerToName(obj.layer));
            if (fromLayer != SurfaceType.Default) return fromLayer;

            // Priority 4: Tag
            SurfaceType fromTag = FromName(obj.tag);
            if (fromTag != SurfaceType.Default) return fromTag;

            return SurfaceType.Default;
        }

        /// <summary>
        /// Casts a ray downward from the given position to detect the surface below.
        /// Useful for footstep / landing detection.
        /// </summary>
        /// <param name="origin">World position to cast from (typically character's feet + small offset).</param>
        /// <param name="rayDistance">Max ray length.</param>
        /// <param name="layerMask">Layers to consider. 0 = everything.</param>
        public static SurfaceType DetectBelow(Vector3 origin, float rayDistance = 2f, int layerMask = 0)
        {
            Ray ray = new Ray(origin + Vector3.up * 0.1f, Vector3.down);
            RaycastHit hit;

            bool didHit = layerMask == 0
                ? Physics.Raycast(ray, out hit, rayDistance)
                : Physics.Raycast(ray, out hit, rayDistance, layerMask);

            return didHit ? Detect(hit) : SurfaceType.Default;
        }

        /// <summary>
        /// Detects surface using a custom ray direction (for bullet impacts, etc.).
        /// </summary>
        public static SurfaceType DetectRay(Vector3 origin, Vector3 direction, float distance = 100f, int layerMask = 0)
        {
            Ray ray = new Ray(origin, direction);
            RaycastHit hit;

            bool didHit = layerMask == 0
                ? Physics.Raycast(ray, out hit, distance)
                : Physics.Raycast(ray, out hit, distance, layerMask);

            return didHit ? Detect(hit) : SurfaceType.Default;
        }

        /// <summary>
        /// Detects surface from a Collision object (use in OnCollisionEnter).
        /// </summary>
        public static SurfaceType Detect(Collision collision)
        {
            if (collision == null || collision.contactCount == 0) return SurfaceType.Default;

            ContactPoint contact = collision.contacts[0];
            GameObject obj = collision.gameObject;

            // Priority 1: SurfaceIdentifier
            var identifier = obj.GetComponentInParent<SurfaceIdentifier>();
            if (identifier != null) return identifier.Surface;

            // Priority 2: PhysicsMaterial
            if (contact.otherCollider.sharedMaterial != null)
            {
                SurfaceType fromPhysic = FromName(contact.otherCollider.sharedMaterial.name);
                if (fromPhysic != SurfaceType.Default) return fromPhysic;
            }

            // Priority 3: Layer
            SurfaceType fromLayer = FromName(LayerMask.LayerToName(obj.layer));
            if (fromLayer != SurfaceType.Default) return fromLayer;

            // Priority 4: Tag
            SurfaceType fromTag = FromName(obj.tag);
            if (fromTag != SurfaceType.Default) return fromTag;

            return SurfaceType.Default;
        }

        // ── Internal name matching ─────────────────────────────────────────────

        private static SurfaceType FromName(string name)
        {
            if (string.IsNullOrEmpty(name)) return SurfaceType.Default;
            string lower = name.ToLower();

            if (ContainsAny(lower, GrassKeywords))  return SurfaceType.Grass;
            if (ContainsAny(lower, MetalKeywords))  return SurfaceType.Metal;
            if (ContainsAny(lower, StoneKeywords))  return SurfaceType.Stone;
            if (ContainsAny(lower, WoodKeywords))   return SurfaceType.Wood;
            if (ContainsAny(lower, WaterKeywords))  return SurfaceType.Water;
            if (ContainsAny(lower, SandKeywords))   return SurfaceType.Sand;
            if (ContainsAny(lower, SnowKeywords))   return SurfaceType.Snow;
            if (ContainsAny(lower, IceKeywords))    return SurfaceType.Ice;
            if (ContainsAny(lower, MudKeywords))    return SurfaceType.Mud;
            if (ContainsAny(lower, GlassKeywords))  return SurfaceType.Glass;
            if (ContainsAny(lower, DirtKeywords))   return SurfaceType.Dirt;
            if (ContainsAny(lower, MagicKeywords))  return SurfaceType.Magic;

            return SurfaceType.Default;
        }

        private static bool ContainsAny(string source, string[] keywords)
        {
            foreach (var kw in keywords)
                if (source.Contains(kw)) return true;
            return false;
        }
    }
}
