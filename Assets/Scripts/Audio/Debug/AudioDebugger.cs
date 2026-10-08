// AudioDebugger.cs
// Responsibility: Provides logging and runtime diagnostics for the audio system.
//                 Completely removable without affecting any other system.
// Called By: AudioInteractionService
// Calls: Nothing (pure logging / display)

using UnityEngine;
using System.Collections.Generic;

namespace RPGAudio
{
    /// <summary>
    /// Audio system debugger.
    ///
    /// Features:
    ///   - Enable/disable all audio logs with a single flag
    ///   - Per-play log with full context information
    ///   - Runtime on-screen overlay (optional)
    ///   - Static warning/verbose methods usable from any class
    ///
    /// Setup: Add to the AudioManager GameObject alongside AudioInteractionService.
    /// </summary>
    public class AudioDebugger : MonoBehaviour
    {
        // ── Static log control ─────────────────────────────────────────────────
        private static bool _debugEnabled  = true;
        private static bool _verboseEnabled = false;

        [Header("Debug Settings")]
        [SerializeField, Tooltip("Enable audio system debug logs.")]
        private bool enableDebugLogs = true;

        [SerializeField, Tooltip("Enable verbose (per-voice-rejection) logs.")]
        private bool enableVerboseLogs = false;

        [Header("On-Screen Overlay")]
        [SerializeField, Tooltip("Show on-screen debug overlay in play mode.")]
        private bool showOverlay = true;

        [SerializeField, Tooltip("Reference to the AudioInteractionService for voice count display.")]
        private AudioInteractionService service;

        // ── Log history ────────────────────────────────────────────────────────
        [System.Serializable]
        public class AudioLogEntry
        {
            public string Timestamp;
            public string Interaction;
            public string Surface;
            public string Profile;
            public string Clip;
            public float  Intensity;
            public float  Volume;
            public float  Pitch;
        }

        private readonly Queue<AudioLogEntry> _logHistory = new Queue<AudioLogEntry>();
        private const int MaxLogHistory = 20;

        private void Awake()
        {
            _debugEnabled  = enableDebugLogs;
            _verboseEnabled = enableVerboseLogs;
        }

        private void OnValidate()
        {
            _debugEnabled   = enableDebugLogs;
            _verboseEnabled = enableVerboseLogs;
        }

        // ── Internal logging called by AudioInteractionService ─────────────────

        internal void LogPlay(
            AudioInteractionContext ctx,
            AudioInteractionProfile profile,
            AudioInteractionData    data,
            AudioClip               clip,
            float                   volume,
            float                   pitch)
        {
            if (!_debugEnabled) return;

            var entry = new AudioLogEntry
            {
                Timestamp   = System.DateTime.Now.ToString("HH:mm:ss.fff"),
                Interaction = ctx.Interaction.ToString(),
                Surface     = ctx.Surface.ToString(),
                Profile     = profile?.ProfileName ?? "NULL",
                Clip        = clip?.name ?? "NULL",
                Intensity   = ctx.Intensity,
                Volume      = volume,
                Pitch       = pitch
            };

            // Enqueue and trim
            _logHistory.Enqueue(entry);
            while (_logHistory.Count > MaxLogHistory)
                _logHistory.Dequeue();

            Debug.Log(
                $"[Audio] {entry.Interaction} | Surface: {entry.Surface} | " +
                $"Profile: {entry.Profile} | Clip: {entry.Clip} | " +
                $"Intensity: {entry.Intensity:F2} | Vol: {entry.Volume:F2} | Pitch: {entry.Pitch:F2}");
        }

        // ── Static helpers usable from any class ───────────────────────────────

        public static void LogWarning(string message)
        {
            if (_debugEnabled)
                Debug.LogWarning($"[Audio] WARNING: {message}");
        }

        public static void LogVerbose(string message)
        {
            if (_verboseEnabled)
                Debug.Log($"[Audio] VERBOSE: {message}");
        }

        // ── On-screen overlay ──────────────────────────────────────────────────

        private void OnGUI()
        {
            if (!showOverlay || !Application.isPlaying) return;

            int voices = service?.VoiceManager?.ActiveVoiceCount ?? 0;

            GUILayout.BeginArea(new Rect(10, 10, 380, 500));
            GUILayout.Box($"── Audio Debug ─────────────────\nActive Voices: {voices}");

            GUILayout.Label("── Recent Plays ──");
            foreach (var entry in _logHistory)
            {
                GUILayout.Label(
                    $"[{entry.Timestamp}] {entry.Interaction}/{entry.Surface}\n" +
                    $"  Clip: {entry.Clip} | Vol:{entry.Volume:F2} Pitch:{entry.Pitch:F2}",
                    new GUIStyle(GUI.skin.label) { fontSize = 10 });
            }

            GUILayout.EndArea();
        }
    }
}
