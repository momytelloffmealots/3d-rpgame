// AudioTestController.cs
// Responsibility: Test/Demo script for AudioTestScene. Triggers audio interactions via keyboard.
//                 FOR TESTING ONLY — not part of production gameplay.
// Called By: AudioTestScene manually
// Calls: AudioInteractionService

using UnityEngine;

namespace RPGAudio
{
    /// <summary>
    /// Interactive test controller for the Audio Framework.
    ///
    /// Keyboard shortcuts (Play mode only):
    ///   1 = Footstep Grass
    ///   2 = Footstep Stone
    ///   3 = Footstep Water
    ///   4 = Landing Stone (light)
    ///   5 = Landing Stone (heavy)
    ///   6 = Bullet Impact Metal
    ///   7 = Bullet Impact Wood
    ///   8 = Explosion
    ///   9 = Random variation test (4x Grass footstep)
    ///   0 = Bullet Impact Default (tests fallback)
    ///
    /// Add this to any GameObject in AudioTestScene.
    /// </summary>
    public class AudioTestController : MonoBehaviour
    {
        [Header("Test Position")]
        [SerializeField, Tooltip("Audio will play at this position (usually the player's position).")]
        private Transform audioOrigin;

        private void Start()
        {
            if (audioOrigin == null)
                audioOrigin = transform;

            Debug.Log("[AudioTest] Audio Test Controller Ready!\n" +
                      "1=Grass Footstep | 2=Stone Footstep | 3=Water Footstep\n" +
                      "4=Light Landing  | 5=Heavy Landing\n" +
                      "6=Bullet Metal   | 7=Bullet Wood | 8=Explosion\n" +
                      "9=Variation Test | 0=Fallback Test");
        }

        private void Update()
        {
            if (AudioInteractionService.Instance == null) return;
            var svc = AudioInteractionService.Instance;
            var pos = audioOrigin.position;

            if (Input.GetKeyDown(KeyCode.Alpha1))
            {
                Debug.Log("[AudioTest] ► Footstep GRASS");
                svc.Play(AudioInteractionContext.Footstep(SurfaceType.Grass, pos, 2f, 0.7f));
            }

            if (Input.GetKeyDown(KeyCode.Alpha2))
            {
                Debug.Log("[AudioTest] ► Footstep STONE");
                svc.Play(AudioInteractionContext.Footstep(SurfaceType.Stone, pos, 2f, 0.7f));
            }

            if (Input.GetKeyDown(KeyCode.Alpha3))
            {
                Debug.Log("[AudioTest] ► Footstep WATER");
                svc.Play(AudioInteractionContext.Footstep(SurfaceType.Water, pos, 2f, 0.6f));
            }

            if (Input.GetKeyDown(KeyCode.Alpha4))
            {
                Debug.Log("[AudioTest] ► Landing STONE (Light - intensity 0.25)");
                svc.Play(AudioInteractionContext.Landing(SurfaceType.Stone, pos, 0.25f));
            }

            if (Input.GetKeyDown(KeyCode.Alpha5))
            {
                Debug.Log("[AudioTest] ► Landing STONE (Heavy - intensity 1.0)");
                svc.Play(AudioInteractionContext.Landing(SurfaceType.Stone, pos, 1.0f));
            }

            if (Input.GetKeyDown(KeyCode.Alpha6))
            {
                Debug.Log("[AudioTest] ► Bullet Impact METAL");
                svc.Play(AudioInteractionContext.BulletImpact(SurfaceType.Metal, pos, 80f));
            }

            if (Input.GetKeyDown(KeyCode.Alpha7))
            {
                Debug.Log("[AudioTest] ► Bullet Impact WOOD");
                svc.Play(AudioInteractionContext.BulletImpact(SurfaceType.Wood, pos, 80f));
            }

            if (Input.GetKeyDown(KeyCode.Alpha8))
            {
                Debug.Log("[AudioTest] ► EXPLOSION");
                WeaponAudioBridge.PlayExplosion(pos, 10f);
            }

            if (Input.GetKeyDown(KeyCode.Alpha9))
            {
                Debug.Log("[AudioTest] ► Variation Test — 4x Grass Footstep");
                for (int i = 0; i < 4; i++)
                {
                    var ctx = AudioInteractionContext.Footstep(SurfaceType.Grass,
                        pos + Vector3.right * i * 0.5f, 2f, 0.8f);
                    svc.Play(ctx);
                }
            }

            if (Input.GetKeyDown(KeyCode.Alpha0))
            {
                Debug.Log("[AudioTest] ► Fallback Test — Bullet Impact on MAGIC (expects Default fallback)");
                svc.Play(AudioInteractionContext.BulletImpact(SurfaceType.Magic, pos, 50f));
            }
        }
    }
}
