// AudioMixerController.cs
// Responsibility: Wraps Unity AudioMixer. Exposes simple volume control for each mixer group.
//                 Converts linear [0..1] volume to decibels for AudioMixer.
// Called By: UI Volume Sliders, AudioInteractionService
// Calls: UnityEngine.Audio.AudioMixer

using UnityEngine;
using UnityEngine.Audio;

namespace RPGAudio
{
    /// <summary>
    /// Controls volume levels on the Audio Mixer via linear [0..1] values.
    ///
    /// Required Mixer hierarchy:
    ///   Master
    ///   ├── Music
    ///   ├── SFX
    ///   │   ├── Player
    ///   │   ├── Enemy
    ///   │   ├── World
    ///   │   ├── Combat
    ///   │   └── Interaction
    ///   ├── Ambience
    ///   ├── Voice
    ///   └── UI
    ///
    /// Exposed parameter names must match the mixer's Exposed Parameters list.
    /// Convention: "{GroupName}Volume" e.g. "MasterVolume", "MusicVolume", "SFXVolume"
    ///
    /// Add to the same GameObject as AudioInteractionService.
    /// </summary>
    public class AudioMixerController : MonoBehaviour
    {
        [Header("Mixer Reference")]
        [SerializeField, Tooltip("The main AudioMixer asset.")]
        private AudioMixer mainMixer;

        // ── Exposed parameter names (must match mixer's Exposed Parameters) ──
        private const string MasterParam    = "MasterVolume";
        private const string MusicParam     = "MusicVolume";
        private const string SFXParam       = "SFXVolume";
        private const string AmbienceParam  = "AmbienceVolume";
        private const string VoiceParam     = "VoiceVolume";
        private const string UIParam        = "UIVolume";

        // Sub-group SFX (optional)
        private const string SFXPlayerParam = "SFXPlayerVolume";
        private const string SFXCombatParam = "SFXCombatVolume";
        private const string SFXWorldParam  = "SFXWorldVolume";

        // ── Public volume setters (linear 0..1) ───────────────────────────────

        public void SetMasterVolume(float linear)   => SetVolume(MasterParam,   linear);
        public void SetMusicVolume(float linear)    => SetVolume(MusicParam,    linear);
        public void SetSFXVolume(float linear)      => SetVolume(SFXParam,      linear);
        public void SetAmbienceVolume(float linear) => SetVolume(AmbienceParam, linear);
        public void SetVoiceVolume(float linear)    => SetVolume(VoiceParam,    linear);
        public void SetUIVolume(float linear)       => SetVolume(UIParam,       linear);

        /// <summary>Gets a mixer group by name. Returns null if not found.</summary>
        public AudioMixerGroup GetGroup(string groupName)
        {
            if (mainMixer == null) return null;
            var groups = mainMixer.FindMatchingGroups(groupName);
            return groups.Length > 0 ? groups[0] : null;
        }

        /// <summary>
        /// Saves all current mixer parameter values to PlayerPrefs.
        /// Call this from a settings save function.
        /// </summary>
        public void SaveVolumes()
        {
            SaveParam(MasterParam);
            SaveParam(MusicParam);
            SaveParam(SFXParam);
            SaveParam(AmbienceParam);
            SaveParam(VoiceParam);
            SaveParam(UIParam);
        }

        /// <summary>Loads saved volume values from PlayerPrefs.</summary>
        public void LoadVolumes()
        {
            LoadParam(MasterParam,   1f);
            LoadParam(MusicParam,    1f);
            LoadParam(SFXParam,      1f);
            LoadParam(AmbienceParam, 0.8f);
            LoadParam(VoiceParam,    1f);
            LoadParam(UIParam,       1f);
        }

        // ── Internal ──────────────────────────────────────────────────────────

        private void SetVolume(string paramName, float linear)
        {
            if (mainMixer == null) return;

            // Convert linear [0..1] → dB. Silence at very low values to avoid -∞.
            float db = linear > 0.0001f
                ? Mathf.Log10(linear) * 20f
                : -80f;

            mainMixer.SetFloat(paramName, db);
        }

        private void SaveParam(string paramName)
        {
            if (mainMixer == null) return;
            if (mainMixer.GetFloat(paramName, out float db))
            {
                // Convert dB back to linear for storage
                float linear = Mathf.Pow(10f, db / 20f);
                PlayerPrefs.SetFloat("Audio_" + paramName, linear);
            }
        }

        private void LoadParam(string paramName, float defaultLinear)
        {
            float linear = PlayerPrefs.GetFloat("Audio_" + paramName, defaultLinear);
            SetVolume(paramName, linear);
        }
    }
}
