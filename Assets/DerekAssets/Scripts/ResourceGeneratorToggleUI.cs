using RTSEngine;
using RTSEngine.Entities;
using RTSEngine.EntityComponent;
using RTSEngine.UI;
using RTSEngine.Event;
using RTSEngine.Utilities;
using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Provides a toggle button in the building selection UI to start/stop resource generation.
/// Automatically updates the icon based on the generator's active state.
///
/// COMPLETE WORKING VERSION - Integrates with RTS Engine's UI system
///
/// Usage:
/// 1. Attach to building with ResourceGenerator
/// 2. Create TWO task UI assets (one for "start", one for "stop")
/// 3. Assign different icons to each
/// 4. The button will automatically swap when clicked
/// </summary>
public class ResourceGeneratorToggleUI : EntityComponentBase
{
    #region Attributes
    [HideInInspector]
    public Int2D tabID = new Int2D { x = 0, y = 0 };

    public enum ActionType : byte { toggleGeneration = 0 }

    public enum VisualEffectMode
    {
        IconSwap,       // Changes icon sprite
        ColorTint,      // Changes icon color
        LockedState     // Uses locked/unlocked visual (grayed out when off)
    }

    [Header("Generator Reference")]
    [SerializeField, Tooltip("The ResourceGenerator to control (leave empty to auto-find)")]
    private ResourceGenerator targetGenerator;

    [Header("Task UI")]
    [SerializeField, Tooltip("Task UI for the toggle button")]
    private EntityComponentTaskUIAsset toggleTask;

    [SerializeField, Tooltip("Icon to show when generation is ACTIVE")]
    private Sprite activeIcon;

    [SerializeField, Tooltip("Icon to show when generation is STOPPED")]
    private Sprite inactiveIcon;

    [SerializeField, Tooltip("Color tint when generation is ACTIVE")]
    private Color activeColor = Color.green;

    [SerializeField, Tooltip("Color tint when generation is STOPPED")]
    private Color inactiveColor = Color.red;

    [Header("Visual Effect Mode")]
    [SerializeField, Tooltip("How to show on/off state: Icon swap, Color tint, or Locked state")]
    private VisualEffectMode effectMode = VisualEffectMode.ColorTint;

    [Header("Settings")]
    [SerializeField, Tooltip("Start with generation enabled?")]
    private bool startEnabled = false;

    [Header("Active Indicator")]
    [SerializeField, Tooltip("Tint color for icon when generator is active")]
    private Color activeIconTint = new Color(0.5f, 1f, 0.5f, 1f); // Light green tint

    private IBuilding building;
    #endregion

    #region Public Access
    /// <summary>
    /// Get the target ResourceGenerator this toggle controls
    /// </summary>
    public ResourceGenerator GetTargetGenerator() => targetGenerator;
    #endregion

    #region Initialization
    protected override void OnInit()
    {
        building = Entity as IBuilding;

        // Auto-find ResourceGenerator if not assigned
        if (targetGenerator == null)
        {
            targetGenerator = Entity.GetComponentInChildren<ResourceGenerator>();
        }

        if (targetGenerator == null)
        {
            logger.LogError($"No ResourceGenerator found on {Entity.Code}!", source: this);
            return;
        }

        if (toggleTask == null)
        {
            logger.LogError($"Toggle task UI asset not assigned on {Entity.Code}!", source: this);
            return;
        }

        // Validate icon/color setup based on effect mode
        if (effectMode == VisualEffectMode.IconSwap && (activeIcon == null || inactiveIcon == null))
        {
            logger.LogWarning($"IconSwap mode selected but icons not assigned on {Entity.Code}. Falling back to LockedState mode.", source: this);
            effectMode = VisualEffectMode.LockedState;
        }

        // Always start disabled (force OFF regardless of inspector value)
        targetGenerator.SetActiveLocal(false, playerCommand: false);
    }
    #endregion

    #region Task UI
    protected override bool OnTaskUICacheUpdate(
        List<EntityComponentTaskUIAttributes> taskUIAttributes,
        List<string> disabledTaskCodes)
    {
        if (targetGenerator == null || toggleTask == null)
            return false;

        bool isActive = targetGenerator.IsActive;

        // Create base task data with modified description showing state
        EntityComponentTaskUIData taskData = toggleTask.Data;
        bool locked = false;
        EntityComponentLockedTaskUIData lockedData = default;

        // Apply visual effect based on mode
        switch (effectMode)
        {
            case VisualEffectMode.IconSwap:
                // Swap icon AND description based on state
                taskData.icon = isActive ? activeIcon : inactiveIcon;
                taskData.description = isActive ? "Stop Generation (ON)" : "Start Generation (OFF)";
                break;

            case VisualEffectMode.ColorTint:
                // Modify description with color tags
                string baseDesc = toggleTask.Data.description;
                taskData.description = isActive
                    ? $"<color=#{ColorUtility.ToHtmlStringRGB(activeColor)}>{baseDesc} (ON)</color>"
                    : $"<color=#{ColorUtility.ToHtmlStringRGB(inactiveColor)}>{baseDesc} (OFF)</color>";
                break;

            case VisualEffectMode.LockedState:
                // Use locked visual state - locked (grayed) when OFF, normal when ON
                locked = !isActive;
                lockedData = new EntityComponentLockedTaskUIData
                {
                    icon = toggleTask.Data.icon,
                    color = new Color(0.5f, 0.5f, 0.5f, 0.5f)
                };
                taskData.description = isActive ? "Stop Generation (ON)" : "Start Generation (OFF)";
                break;
        }

        taskUIAttributes.Add(new EntityComponentTaskUIAttributes
        {
            data = taskData,
            locked = locked,
            lockedData = lockedData
        });

        return true;
    }

    public override bool OnTaskUIClick(EntityComponentTaskUIAttributes taskUIAttributes)
    {
        // Verify this is our task
        if (toggleTask.IsValid() && taskUIAttributes.data.code == toggleTask.Data.code)
        {
            // Toggle the generator state
            bool newState = !targetGenerator.IsActive;

            // If turning ON, turn off all other generators first (only one can be active at a time)
            if (newState)
            {
                var allToggles = (Entity as MonoBehaviour).GetComponentsInChildren<ResourceGeneratorToggleUI>();
                foreach (var toggle in allToggles)
                {
                    if (toggle != this && toggle.GetTargetGenerator() != null && toggle.GetTargetGenerator().IsActive)
                    {
                        toggle.GetTargetGenerator().SetActive(false, playerCommand: true);
                    }
                }
            }

            ErrorMessage result = targetGenerator.SetActive(newState, playerCommand: true);
            return result == ErrorMessage.none;
        }

        return false;
    }
    #endregion

    #region Actions
    public override ErrorMessage LaunchActionLocal(byte actionID, SetTargetInputData input)
    {
        if (targetGenerator == null)
            return ErrorMessage.invalid;

        switch ((ActionType)actionID)
        {
            case ActionType.toggleGeneration:
                // Toggle the generator state
                bool newState = !targetGenerator.IsActive;
                targetGenerator.SetActiveLocal(newState, playerCommand: true);
                return ErrorMessage.none;

            default:
                return base.LaunchActionLocal(actionID, input);
        }
    }
    #endregion

}
