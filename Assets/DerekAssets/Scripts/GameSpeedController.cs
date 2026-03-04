using System;
using UnityEngine;

using RTSEngine;
using RTSEngine.Game;
using RTSEngine.Logging;
using RTSEngine.Determinism;

/// <summary>
/// Keyboard-driven game speed controller.
///
/// Integrates with the RTS Engine's ITimeModifier service — do NOT use
/// Time.timeScale directly, as the engine applies its own CurrentModifier
/// to all in-game timers (TimeModifiedTimer, TimeModifiedFloat, etc.) and
/// manages pause/freeze states separately.
///
/// Setup:
///   1. Add this component to a child GameObject of the GameManager object
///      in the scene. The GameManager discovers services via GetComponentsInChildren,
///      so it must be parented there to be auto-initialized.
///   2. Assign speed presets and keybinds in the Inspector, or leave defaults.
///
/// Default keys:
///   1 → 1x   2 → 2x   3 → 3x
///   - (minus) → slower   = (equals) → faster (cycles through presets)
/// </summary>
public class GameSpeedController : MonoBehaviour, IPostRunGameService
{
    #region Attributes
    [System.Serializable]
    public class SpeedPreset
    {
        [Tooltip("Display label shown in the log / debug UI")]
        public string label = "1x";
        [Tooltip("Speed multiplier passed to ITimeModifier (must be > 0)")]
        public float modifier = 1.0f;
        [Tooltip("Key that directly selects this preset. Set to None to disable.")]
        public KeyCode hotkey = KeyCode.None;
    }

    [Header("Speed Presets")]
    [SerializeField, Tooltip("Define available speeds and their hotkeys.")]
    private SpeedPreset[] presets = new SpeedPreset[]
    {
        new SpeedPreset { label = "0.5x", modifier = 0.5f, hotkey = KeyCode.Alpha1 },
        new SpeedPreset { label = "1x",   modifier = 1.0f, hotkey = KeyCode.Alpha2 },
        new SpeedPreset { label = "2x",   modifier = 2.0f, hotkey = KeyCode.Alpha3 },
        new SpeedPreset { label = "3x",   modifier = 3.0f, hotkey = KeyCode.Alpha4 },
    };

    [Header("Starting Speed")]
    [SerializeField, Tooltip("Preset index to apply when the game starts. 0 = first preset in the list above.")]
    private int startPresetIndex = 0;

    [Header("Cycle Keys")]
    [SerializeField, Tooltip("Cycle to the next slower preset.")]
    private KeyCode decreaseKey = KeyCode.Minus;
    [SerializeField, Tooltip("Cycle to the next faster preset.")]
    private KeyCode increaseKey = KeyCode.Equals;

    [Header("Debug")]
    [SerializeField, Tooltip("Log speed changes to the Console.")]
    private bool logChanges = true;

    // Tracks which preset is currently active so cycle keys work correctly.
    private int currentPresetIndex = 0;

    // Services
    private ITimeModifier timeModifier;
    private IGameLoggingService logger;
    #endregion

    #region IPostRunGameService — Initialization
    // Called automatically by GameManager after all services are ready.
    public void Init(IGameManager gameMgr)
    {
        this.logger = gameMgr.GetService<IGameLoggingService>();
        this.timeModifier = gameMgr.GetService<ITimeModifier>();

        if (timeModifier == null)
        {
            logger.LogError("[GameSpeedController] ITimeModifier service not found. " +
                "Make sure a TimeModifier component exists on the GameManager.", source: this);
            return;
        }

        if (presets == null || presets.Length == 0)
        {
            logger.LogError("[GameSpeedController] No speed presets defined.", source: this);
            return;
        }

        // Validate all preset modifiers
        for (int i = 0; i < presets.Length; i++)
        {
            if (presets[i].modifier <= 0f)
            {
                logger.LogError($"[GameSpeedController] Preset '{presets[i].label}' has a modifier <= 0. " +
                    "All modifiers must be > 0.", source: this);
                return;
            }
        }

        // Apply the configured starting preset, overriding whatever the TimeModifier defaulted to.
        // This is the most reliable way to guarantee the game starts at the desired speed
        // regardless of the TimeModifier's defaultModifier field or any game builder data.
        if (startPresetIndex.IsValidIndex(presets))
        {
            currentPresetIndex = startPresetIndex - 1; // force ApplyPreset to not skip as "already active"
            ApplyPreset(startPresetIndex);
        }
        else
        {
            currentPresetIndex = FindClosestPresetIndex(TimeModifier.CurrentModifier);
            logger.LogError($"[GameSpeedController] startPresetIndex {startPresetIndex} is out of range. " +
                "Syncing to current modifier instead.", source: this);
        }

        if (logChanges)
            Debug.Log($"[GameSpeedController] Initialized. Current speed: {presets[currentPresetIndex].label}");
    }
    #endregion

    #region Input Polling
    private void Update()
    {
        if (timeModifier == null)
            return;

        // Direct hotkeys
        for (int i = 0; i < presets.Length; i++)
        {
            if (presets[i].hotkey != KeyCode.None && Input.GetKeyDown(presets[i].hotkey))
            {
                ApplyPreset(i);
                return;
            }
        }

        // Cycle keys
        if (Input.GetKeyDown(increaseKey))
        {
            ApplyPreset(Mathf.Min(currentPresetIndex + 1, presets.Length - 1));
        }
        else if (Input.GetKeyDown(decreaseKey))
        {
            ApplyPreset(Mathf.Max(currentPresetIndex - 1, 0));
        }
    }
    #endregion

    #region Helpers
    private void ApplyPreset(int index)
    {
        if (index == currentPresetIndex)
            return;

        if (timeModifier.SetModifier(presets[index].modifier, playerCommand: false) == ErrorMessage.none)
        {
            currentPresetIndex = index;
            if (logChanges)
                Debug.Log($"[GameSpeedController] Speed set to {presets[index].label} ({presets[index].modifier}x)");
        }
        else
        {
            // The engine blocks modifier changes while frozen/paused — this is expected.
            if (logChanges)
                Debug.Log($"[GameSpeedController] Could not apply speed {presets[index].label} " +
                    "(game may be paused or frozen).");
        }
    }

    private int FindClosestPresetIndex(float targetModifier)
    {
        int best = 0;
        float bestDiff = Mathf.Abs(presets[0].modifier - targetModifier);
        for (int i = 1; i < presets.Length; i++)
        {
            float diff = Mathf.Abs(presets[i].modifier - targetModifier);
            if (diff < bestDiff)
            {
                bestDiff = diff;
                best = i;
            }
        }
        return best;
    }
    #endregion
}
