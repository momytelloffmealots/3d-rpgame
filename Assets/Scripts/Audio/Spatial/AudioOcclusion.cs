// AudioOcclusion.cs
// Responsibility: Calculates occlusion between a sound position and the audio listener.
//                 Applies low-pass filter and volume reduction when blocked. NOT tightly coupled to playback.
// Called By: AudioInteractionService (optional, per-source)
// Calls: Physics.Raycast, AudioLowPassFilter

using UnityEngine;

namespace RPGAudio
{
    /// <summary>
    /// Occlusion handler attached to a PooledAudioSource GameObject.
    ///
    /// Uses a single raycast from the sound position to the listener each frame.
    /// If blocked:
    ///   - Volume is multiplied by occlusionVolumeAttenuation
    ///   - AudioLowPassFilter cutoff is lowered to occlusionLowPassFrequency
    ///
    /// If not blocked:
    ///   - Volume and filter are restored to their original values.
    ///
    /// Limitations (basic implementation):
    ///   - Single ray only (no diffraction)
    ///   - No material-based transmission
    ///   - Future: multi-ray, portal occlusion, material transmission coefficients
    ///
    /// HOW TO USE:
    ///   AudioInteractionService optionally adds this component to a PooledAudioSource
    ///   when a context requires occlusion checking.
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class AudioOcclusion : MonoBehaviour
    {
        [Header("Occlusion Settings")]
        [Tooltip("Layers that block audio. Usually 'Default' and obstacle layers.")]
        [SerializeField] private LayerMask occlusionMask = ~0; // Everything by default

        [Tooltip("Volume multiplier applied when fully occluded (0 = silent, 1 = no change).")]
        [Range(0f, 1f)]
        [SerializeField] private float occlusionVolumeAttenuation = 0.3f;

        [Tooltip("Low-pass filter cutoff (Hz) when occluded. Typical: 800–2000 Hz.")]
        [SerializeField] private float occlusionLowPassFrequency = 1500f;

        [Tooltip("How fast the occlusion effect blends in/out (lerp speed).")]
        [SerializeField] private float blendSpeed = 5f;

        // ── State ─────────────────────────────────────────────────────────────
        private AudioSource         _source;
        private AudioLowPassFilter  _lowPass;
        private Transform           _listenerTransform;
        private float               _originalVolume;
        private bool                _initialized;

        private float               _targetVolume;
        private float               _targetCutoff;

        private const float OpenCutoff = 22000f;

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Initializes the occlusion component with the source's original volume.
        /// Must be called after configuring the AudioSource.
        /// </summary>
        public void Initialize(float originalVolume)
        {
            _source = GetComponent<AudioSource>();
            _originalVolume = originalVolume;

            // Add low-pass filter if not present
            _lowPass = GetComponent<AudioLowPassFilter>();
            if (_lowPass == null)
                _lowPass = gameObject.AddComponent<AudioLowPassFilter>();

            _lowPass.cutoffFrequency = OpenCutoff;

            _targetVolume = originalVolume;
            _targetCutoff = OpenCutoff;
            _initialized  = true;

            FindListener();
        }

        private void FindListener()
        {
            var listener = FindObjectOfType<AudioListener>();
            if (listener != null)
                _listenerTransform = listener.transform;
        }

        private void Update()
        {
            if (!_initialized || _source == null || !_source.isPlaying) return;
            if (_listenerTransform == null) { FindListener(); return; }

            CheckOcclusion();
            SmoothApply();
        }

        private void CheckOcclusion()
        {
            Vector3 dir      = _listenerTransform.position - transform.position;
            float   distance = dir.magnitude;

            bool blocked = Physics.Raycast(transform.position, dir.normalized, distance, occlusionMask);

            if (blocked)
            {
                _targetVolume = _originalVolume * occlusionVolumeAttenuation;
                _targetCutoff = occlusionLowPassFrequency;
            }
            else
            {
                _targetVolume = _originalVolume;
                _targetCutoff = OpenCutoff;
            }
        }

        private void SmoothApply()
        {
            _source.volume           = Mathf.Lerp(_source.volume,           _targetVolume, Time.deltaTime * blendSpeed);
            _lowPass.cutoffFrequency = Mathf.Lerp(_lowPass.cutoffFrequency, _targetCutoff, Time.deltaTime * blendSpeed);
        }

        public bool IsOccluded => Mathf.Approximately(_targetVolume, _originalVolume * occlusionVolumeAttenuation);

        private void OnDrawGizmos()
        {
            if (!_initialized || _listenerTransform == null) return;
            Gizmos.color = IsOccluded ? Color.red : Color.green;
            Gizmos.DrawLine(transform.position, _listenerTransform.position);
        }
    }
}
