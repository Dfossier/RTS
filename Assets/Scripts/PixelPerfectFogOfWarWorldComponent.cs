/// Component created by Marius van den Oever for RTS Engine 2023.0.1 and Pixel Perfect Fog Of war 1.6.5
/// Component version 1.2 (2023-08-15)
/// Engine 2023.0.1 - https://assetstore.unity.com/packages/tools/game-toolkits/rts-engine-2023-79732
/// Pixel Perfect Fog Of war 1.6.5 https://assetstore.unity.com/packages/vfx/shaders/fullscreen-camera-effects/pixel-perfect-fog-of-war-229484
/// Instructions
///   Add this component to the game Game Object where you added Fog of War World

using UnityEngine;

namespace Assets._GAME.Scripts.Components
{
    public class PixelPerfectFogOfWarWorldComponent : MonoBehaviour
    {
        public static bool ShowAllEntities = false;

        [Header("Debug Controls")]
        [Tooltip("Set to true to completely disable Fog of War rendering (for testing)")]
        public bool DisableFogOfWar = false;

        private void OnEnable()
        {
            Debug.Log("[FoW] PixelPerfectFogOfWarWorldComponent ENABLED - F1=ShowAllEntities, F2=ToggleFoW");
        }

        private void Start()
        {
            if (FOW.FogOfWarWorld.instance == null)
            {
                Debug.LogWarning("[FoW] FogOfWarWorld.instance is NULL! Make sure FogOfWarWorld component exists in scene.");
            }
            else
            {
                Debug.Log($"[FoW] FogOfWarWorld found. Current state: enabled={FOW.FogOfWarWorld.instance.enabled}");
            }
        }

        private void Update()
        {
            // F1 - Toggle showing all entities (reveals units but keeps FoW overlay)
            if (Input.GetKeyDown(KeyCode.F1))
            {
                ShowAllEntities = !ShowAllEntities;
                Debug.Log($"[FoW] F1 PRESSED - ShowAllEntities: {ShowAllEntities}");
            }

            // F2 - Toggle Fog of War completely (disables entire system)
            if (Input.GetKeyDown(KeyCode.F2))
            {
                DisableFogOfWar = !DisableFogOfWar;
                if (FOW.FogOfWarWorld.instance != null)
                {
                    FOW.FogOfWarWorld.instance.enabled = !DisableFogOfWar;
                    Debug.Log($"[FoW] F2 PRESSED - Fog of War {(DisableFogOfWar ? "DISABLED" : "ENABLED")}");
                }
                else
                {
                    Debug.LogError("[FoW] F2 PRESSED but FogOfWarWorld.instance is NULL!");
                }
            }

            // Apply DisableFogOfWar setting every frame (in case it's changed in Inspector)
            if (FOW.FogOfWarWorld.instance != null)
            {
                bool shouldBeEnabled = !DisableFogOfWar;
                if (FOW.FogOfWarWorld.instance.enabled != shouldBeEnabled)
                {
                    FOW.FogOfWarWorld.instance.enabled = shouldBeEnabled;
                }
            }
        }
    }
}
