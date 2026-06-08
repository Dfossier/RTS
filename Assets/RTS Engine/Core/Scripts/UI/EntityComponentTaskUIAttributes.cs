using RTSEngine.Entities;
using RTSEngine.EntityComponent;
using RTSEngine.ResourceExtension;
using System.Collections.Generic;
using UnityEngine;

namespace RTSEngine.UI
{
    public struct EntityComponentTaskUIAttributes : ITaskUIAttributes
    {
        public EntityComponentTaskUIData data;

        public int factionID;

        public string title;
        public IReadOnlyList<ResourceInput> requiredResources;
        public IReadOnlyList<FactionEntityRequirement> factionEntityRequirements;

        public bool launchOnce;

        public IEntityComponentGroupDisplayer sourceTracker;

        public bool locked;
        public EntityComponentLockedTaskUIData lockedData;

        public string tooltipText;

        // When set, the task button's background image is tinted with backgroundColor (instead of leaving it white).
        // Used for at-a-glance state indicators (e.g. ResourceGeneratorToggleUI ON/OFF). See EntityComponentTaskUI.OnReload.
        public bool overrideBackgroundColor;
        public Color backgroundColor;
    }
}
