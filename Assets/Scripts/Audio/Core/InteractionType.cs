// InteractionType.cs
// Responsibility: Defines all possible audio interaction types in the game.
// Called By: AudioInteractionContext, AudioInteractionService, AudioInteractionProfile
// Calls: Nothing

namespace RPGAudio
{
    /// <summary>
    /// Represents the type of audio interaction event.
    /// Add new types here to extend the framework — no other core files need changing.
    /// </summary>
    public enum InteractionType
    {
        // Movement
        Footstep,
        Landing,
        Jump,
        Slide,
        Roll,

        // Melee Combat
        WeaponSwing,
        WeaponImpact,

        // Ranged Combat
        BulletImpact,
        ProjectilePass,
        Explosion,

        // Object State
        Destroy,
        Interact,
        Pickup,
        Drop,
        Push,
        Pull,
        Open,
        Close,

        // Environment
        Splash,

        // Magic
        MagicCast,
        MagicImpact,

        // Fallback
        Default
    }
}
