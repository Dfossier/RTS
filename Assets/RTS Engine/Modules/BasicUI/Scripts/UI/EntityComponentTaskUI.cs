using RTSEngine.Controls;
using RTSEngine.EntityComponent;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UI;

namespace RTSEngine.UI
{
    [RequireComponent(typeof(Button))]
    public class EntityComponentTaskUI : BaseTaskUI<EntityComponentTaskUIAttributes>
    {
        protected override Sprite Icon => Attributes.locked && Attributes.lockedData.icon != null 
            ? Attributes.lockedData.icon
            : Attributes.data.icon; 

        protected override Color IconColor => Attributes.locked 
            ? Attributes.lockedData.color 
            : Color.white;

        protected override bool IsTooltipEnabled => Attributes.data.tooltipEnabled;

        protected override string TooltipDescription => String.IsNullOrEmpty(Attributes.tooltipText) ? Attributes.data.description : Attributes.tooltipText;

        [SerializeField, Tooltip("UI Text to display the control key assigned to the task, if there is any.")]
        private TextMeshProUGUI controlLabel = null;

        protected IGameControlsManager controlsMgr { private set; get; }

        #region Initializing/Terminating
        protected override void OnInit()
        {
            this.controlsMgr = gameMgr.GetService<IGameControlsManager>(); 
        }
        #endregion

        #region Disabling Task UI
        protected override void OnDisabled()
        {
            if(controlLabel.IsValid())
                controlLabel.enabled = false;
        }
        #endregion

        #region Handling Attributes Reload
        protected override void OnReload()
        {
            // Tint the button background for at-a-glance state indicators (e.g. ResourceGeneratorToggleUI ON/OFF).
            // The button uses a Color Tint transition, so we must drive its ColorBlock (not button.image.color,
            // which the transition would immediately overwrite). Reset to defaults when not overriding, since
            // task buttons are pooled and reused across tasks.
            if (button.IsValid())
            {
                ColorBlock colors = button.colors;
                if (Attributes.overrideBackgroundColor)
                {
                    Color c = Attributes.backgroundColor;
                    colors.normalColor = c;
                    colors.selectedColor = c;
                    colors.highlightedColor = Color.Lerp(c, Color.white, 0.25f);
                    colors.pressedColor = Color.Lerp(c, Color.black, 0.15f);
                }
                else
                {
                    colors.normalColor = Color.white;
                    colors.selectedColor = new Color(0.96f, 0.96f, 0.96f, 1f);
                    colors.highlightedColor = new Color(0.96f, 0.96f, 0.96f, 1f);
                    colors.pressedColor = new Color(0.78f, 0.78f, 0.78f, 1f);
                }
                button.colors = colors;
            }

            if (controlLabel.IsValid())
            {
                if (Attributes.data.controlType.IsValid())
                {
                    controlLabel.enabled = true;
                    controlLabel.text = $"{controlsMgr.GetCurrentKeyCode(Attributes.data.controlType)}";
                }
                else
                {
                    controlLabel.enabled = false;
                }
            }
        }
        #endregion

        private void Update()
        {
            if (!Attributes.data.controlType.IsValid())
                return;

            if (controlsMgr.GetUp(Attributes.data.controlType))
                OnClick();
        }

        protected override void OnClick()
        {
            if (Attributes.locked)
                return;

            if (Attributes.launchOnce)
                Attributes.sourceTracker.EntityComponents.FirstOrDefault()?.OnTaskUIClick(Attributes);
            else
            {
                // The ToArray() call is used to create a new Enumerable of the source IEntityComponent components because the original collection might be updated when the task is clicked.
                foreach (IEntityComponent component in Attributes.sourceTracker.EntityComponents.ToArray())
                    component.OnTaskUIClick(Attributes);
            }

            if (Attributes.data.hideTooltipOnClick)
                HideTaskTooltip();
        }
    }
}
