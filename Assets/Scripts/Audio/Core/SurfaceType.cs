// SurfaceType.cs
// Responsibility: Defines all possible surface types for audio detection.
// Called By: SurfaceDetector, AudioInteractionContext, AudioInteractionDatabase
// Calls: Nothing

namespace RPGAudio
{
    /// <summary>
    /// Represents the type of physical surface for audio interactions.
    /// Default is used as fallback when no specific surface is detected.
    /// Add new surface types here — update AudioInteractionDatabase with corresponding profile.
    /// </summary>
    public enum SurfaceType
    {
        Default = 0,
        Grass,
        Dirt,
        Stone,
        Wood,
        Metal,
        Sand,
        Snow,
        Water,
        Ice,
        Mud,
        Glass,
        Magic
    }
}
