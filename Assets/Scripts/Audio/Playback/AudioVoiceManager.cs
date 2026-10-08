// AudioVoiceManager.cs
// Responsibility: Tracks active voices, enforces max voice limits, handles priority and voice stealing.
//                 Does NOT play audio — only decides if/when a voice should be allowed.
// Called By: AudioInteractionService
// Calls: PooledAudioSource (via release)

using UnityEngine;
using System.Collections.Generic;

namespace RPGAudio
{
    /// <summary>
    /// Manages active audio voices:
    ///   - Tracks how many voices are active per category (InteractionType)
    ///   - Enforces global max voice limits
    ///   - Priority-based voice stealing when limits are reached
    ///   - Per-interaction cooldown tracking
    ///
    /// Voice entry holds: source, priority, interaction type, and spawn time (for age-based stealing).
    /// </summary>
    public class AudioVoiceManager
    {
        [System.Serializable]
        public class VoiceEntry
        {
            public PooledAudioSource Source;
            public int               Priority;
            public InteractionType   Interaction;
            public float             StartTime;
            public string            CooldownKey; // "SourceInstanceID_InteractionType"
        }

        // ── Config ────────────────────────────────────────────────────────────
        private int _globalMaxVoices;

        // Per-interaction type concurrency limits
        private readonly Dictionary<InteractionType, int> _maxConcurrency
            = new Dictionary<InteractionType, int>
            {
                { InteractionType.Footstep,    8  },
                { InteractionType.BulletImpact,12 },
                { InteractionType.Explosion,   6  },
                { InteractionType.WeaponSwing, 4  },
                { InteractionType.WeaponImpact,8  },
                { InteractionType.MagicCast,   4  },
                { InteractionType.MagicImpact, 6  },
            };

        // ── State ─────────────────────────────────────────────────────────────
        private readonly List<VoiceEntry>                   _activeVoices  = new List<VoiceEntry>();
        private readonly Dictionary<string, float>          _cooldownTimers = new Dictionary<string, float>();

        public AudioVoiceManager(int globalMaxVoices = 32)
        {
            _globalMaxVoices = globalMaxVoices;
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Checks whether a new voice is allowed given current state and policy.
        /// If global or per-type limits are reached, attempts voice stealing.
        /// </summary>
        /// <returns>True if a voice slot is available (may steal).</returns>
        public bool RequestVoice(
            InteractionType type,
            int priority,
            string cooldownKey,
            float cooldownDuration,
            AudioSourcePool pool,
            out PooledAudioSource stolen)
        {
            stolen = null;

            // Cooldown check
            if (!string.IsNullOrEmpty(cooldownKey) && cooldownDuration > 0f)
            {
                if (_cooldownTimers.TryGetValue(cooldownKey, out float lastTime))
                {
                    if (Time.time - lastTime < cooldownDuration)
                        return false;
                }
            }

            PruneFinishedVoices();

            // Per-type concurrency check
            int typeCount = CountActiveByType(type);
            int typeMax   = GetMaxConcurrency(type);
            if (typeCount >= typeMax)
            {
                // Try stealing lowest-priority voice of same type
                stolen = TryStealByType(type, priority, pool);
                if (stolen == null) return false;
            }

            // Global limit check
            if (_activeVoices.Count >= _globalMaxVoices)
            {
                stolen = TryStealGlobal(priority, pool);
                if (stolen == null) return false;
            }

            return true;
        }

        /// <summary>
        /// Registers a newly started voice with the manager.
        /// </summary>
        public void RegisterVoice(PooledAudioSource source, InteractionType type, int priority, string cooldownKey, float cooldownDuration)
        {
            _activeVoices.Add(new VoiceEntry
            {
                Source      = source,
                Priority    = priority,
                Interaction = type,
                StartTime   = Time.time,
                CooldownKey = cooldownKey
            });

            if (!string.IsNullOrEmpty(cooldownKey) && cooldownDuration > 0f)
                _cooldownTimers[cooldownKey] = Time.time;
        }

        /// <summary>
        /// Explicitly unregisters a voice (e.g. when manually stopped).
        /// </summary>
        public void UnregisterVoice(PooledAudioSource source)
        {
            _activeVoices.RemoveAll(v => v.Source == source);
        }

        public int ActiveVoiceCount => _activeVoices.Count;

        /// <summary>Returns a read-only snapshot of active voices (for debugger).</summary>
        public IReadOnlyList<VoiceEntry> ActiveVoices => _activeVoices;

        /// <summary>Override max concurrency for a specific type at runtime.</summary>
        public void SetMaxConcurrency(InteractionType type, int max)
        {
            _maxConcurrency[type] = Mathf.Max(0, max);
        }

        // ── Internal ──────────────────────────────────────────────────────────

        private void PruneFinishedVoices()
        {
            _activeVoices.RemoveAll(v => v.Source == null || !v.Source.IsInUse);
        }

        private int CountActiveByType(InteractionType type)
        {
            int count = 0;
            foreach (var v in _activeVoices)
                if (v.Interaction == type) count++;
            return count;
        }

        private int GetMaxConcurrency(InteractionType type)
        {
            return _maxConcurrency.TryGetValue(type, out int max) ? max : 16;
        }

        private PooledAudioSource TryStealByType(InteractionType type, int requesterPriority, AudioSourcePool pool)
        {
            VoiceEntry lowest = null;
            foreach (var v in _activeVoices)
            {
                if (v.Interaction != type || v.Source == null || v.Source.IsLooping) continue;
                if (lowest == null || v.Priority < lowest.Priority ||
                    (v.Priority == lowest.Priority && v.StartTime < lowest.StartTime))
                    lowest = v;
            }

            if (lowest != null && lowest.Priority < requesterPriority)
            {
                pool.ReturnToPool(lowest.Source);
                _activeVoices.Remove(lowest);
                return lowest.Source;
            }

            return null;
        }

        private PooledAudioSource TryStealGlobal(int requesterPriority, AudioSourcePool pool)
        {
            VoiceEntry lowest = null;
            foreach (var v in _activeVoices)
            {
                if (v.Source == null || v.Source.IsLooping) continue;
                if (lowest == null || v.Priority < lowest.Priority ||
                    (v.Priority == lowest.Priority && v.StartTime < lowest.StartTime))
                    lowest = v;
            }

            if (lowest != null && lowest.Priority < requesterPriority)
            {
                pool.ReturnToPool(lowest.Source);
                _activeVoices.Remove(lowest);
                return lowest.Source;
            }

            return null;
        }
    }
}
