// AudioInteractionService.cs
// Responsibility: Central orchestrator. Receives AudioInteractionContext, finds the right profile and data,
//                 selects clip via variation, checks voice management, plays via pool.
//                 This is the ONLY class gameplay needs to talk to.
// Called By: Gameplay bridges (ThirdPersonAudioBridge, WeaponAudioBridge, etc.), any script needing audio.
// Calls: AudioInteractionDatabase, AudioInteractionProfile, AudioVariationUtility,
//         AudioVoiceManager, AudioSourcePool, AudioMixerController, AudioDebugger

using UnityEngine;
using UnityEngine.Audio;

namespace RPGAudio
{
    /// <summary>
    /// The single entry point for all audio interaction playback.
    ///
    /// Usage from gameplay:
    ///   var ctx = AudioInteractionContext.Footstep(SurfaceType.Grass, transform.position);
    ///   AudioInteractionService.Instance.Play(ctx);
    ///
    /// Never call AudioSource directly from gameplay scripts.
    ///
    /// Setup:
    ///   1. Add to a dedicated "AudioManager" GameObject.
    ///   2. Assign Database, Pool, and MixerController in Inspector.
    ///   3. Optionally assign Debugger.
    /// </summary>
    public class AudioInteractionService : MonoBehaviour
    {
        // ── Singleton ─────────────────────────────────────────────────────────
        private static AudioInteractionService _instance;
        public static AudioInteractionService Instance
        {
            get
            {
                if (_instance == null)
                    Debug.LogError("[AudioInteractionService] Instance not found. " +
                                  "Ensure the AudioManager GameObject is in the scene.");
                return _instance;
            }
        }

        // ── Inspector References ──────────────────────────────────────────────
        [Header("Core References")]
        [SerializeField, Tooltip("ScriptableObject mapping surfaces to audio profiles.")]
        private AudioInteractionDatabase database;

        [SerializeField, Tooltip("AudioSource pool component.")]
        private AudioSourcePool pool;

        [SerializeField, Tooltip("Mixer controller for group lookups.")]
        private AudioMixerController mixerController;

        [Header("Occlusion")]
        [SerializeField, Tooltip("Enable basic occlusion for 3D sounds.")]
        private bool enableOcclusion = false;

        [SerializeField, Tooltip("Occlusion volume attenuation when blocked (0=silent, 1=no change).")]
        [Range(0f, 1f)]
        private float occlusionAttenuation = 0.3f;

        [Header("Voice Management")]
        [SerializeField, Tooltip("Global maximum simultaneous voices.")]
        private int globalMaxVoices = 32;

        [Header("Clip Selection")]
        [SerializeField, Tooltip("Default clip selection strategy.")]
        private AudioVariationUtility.SelectionStrategy defaultStrategy
            = AudioVariationUtility.SelectionStrategy.NoRepeat;

        [Header("Debug")]
        [SerializeField, Tooltip("Reference to the Audio Debugger (optional).")]
        private AudioDebugger debugger;

        // ── Internal systems ──────────────────────────────────────────────────
        private AudioVariationUtility _variation;
        private AudioVoiceManager     _voiceManager;

        // ── Unity lifecycle ────────────────────────────────────────────────────
        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Debug.LogWarning("[AudioInteractionService] Duplicate instance destroyed.");
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);

            _variation    = new AudioVariationUtility();
            _voiceManager = new AudioVoiceManager(globalMaxVoices);

            ValidateSetup();
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Primary API. Sends a context to the audio system for playback.
        /// Returns the PooledAudioSource playing this sound (null if rejected).
        /// </summary>
        public PooledAudioSource Play(AudioInteractionContext context)
        {
            if (context == null)
            {
                AudioDebugger.LogWarning("Play() called with null context.");
                return null;
            }

            // 1. Find profile
            AudioInteractionProfile profile = FindProfile(context.Surface);
            if (profile == null)
            {
                AudioDebugger.LogWarning($"No profile found for surface '{context.Surface}'. " +
                                        $"Interaction: {context.Interaction}");
                return null;
            }

            // 2. Find interaction data — with fallback to Default profile
            AudioInteractionData data = profile.GetInteractionData(context.Interaction);
            if (data == null || !data.HasClips)
            {
                // Fallback: try Default surface profile
                if (context.Surface != SurfaceType.Default)
                {
                    var defaultProfile = database?.GetProfile(SurfaceType.Default);
                    data = defaultProfile?.GetInteractionData(context.Interaction);
                }

                if (data == null || !data.HasClips)
                {
                    AudioDebugger.LogWarning(
                        $"No audio data for [{context.Interaction}] on surface [{context.Surface}]. " +
                        "Add clips to the profile or DefaultProfile.");
                    return null;
                }
            }

            // 3. Voice management — check concurrency / cooldown
            string cooldownKey = BuildCooldownKey(context);
            bool voiceAllowed  = _voiceManager.RequestVoice(
                context.Interaction,
                data.priority,
                cooldownKey,
                data.cooldown,
                pool,
                out _);

            if (!voiceAllowed)
            {
                AudioDebugger.LogVerbose(
                    $"Voice rejected for {context.Interaction}/{context.Surface} (cooldown or limit).");
                return null;
            }

            // 4. Select clip via variation
            string variationKey = $"{context.SourceObject?.GetInstanceID()}_{context.Interaction}_{context.Surface}";
            AudioClip clip = _variation.SelectClip(data, defaultStrategy, variationKey);
            if (clip == null)
            {
                AudioDebugger.LogWarning($"SelectClip returned null for {context.Interaction}/{context.Surface}.");
                return null;
            }

            // 5. Compute final volume / pitch
            float volume = _variation.GetVolume(data, context.Intensity);
            float pitch  = _variation.GetPitch(data);

            // 6. Get mixer group
            AudioMixerGroup group = ResolveGroup(profile, context.Interaction);

            // 7. Acquire source from pool
            PooledAudioSource source = pool.Acquire();
            if (source == null) return null;

            // 8. Play
            source.Play(
                clip,
                context.Position,
                volume,
                pitch,
                data.spatialMinDistance,
                data.spatialMaxDistance,
                data.spatialBlend,
                group);

            // 9. Optionally add occlusion
            if (enableOcclusion && data.spatialBlend > 0.5f)
            {
                var occ = source.GetComponent<AudioOcclusion>()
                       ?? source.gameObject.AddComponent<AudioOcclusion>();
                occ.Initialize(volume);
            }

            // 10. Register with voice manager
            _voiceManager.RegisterVoice(source, context.Interaction, data.priority, cooldownKey, data.cooldown);

            // 11. Debug log
            debugger?.LogPlay(context, profile, data, clip, volume, pitch);

            return source;
        }

        /// <summary>
        /// Convenience overload: build and play context in one call.
        /// </summary>
        public PooledAudioSource Play(
            InteractionType interaction,
            SurfaceType surface,
            Vector3 position,
            float intensity = 1f,
            GameObject sourceObject = null)
        {
            var ctx = AudioInteractionContext.Create(interaction, surface, position, intensity);
            ctx.SourceObject = sourceObject;
            return Play(ctx);
        }

        /// <summary>
        /// Manually stops a looping voice and returns it to the pool.
        /// </summary>
        public void Stop(PooledAudioSource source)
        {
            if (source == null) return;
            _voiceManager.UnregisterVoice(source);
            pool.ReturnToPool(source);
        }

        /// <summary>Exposes voice manager for diagnostics.</summary>
        public AudioVoiceManager VoiceManager => _voiceManager;

        // ── Internal helpers ──────────────────────────────────────────────────

        private AudioInteractionProfile FindProfile(SurfaceType surface)
        {
            if (database == null) return null;
            return database.GetProfile(surface);
        }

        private string BuildCooldownKey(AudioInteractionContext ctx)
        {
            int sourceID = ctx.SourceObject != null ? ctx.SourceObject.GetInstanceID() : 0;
            return $"{sourceID}_{ctx.Interaction}";
        }

        private AudioMixerGroup ResolveGroup(AudioInteractionProfile profile, InteractionType type)
        {
            if (mixerController == null) return null;
            string groupName = profile.GetMixerGroup(type);
            return mixerController.GetGroup(groupName);
        }

        private void ValidateSetup()
        {
            if (database == null)
                Debug.LogError("[AudioInteractionService] Database is not assigned!", this);
            if (pool == null)
                Debug.LogError("[AudioInteractionService] AudioSourcePool is not assigned!", this);
        }
    }
}
