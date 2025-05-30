﻿using System.Collections.Generic;
using System.Linq;

using UnityEngine;

using RTSEngine.ResourceExtension;
using RTSEngine.Entities;
using RTSEngine.Game;
using RTSEngine.Logging;
using RTSEngine.UI;
using UnityEngine.Serialization;
using RTSEngine.Event;
using RTSEngine.Task;
using RTSEngine.Faction;
using UnityEngine.Events;

namespace RTSEngine.EntityComponent
{
    [System.Serializable]
    public class EntityComponentTaskInputBase : IEntityComponentTaskInput
    {
        [SerializeField, Tooltip("Title of the task to be displayed in UI elements.")]
        private string taskTitle = "Task Title";
        public string Title => taskTitle;

        public bool IsInitialized { private set; get; } = false;
        public bool IsEnabled { private set; get; } = false;

        public virtual GameObject Object { get; }

        [SerializeField, Header("General Task Properties"), Tooltip("Defines the data used to display the task."), FormerlySerializedAs("asset")]
        private EntityComponentTaskUIAsset taskUI = null;
        public EntityComponentTaskUIData Data => taskUI.Data;

        [SerializeField, Tooltip("Pick the faction types allowed to execute this task. When the faction type does not match, then the task will be hidden.")]
        private FactionTypeTargetPicker allowedFactionTypes = new FactionTypeTargetPicker();
        public bool IsFactionTypeAllowed(FactionTypeInfo factionType) => allowedFactionTypes.IsValidTarget(factionType);

        [Space(), SerializeField, Tooltip("Resources required to launch the task.")]
        protected ResourceInput[] requiredResources = new ResourceInput[0];
        public IReadOnlyList<ResourceInput> RequiredResources => requiredResources.ToList();

        [Space(), SerializeField, Tooltip("Input the faction units/buildings required to create this faction entity.")]
        protected FactionEntityRequirement[] factionEntityRequirements = new FactionEntityRequirement[0];
        public IReadOnlyList<FactionEntityRequirement> FactionEntityRequirements => factionEntityRequirements.ToList();

        [Space(), SerializeField, Tooltip("How would the task icon look in the task panel in case requirements are not met?")]
        private EntityComponentLockedTaskUIData missingRequirementData = new EntityComponentLockedTaskUIData { color = new Color(255, 76, 76, 1.0f), icon = null };
        public EntityComponentLockedTaskUIData MissingRequirementData => missingRequirementData;

        [Space(), SerializeField, Tooltip("Event triggered when the task is complete.")]
        private UnityEvent onComplete = new UnityEvent();

        /// <summary>
        /// Amounts of times the task has been launched.
        /// </summary>
        public int LaunchTimes { private set; get; }
        /// <summary>
        /// Current pending amount of active instances of the task.
        /// </summary>
        public int PendingAmount { private set; get; }

        protected IGameManager gameMgr { private set; get; }
        protected IGameLoggingService logger { private set; get; }
        protected IResourceManager resourceMgr { private set; get; }

        public IEntityComponent SourceComponent { private set; get; }
        public IEntity Entity => SourceComponent.Entity;
        public int ID { private set; get; }

        public void Init(IEntityComponent entityComponent, int taskID, IGameManager gameMgr)
        {
            this.gameMgr = gameMgr;
            this.logger = this.gameMgr.GetService<IGameLoggingService>();

            if (!this.logger.RequireTrue(!IsInitialized,
                $"[{GetType().Name}] Input task has been already initialized. Unable to initialize again."))
                return;

            this.resourceMgr = this.gameMgr.GetService<IResourceManager>();

            this.SourceComponent = entityComponent;
            this.ID = taskID;

            if (!this.logger.RequireValid(this.Entity,
                $"[{GetType().Name}] Input task must be initialized with a valid instance of '{typeof(IEntity).Name}' as the source.")
                || !this.logger.RequireValid(taskUI,
                $"[{GetType().Name} - Entity: {this.Entity.Code} - Faction ID: {this.Entity.FactionID}] Input tasks must have the 'Asset' field assigned!"))
                return;

            IsInitialized = true;

            LaunchTimes = 0;

            OnInit();

            if (gameMgr.GetService<ITaskManager>().TryGetEntityComponentTaskInputInitialData(SourceComponent, ID, out EntityComponentTaskInputData initialData))
            {
                LaunchTimes = initialData.launchTimes;
            }

            gameMgr.GetService<IGlobalEventPublisher>().RaiseEntityComponentTaskInputInitializedGlobal(this);
        }

        protected virtual void OnInit() { }

        public void Enable()
        {
            IsEnabled = true;

            OnEnabled();
        }

        protected virtual void OnEnabled() { }

        public void Disable()
        {
            IsEnabled = false;

            OnDisabled();
        }

        protected virtual void OnDisabled() { }

        //are all conditions for launching the task satisfied?
        public virtual ErrorMessage CanComplete()
        {
            if (!IsInitialized)
                return ErrorMessage.inactive;
            else if (!IsEnabled)
                return ErrorMessage.disabled;
            else if (!RTSHelper.TestFactionEntityRequirements(factionEntityRequirements, gameMgr.GetFactionSlot(Entity.FactionID).FactionMgr))
                return ErrorMessage.taskMissingFactionEntityRequirements;
            else if (!resourceMgr.HasResources(requiredResources, Entity.FactionID))
                return ErrorMessage.taskMissingResourceRequirements;

            return ErrorMessage.none;
        }

        //what happens regarding the task requirements when an instance of this task is launched?
        public virtual void OnComplete()
        {
            if (!logger.RequireTrue(IsInitialized,
                $"[{GetType().Name}] Component must be initialized before it can be used!"))
                return;

            resourceMgr.UpdateResource(Entity.FactionID, requiredResources, add: false);
            PendingAmount--;

            onComplete.Invoke();
        }

        public virtual ErrorMessage CanStart() => CanComplete();

        public virtual void OnStart()
        {
            LaunchTimes++;

            PendingAmount++;
        }

        public virtual void OnCancel() 
        {
            LaunchTimes--;

            PendingAmount--;
        }
    }

    public class EntityComponentTargetTaskInputBase<T> : EntityComponentTaskInputBase
    { 
        public T TargetObject => Object.GetComponent<T>();

        protected sealed override void OnInit()
        {
            if (!logger.RequireValid(this.TargetObject,
                $"[{GetType().Name}] The 'Prefab Object' field must be assigned to a prefab that has a component of type '{typeof(T).Name}' attached to it."))
                return;
        }
    }

    public class FactionEntityCreationTask<T> : EntityComponentTargetTaskInputBase<T> where T : IFactionEntity
    {
        public override ErrorMessage CanComplete()
        {
            ErrorMessage errorMessage;
            if ((errorMessage = base.CanComplete()) != ErrorMessage.none)
                return errorMessage;
            else if(gameMgr.GetFactionSlot(Entity.FactionID).FactionMgr.HasReachedLimit(TargetObject.Code, TargetObject.Category))
                return ErrorMessage.factionLimitReached;

            return ErrorMessage.none;
        }
    }
}
