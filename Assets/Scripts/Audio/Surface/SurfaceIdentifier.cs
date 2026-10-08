// SurfaceIdentifier.cs
// Responsibility: Component placed on GameObjects to declare their surface type.
//                 Highest priority in SurfaceDetector's detection chain.
// Called By: SurfaceDetector
// Calls: Nothing

using UnityEngine;

namespace RPGAudio
{
    /// <summary>
    /// Attach this component to any GameObject to explicitly declare its surface type.
    /// SurfaceDetector checks for this component FIRST before any other detection method.
    ///
    /// Example: Place on a Grass plane → set SurfaceType = Grass
    ///          SurfaceDetector will immediately return Grass without checking layers/materials.
    /// </summary>
    public class SurfaceIdentifier : MonoBehaviour
    {
        [Tooltip("The surface type this GameObject represents.")]
        [SerializeField] private SurfaceType surfaceType = SurfaceType.Default;

        /// <summary>The declared surface type for this object.</summary>
        public SurfaceType Surface => surfaceType;
    }
}
