// PooledAudioSource.cs
// Responsibility: Wrapper around an AudioSource to track its pool state and auto-return when clip finishes.
// Called By: AudioSourcePool
// Calls: AudioSourcePool (to return itself)

using UnityEngine;

namespace RPGAudio
{
    /// <summary>
    /// A pooled AudioSource wrapper.
    /// Automatically returns itself to the pool when a one-shot clip finishes playing.
    /// Looping sources must be manually returned via pool.Release().
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class PooledAudioSource : MonoBehaviour
    {
        private AudioSource  _source;
        private AudioSourcePool _pool;
        private bool         _inUse;
        private bool         _isLooping;

        internal void Initialize(AudioSourcePool pool)
        {
            _source = GetComponent<AudioSource>();
            _pool   = pool;
        }

        internal void Acquire()
        {
            _inUse = true;
            gameObject.SetActive(true);
        }

        internal void Release()
        {
            _inUse    = false;
            _isLooping = false;
            gameObject.SetActive(false);

            if (_source != null)
            {
                _source.Stop();
                _source.clip   = null;
                _source.loop   = false;
                _source.volume = 1f;
                _source.pitch  = 1f;
            }
        }

        /// <summary>
        /// Configure and play a one-shot clip. Source auto-returns to pool when done.
        /// </summary>
        public void Play(
            AudioClip clip,
            Vector3 worldPosition,
            float volume,
            float pitch,
            float minDistance,
            float maxDistance,
            float spatialBlend,
            UnityEngine.Audio.AudioMixerGroup mixerGroup = null)
        {
            _isLooping = false;
            ConfigureSource(clip, worldPosition, volume, pitch, minDistance, maxDistance, spatialBlend, mixerGroup);
            _source.loop = false;
            _source.Play();
        }

        /// <summary>
        /// Configure and play a looping clip. Must be manually released via pool.Release().
        /// </summary>
        public void PlayLooping(
            AudioClip clip,
            Vector3 worldPosition,
            float volume,
            float pitch,
            float minDistance,
            float maxDistance,
            float spatialBlend,
            UnityEngine.Audio.AudioMixerGroup mixerGroup = null)
        {
            _isLooping = true;
            ConfigureSource(clip, worldPosition, volume, pitch, minDistance, maxDistance, spatialBlend, mixerGroup);
            _source.loop = true;
            _source.Play();
        }

        private void ConfigureSource(
            AudioClip clip,
            Vector3 worldPosition,
            float volume,
            float pitch,
            float minDistance,
            float maxDistance,
            float spatialBlend,
            UnityEngine.Audio.AudioMixerGroup mixerGroup)
        {
            transform.position = worldPosition;
            _source.clip             = clip;
            _source.volume           = volume;
            _source.pitch            = pitch;
            _source.minDistance      = minDistance;
            _source.maxDistance      = maxDistance;
            _source.spatialBlend     = spatialBlend;
            _source.rolloffMode      = AudioRolloffMode.Logarithmic;
            _source.outputAudioMixerGroup = mixerGroup;
        }

        public AudioSource Source => _source;
        public bool IsInUse       => _inUse;
        public bool IsLooping     => _isLooping;

        private void Update()
        {
            // Auto-return non-looping sources to pool when playback is done
            if (_inUse && !_isLooping && _source != null && !_source.isPlaying)
            {
                _pool?.ReturnToPool(this);
            }
        }
    }
}
