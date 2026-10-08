// AudioInteractionDatabase.cs
// Responsibility: ScriptableObject that maps SurfaceType → AudioInteractionProfile.
//                 Acts as the single source of truth for which profile to use per surface.
// Called By: AudioInteractionService
// Calls: AudioInteractionProfile

using UnityEngine;
using System.Collections.Generic;

namespace RPGAudio
{
    /// <summary>
    /// Database mapping surface types to audio profiles.
    ///
    /// HOW TO CREATE:
    ///   Right-click in Project → Create → RPG Audio → Interaction Database
    ///
    /// Place ONE instance in your project. AudioInteractionService references it.
    /// Fallback order: Surface Profile → Default Profile → null (warning logged).
    /// </summary>
    [CreateAssetMenu(
        menuName = "RPG Audio/Interaction Database",
        fileName = "AudioInteractionDatabase",
        order = 0)]
    public class AudioInteractionDatabase : ScriptableObject
    {
        [System.Serializable]
        public class SurfaceProfileEntry
        {
            public SurfaceType surface;
            public AudioInteractionProfile profile;
        }

        [Tooltip("Map each surface type to its audio profile.")]
        [SerializeField] private List<SurfaceProfileEntry> entries = new List<SurfaceProfileEntry>();

        [Tooltip("Fallback profile used when no entry matches the requested surface.")]
        [SerializeField] private AudioInteractionProfile defaultProfile;

        // ── Fast lookup ────────────────────────────────────────────────────────
        private Dictionary<SurfaceType, AudioInteractionProfile> _lookup;

        private void OnEnable()
        {
            BuildLookup();
        }

        private void OnValidate()
        {
            BuildLookup();
        }

        private void BuildLookup()
        {
            _lookup = new Dictionary<SurfaceType, AudioInteractionProfile>();
            if (entries == null) return;

            foreach (var entry in entries)
            {
                if (entry == null || entry.profile == null) continue;
                if (_lookup.ContainsKey(entry.surface))
                {
                    Debug.LogWarning(
                        $"[AudioDatabase] Duplicate surface entry: {entry.surface}. Using first.", this);
                    continue;
                }
                _lookup[entry.surface] = entry.profile;
            }
        }

        /// <summary>
        /// Returns the profile for the requested surface.
        /// Falls back to Default surface profile, then to the explicit defaultProfile.
        /// Returns null only if nothing is configured at all.
        /// </summary>
        public AudioInteractionProfile GetProfile(SurfaceType surface)
        {
            if (_lookup == null) BuildLookup();

            // Exact match
            if (_lookup.TryGetValue(surface, out var profile) && profile != null)
                return profile;

            // Default surface entry
            if (surface != SurfaceType.Default &&
                _lookup.TryGetValue(SurfaceType.Default, out var defFromMap) && defFromMap != null)
                return defFromMap;

            // Explicit default profile field
            if (defaultProfile != null)
                return defaultProfile;

            return null;
        }

        /// <summary>
        /// Returns true if a profile (or default) exists for the surface.
        /// </summary>
        public bool HasProfile(SurfaceType surface)
        {
            return GetProfile(surface) != null;
        }
    }
}
