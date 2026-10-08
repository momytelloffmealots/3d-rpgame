// AudioSourcePool.cs
// Responsibility: Manages a pool of AudioSources to avoid Instantiate/Destroy per sound.
//                 Handles acquiring, releasing, and growing the pool as needed.
// Called By: AudioInteractionService
// Calls: PooledAudioSource

using UnityEngine;
using System.Collections.Generic;

namespace RPGAudio
{
    /// <summary>
    /// Object pool for AudioSource instances.
    ///
    /// Setup:
    ///   1. Add to a dedicated "AudioPool" GameObject.
    ///   2. Set initialPoolSize and maxPoolSize in Inspector.
    ///   3. AudioInteractionService holds a reference to this.
    ///
    /// Behavior:
    ///   - Pre-warms poolSize sources on Awake.
    ///   - Grows up to maxPoolSize when needed.
    ///   - Beyond maxPoolSize: steals the oldest non-looping source.
    ///   - Looping sources must be released manually.
    /// </summary>
    public class AudioSourcePool : MonoBehaviour
    {
        [SerializeField, Tooltip("Number of AudioSources to pre-create on startup.")]
        private int initialPoolSize = 16;

        [SerializeField, Tooltip("Maximum pool size before stealing occurs.")]
        private int maxPoolSize = 32;

        private readonly Queue<PooledAudioSource>  _available  = new Queue<PooledAudioSource>();
        private readonly List<PooledAudioSource>   _allSources = new List<PooledAudioSource>();

        private void Awake()
        {
            PreWarm(initialPoolSize);
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Acquires a source from the pool. May grow or steal if pool is exhausted.
        /// Returns null only if maxPoolSize is 0 or something very unexpected happens.
        /// </summary>
        public PooledAudioSource Acquire()
        {
            PooledAudioSource source;

            if (_available.Count > 0)
            {
                source = _available.Dequeue();
            }
            else if (_allSources.Count < maxPoolSize)
            {
                source = CreateSource();
            }
            else
            {
                // Pool exhausted — steal oldest non-looping source
                source = StealOldestNonLooping();
                if (source == null)
                {
                    Debug.LogWarning("[AudioSourcePool] All sources are busy (looping?). Returning null.");
                    return null;
                }
            }

            source.Acquire();
            return source;
        }

        /// <summary>
        /// Returns a source back to the pool. Called automatically by PooledAudioSource for one-shots.
        /// Must be called manually for looping sources.
        /// </summary>
        public void ReturnToPool(PooledAudioSource source)
        {
            if (source == null) return;
            source.Release();
            _available.Enqueue(source);
        }

        public int TotalSources    => _allSources.Count;
        public int AvailableSources => _available.Count;
        public int ActiveSources   => TotalSources - AvailableSources;

        // ── Internal ──────────────────────────────────────────────────────────

        private void PreWarm(int count)
        {
            for (int i = 0; i < count; i++)
            {
                var source = CreateSource();
                _available.Enqueue(source);
            }
        }

        private PooledAudioSource CreateSource()
        {
            var go = new GameObject($"PooledAudio_{_allSources.Count:000}");
            go.transform.SetParent(transform);
            go.SetActive(false);

            var audioSource = go.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;

            var pooled = go.AddComponent<PooledAudioSource>();
            pooled.Initialize(this);

            _allSources.Add(pooled);
            return pooled;
        }

        private PooledAudioSource StealOldestNonLooping()
        {
            foreach (var src in _allSources)
            {
                if (src.IsInUse && !src.IsLooping)
                {
                    src.Release(); // Force stop it
                    return src;
                }
            }
            return null;
        }
    }
}
