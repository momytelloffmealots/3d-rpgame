// AudioInteractionProfile.cs
// Responsibility: ScriptableObject that holds all audio data for ONE surface type.
//                 Contains multiple AudioInteractionData entries (one per interaction type).
// Called By: AudioInteractionDatabase, AudioInteractionService
// Calls: AudioInteractionData

using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace RPGAudio
{
    /// <summary>
    /// ScriptableObject representing the audio profile for a single surface type.
    ///
    /// HOW TO CREATE:
    ///   Right-click in Project → Create → RPG Audio → Interaction Profile
    ///
    /// Each profile maps multiple interaction types to their audio data.
    /// Example: A "Grass" profile contains data for Footstep, Landing, Jump, BulletImpact, etc.
    /// </summary>
    [CreateAssetMenu(
        menuName = "RPG Audio/Interaction Profile",
        fileName = "New_AudioInteractionProfile",
        order = 1)]
    public class AudioInteractionProfile : ScriptableObject
    {
        [Tooltip("Human-readable name for this profile (e.g. 'Grass', 'Metal').")]
        [SerializeField] private string profileName = "New Profile";

        [Tooltip("The surface type this profile represents.")]
        [SerializeField] private SurfaceType surfaceType = SurfaceType.Default;

        [Tooltip("Default mixer group for sounds from this profile. Can be overridden per interaction.")]
        [SerializeField] private string defaultMixerGroupName = "SFX";

        [Tooltip("All interaction entries for this surface.")]
        [SerializeField] private List<AudioInteractionData> interactions = new List<AudioInteractionData>();

        // ── Properties ────────────────────────────────────────────────────────
        public string ProfileName    => profileName;
        public SurfaceType Surface   => surfaceType;
        public string DefaultMixerGroupName => defaultMixerGroupName;

        // ── Fast lookup cache ──────────────────────────────────────────────────
        private Dictionary<InteractionType, AudioInteractionData> _cache;

        private void OnEnable()
        {
            BuildCache();
        }

        private void BuildCache()
        {
            _cache = new Dictionary<InteractionType, AudioInteractionData>();
            if (interactions == null) return;

            foreach (var data in interactions)
            {
                if (data == null) continue;
                // Last entry wins if duplicates exist (warn in editor)
                if (_cache.ContainsKey(data.interactionType))
                {
                    Debug.LogWarning(
                        $"[AudioProfile:{profileName}] Duplicate entry for {data.interactionType}. Using last.", this);
                }
                _cache[data.interactionType] = data;
            }
        }

        /// <summary>
        /// Gets audio data for the specified interaction type.
        /// Returns null if not found — caller should use fallback logic.
        /// </summary>
        public AudioInteractionData GetInteractionData(InteractionType type)
        {
            // Rebuild cache if needed (e.g. after domain reload)
            if (_cache == null) BuildCache();

            _cache.TryGetValue(type, out AudioInteractionData data);
            return data;
        }

        /// <summary>Returns true if this profile has data for the given interaction type.</summary>
        public bool HasInteraction(InteractionType type)
        {
            if (_cache == null) BuildCache();
            return _cache.ContainsKey(type) && _cache[type].HasClips;
        }

        /// <summary>Returns the effective mixer group name for a given interaction type.</summary>
        public string GetMixerGroup(InteractionType type)
        {
            if (_cache == null) BuildCache();

            if (_cache.TryGetValue(type, out var data) && !string.IsNullOrEmpty(data.mixerGroupName))
                return data.mixerGroupName;

            return defaultMixerGroupName;
        }

        // For editor validation
        private void OnValidate()
        {
            BuildCache();
        }
    }
}
