// AudioVariationUtility.cs
// Responsibility: Centralized clip selection and variation logic.
//                 No audio playing here — only selection algorithms.
// Called By: AudioInteractionService
// Calls: AudioInteractionData

using UnityEngine;
using System.Collections.Generic;

namespace RPGAudio
{
    /// <summary>
    /// Handles all audio variation logic: clip selection, pitch/volume randomization.
    ///
    /// Supported strategies:
    ///   - Random:       Picks any clip at random (simple, may repeat).
    ///   - NoRepeat:     Avoids repeating the last played clip (per-key tracking).
    ///   - ShuffleBag:   Cycles through all clips before repeating (most organic).
    ///
    /// To add a new strategy: add enum value + case in SelectClip().
    /// </summary>
    public class AudioVariationUtility
    {
        /// <summary>Available clip selection strategies.</summary>
        public enum SelectionStrategy
        {
            Random,
            NoRepeat,
            ShuffleBag
        }

        // ── Per-key state for no-repeat and shuffle ─────────────────────────
        private readonly Dictionary<string, int>        _lastIndex  = new Dictionary<string, int>();
        private readonly Dictionary<string, List<int>>  _shuffleBag = new Dictionary<string, List<int>>();

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Selects a clip from the given data using the specified strategy.
        /// </summary>
        /// <param name="data">Interaction data containing clips array.</param>
        /// <param name="strategy">How to pick the next clip.</param>
        /// <param name="key">Unique key per interaction source (e.g. "Player_Footstep_Grass") for state tracking.</param>
        /// <returns>Selected clip, or null if no clips are available.</returns>
        public AudioClip SelectClip(
            AudioInteractionData data,
            SelectionStrategy strategy = SelectionStrategy.NoRepeat,
            string key = "default")
        {
            if (data == null || !data.HasClips) return null;
            if (data.clips.Length == 1)         return data.clips[0];

            switch (strategy)
            {
                case SelectionStrategy.Random:
                    return data.clips[Random.Range(0, data.clips.Length)];

                case SelectionStrategy.NoRepeat:
                    return SelectNoRepeat(data.clips, key);

                case SelectionStrategy.ShuffleBag:
                    return SelectShuffle(data.clips, key);

                default:
                    return data.clips[Random.Range(0, data.clips.Length)];
            }
        }

        /// <summary>
        /// Returns a randomized pitch value within the data's range.
        /// </summary>
        public float GetPitch(AudioInteractionData data)
        {
            return Random.Range(data.pitchMin, data.pitchMax);
        }

        /// <summary>
        /// Returns final volume applying base, variation, and intensity curve.
        /// </summary>
        public float GetVolume(AudioInteractionData data, float intensity)
        {
            float variation  = Random.Range(-data.volumeVariation, data.volumeVariation);
            float baseResult = data.GetFinalVolume(intensity);
            return Mathf.Clamp01(baseResult + variation);
        }

        // ── Internal strategies ────────────────────────────────────────────────

        private AudioClip SelectNoRepeat(AudioClip[] clips, string key)
        {
            int lastIdx = _lastIndex.TryGetValue(key, out int prev) ? prev : -1;
            int newIdx;

            if (clips.Length == 2)
            {
                // Only two options — just swap
                newIdx = lastIdx == 0 ? 1 : 0;
            }
            else
            {
                do { newIdx = Random.Range(0, clips.Length); }
                while (newIdx == lastIdx);
            }

            _lastIndex[key] = newIdx;
            return clips[newIdx];
        }

        private AudioClip SelectShuffle(AudioClip[] clips, string key)
        {
            if (!_shuffleBag.TryGetValue(key, out List<int> bag) || bag.Count == 0)
            {
                bag = new List<int>();
                for (int i = 0; i < clips.Length; i++) bag.Add(i);
                ShuffleList(bag);
                _shuffleBag[key] = bag;
            }

            int idx = bag[bag.Count - 1];
            bag.RemoveAt(bag.Count - 1);
            return clips[idx];
        }

        private void ShuffleList(List<int> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }

        /// <summary>
        /// Resets all stored state (useful for unit tests or scene reloads).
        /// </summary>
        public void ResetState()
        {
            _lastIndex.Clear();
            _shuffleBag.Clear();
        }
    }
}
