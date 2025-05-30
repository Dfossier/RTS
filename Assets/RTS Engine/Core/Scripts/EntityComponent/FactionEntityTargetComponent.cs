﻿using System.Collections.Generic;

using UnityEngine;

using RTSEngine.UI;
using RTSEngine.Entities;
using RTSEngine.Event;
using RTSEngine.Effect;
using RTSEngine.Determinism;
using RTSEngine.Audio;
using RTSEngine.Logging;
using RTSEngine.Task;
using RTSEngine.Movement;
using RTSEngine.Selection;
using RTSEngine.Utilities;
using RTSEngine.Search;
using RTSEngine.ResourceExtension;

namespace RTSEngine.EntityComponent
{
    public abstract class FactionEntityTargetComponent<T> : EntityComponentBase, IEntityTargetComponent where T : IEntity
    {
        #region Class Attributes
        // EDITOR ONLY
        [HideInInspector]
        public Int2D tabID = new Int2D {x = 0, y = 0};

        protected IFactionEntity factionEntity { private set; get; }

        public bool IsDisabled { private set; get; }

        [SerializeField, Tooltip("The active component with the lowest value will be considered for the right mouse click target set.")]
        private int priority = 0;
        public int Priority => priority;

        [SerializeField, Tooltip("Enable to require the entity, where this component is attached, to be idle when this component has an active target.")]
        private bool requireIdleEntity = true;
        public virtual bool RequireIdleEntity => requireIdleEntity;
        public abstract bool IsIdle { get; }

        /// <summary>
        /// The input data that the target was set with.
        /// </summary>
        public SetTargetInputData TargetInputData { get; protected set; }
        /// <summary>
        /// The instance that is being actively targetted.
        /// </summary>
        public TargetData<T> Target { get; protected set; }

        public EntityTargetComponentData TargetData => new EntityTargetComponentData
        {
            targetKey = Target.instance.GetKey(),
            position = Target.position,
            opPosition = Target.opPosition
        };
        // Assigned when Stop() is called to hold the last target.
        public TargetData<T> LastTarget { get; private set; }

        public virtual bool HasTarget => Target.instance.IsValid();


        [SerializeField, Tooltip("Set the settings for allowing the entity to launch this component automatically.")]
        private TargetEntityFinderData targetFinderData = new TargetEntityFinderData { enabled = false, idleOnly = true, range = 10.0f, reloadTime = 5.0f };
        protected TargetEntityFinder<T> targetFinder = null;
        public TargetEntityFinderData TargetFinderData => targetFinder.IsValid() ? targetFinder.Data : targetFinderData;
        public float TargetFinderCurrReloadValue => targetFinder.IsValid() ? targetFinder.CurrReloadValue : -1.0f;

        [SerializeField, Tooltip("Defines information used to display a task to set the target of this component in the task panel, when the faction entity is selected.")]
        private EntityComponentTaskUIAsset setTargetTaskUI = null;
        public EntityComponentTaskUIAsset SetTargetTaskUI => setTargetTaskUI;

        [SerializeField, Tooltip("What audio clip to play when the faction entity is ordered to perform the task of this component?")]
        private AudioClipFetcher orderAudio = new AudioClipFetcher();
        public AudioClip OrderAudio => orderAudio.Fetch();

        // Game services
        protected IInputManager inputMgr { private set; get; }
        protected IGameAudioManager audioMgr { private set; get; }
        protected IGlobalEventPublisher globalEvent { private set; get; }
        protected IEffectObjectPool effectObjPool { private set; get; }
        protected ISelectionManager selectionMgr { private set; get; }
        protected ISelector selector { private set; get; } 
        protected ITaskManager taskMgr { private set; get; }
        protected IMovementManager mvtMgr { private set; get; } 
        protected IGridSearchHandler gridSearch { private set; get; }
        protected IResourceManager resourceMgr { private set; get; }
        #endregion

        #region Raising Events
        public event CustomEventHandler<IEntityTargetComponent, TargetDataEventArgs> TargetUpdated;

        protected void RaiseTargetUpdated()
        {
            var handler = TargetUpdated;
            handler?.Invoke(this, new TargetDataEventArgs(Target));
        }

        public event CustomEventHandler<IEntityTargetComponent, TargetDataEventArgs> TargetStop;
        private void RaiseTargetStop()
        {
            var handler = TargetStop;
            handler?.Invoke(this, new TargetDataEventArgs(LastTarget));
        }
        #endregion

        #region Initializing/Terminating
        protected sealed override void OnInit ()
        {
            this.factionEntity = Entity as IFactionEntity;

            if (!logger.RequireTrue(!IsInitialized,
              $"[{GetType().Name} - {factionEntity.Code}] Component already initialized! It is not supposed to be initialized again! Please retrace and report!"))
                return; 

            this.inputMgr = gameMgr.GetService<IInputManager>();
            this.audioMgr = gameMgr.GetService<IGameAudioManager>();
            this.globalEvent = gameMgr.GetService<IGlobalEventPublisher>();
            this.effectObjPool = gameMgr.GetService<IEffectObjectPool>();
            this.selectionMgr = gameMgr.GetService<ISelectionManager>();
            this.selector = gameMgr.GetService<ISelector>(); 
            this.taskMgr = gameMgr.GetService<ITaskManager>();
            this.mvtMgr = gameMgr.GetService<IMovementManager>(); 
            this.gridSearch = gameMgr.GetService<IGridSearchHandler>();
            this.resourceMgr = gameMgr.GetService<IResourceManager>();

            if(targetFinderData.enabled)
                targetFinder = new TargetEntityFinder<T>(gameMgr, source: this, center: factionEntity.transform, data: targetFinderData);

            OnTargetInit();
        }

        protected virtual void OnTargetInit() { }

        protected sealed override void OnDisabled()
        {
            if (IsDisabled)
                return;

            Stop();
            if (targetFinder.IsValid())
                targetFinder.Disable();

            OnTargetDisabled();

            IsDisabled = true;
        }

        protected virtual void OnTargetDisabled() { }
        #endregion

        #region Handling Faction Update
        protected sealed override void OnFactionUpdateStart() 
        {
            Stop();
        }

        protected sealed override void OnFactionUpdateComplete() 
        {
        }
        #endregion

        #region Handling Component Upgrade
        public sealed override void HandleComponentUpgrade (IEntityComponent sourceEntityComponent)
        {
            FactionEntityTargetComponent<T> sourceFactionEntityTargetComponent = sourceEntityComponent as FactionEntityTargetComponent<T>;
            if (!sourceFactionEntityTargetComponent.IsValid())
                return;

            if (sourceFactionEntityTargetComponent.HasTarget)
            {
                TargetData<T> lastTarget = sourceFactionEntityTargetComponent.Target;
                bool lastTargetFromQueue = sourceFactionEntityTargetComponent.TargetInputData.fromTasksQueue;
                sourceFactionEntityTargetComponent.Disable();

                SetTarget(new SetTargetInputData 
                {
                    target = lastTarget,
                    playerCommand = false,
                    fromTasksQueue = lastTargetFromQueue 
                });
            }

            OnComponentUpgraded(sourceFactionEntityTargetComponent);
        }

        protected virtual void OnComponentUpgraded(FactionEntityTargetComponent<T> sourceFactionEntityTargetComponent) { }
        #endregion

        #region Activating/Deactivating Component
        protected sealed override void OnActiveStatusUpdated()
        {
            if(targetFinder.IsValid())
                targetFinder.IsActive = IsActive;

            // If the SetActive method is called before the componenty is fully initialized then it is like picking the initial settings for the activity status
            // And that means we do not need to Stop() or do any additional callbacks since the component has not initialized fully yet
            if (!IsInitialized)
                return;

            if (!IsActive)
                Stop();

            globalEvent.RaiseEntityComponentTaskUIReloadRequestGlobal(this);

            OnTargetActiveStatusUpdated();
        }

        protected virtual void OnTargetActiveStatusUpdated() { }
        #endregion

        #region Stopping
        public virtual bool CanStopOnSetIdleSource(IEntityTargetComponent idleSource) => true;

        protected virtual bool CanStopOnNoTarget() => true;

        public void Stop()
        {
            if (!CanStopOnNoTarget() && !HasTarget)
                return;

            audioMgr.StopSFX(factionEntity.AudioSourceComponent);

            globalEvent.RaiseEntityComponentTargetStopGlobal(this, new TargetDataEventArgs(Target));

            LastTarget = Target;
            Target = new TargetData<T> { instance = default, position = Target.position, opPosition = Target.opPosition };
            TargetInputData = new SetTargetInputData { target = Target };

            OnStop();

            RaiseTargetStop();
        }

        protected virtual void OnStop() { }
        #endregion

        #region Searching/Updating Target
        public virtual bool CanSearch => true;

        // IsTargetValid methods:
        /*
         * SetTargetLocal calls 'bool IsTargetValid(SetTargetInputData testInput, out ErrorMessage errorMessage)'
         * by default, that method calls 'ErrorMessage IsTargetValid(SetTargetInputData testInput)'
         * which then calls the 'ErrorMessage IsTargetValid(TargetData<IEntity> testTarget, bool playerCommand)' method
         * The latter is the abstract method that is implemented in most entity target components
         * The other two methods allow the implementations to this to add additional functionality when checking whether the target is valid
         * Examples would be the ResourceCollector and FactionEntityAttack scripts.
         * */
        public virtual bool IsTargetValid(SetTargetInputData testInput, out ErrorMessage errorMessage)
        {
            errorMessage = IsTargetValid(testInput);
            return errorMessage == ErrorMessage.none;
        }
        public abstract ErrorMessage IsTargetValid(SetTargetInputData testInput);
        public virtual ErrorMessage IsTargetValidOnSearch(SetTargetInputData testInput) => IsTargetValid(testInput);

        public abstract bool IsTargetInRange(Vector3 sourcePosition, TargetData<IEntity> target);

        public ErrorMessage SetTarget(TargetData<IEntity> newTarget, bool playerCommand)
            => SetTarget(new SetTargetInputData { target = newTarget, playerCommand = playerCommand });
        public virtual ErrorMessage SetTarget (SetTargetInputData input)
        {
            if(Entity.TasksQueue.IsValid() && Entity.TasksQueue.CanAdd(input))
            {
                return Entity.TasksQueue.Add(new SetTargetInputData 
                {
                    componentCode = Code,

                    target = input.target,
                    playerCommand = input.playerCommand,
                });
            }

            return inputMgr.SendInput(
                new CommandInput()
                {
                    sourceMode = (byte)InputMode.entity,
                    targetMode = (byte)InputMode.setComponentTarget,

                    targetPosition = input.target.position,
                    opPosition = input.target.opPosition,

                    code = Code,
                    playerCommand = input.playerCommand,

                    intValues = inputMgr.ToIntValues((int)input.BooleansToMask())
                },
                source: factionEntity,
                target: input.target.instance);
        }

        public virtual ErrorMessage SetTargetLocal(TargetData<IEntity> newTarget, bool playerCommand)
            => SetTargetLocal(new SetTargetInputData { target = newTarget, playerCommand = playerCommand });
        public virtual ErrorMessage SetTargetLocal(SetTargetInputData input)
        {
            if (!factionEntity.CanLaunchTask) 
                return ErrorMessage.taskSourceCanNotLaunch;

            IsTargetValid(input, out ErrorMessage errorMsg);
            if (errorMsg != ErrorMessage.none)
            {
                OnSetTargetError(input, errorMsg);
                if (input.playerCommand && RTSHelper.IsLocalPlayerFaction(factionEntity))
                    playerMsgHandler.OnErrorMessage(new PlayerErrorMessageWrapper
                    {
                        message = errorMsg,

                        source = Entity,
                        target = input.target.instance
                    });

                return errorMsg;
            }

            if (HasTarget)
                Stop();

            bool sameTarget = input.target.instance == Target.instance as IEntity && input.target.instance.IsValid();

            // If this component requires the entity to be idle to run then set the entity to idle before assigning the new target
            if(RequireIdleEntity)
                factionEntity.SetIdle(sameTarget ? this : null);

            OnTargetPreLocked(input.playerCommand, input.target, sameTarget);

            TargetInputData = input;
            Target = input.target;

            if (input.playerCommand && Target.instance.IsValid() && factionEntity.IsLocalPlayerFaction())
                selector.FlashSelection(Target.instance, factionEntity.IsFriendlyFaction(Target.instance));

            OnTargetPostLocked(input, sameTarget);

            RaiseTargetUpdated();

            return ErrorMessage.none;
        }

        protected virtual void OnSetTargetError(SetTargetInputData input, ErrorMessage errorMsg) { }

        protected virtual void OnTargetPreLocked(bool playerCommand, TargetData<IEntity> newTarget, bool sameTarget) { }

        protected virtual void OnTargetPostLocked (SetTargetInputData input, bool sameTarget) { }
        #endregion

        #region Task UI
        protected override bool OnTaskUICacheUpdate(List<EntityComponentTaskUIAttributes> taskUIAttributesCache, List<string> disabledTaskCodesCache)
        {
            return RTSHelper.OnSingleTaskUIRequest(
                this,
                taskUIAttributesCache,
                disabledTaskCodesCache,
                setTargetTaskUI);
        }

        public override bool OnTaskUIClick(EntityComponentTaskUIAttributes taskAttributes) 
        {
            if (SetTargetTaskUI.IsValid() && taskAttributes.data.code == SetTargetTaskUI.Key)
            {
                taskMgr.AwaitingTask.Enable(taskAttributes);
                return true;
            }

            return false;
        }

        public override bool OnAwaitingTaskTargetSet(EntityComponentTaskUIAttributes taskAttributes, TargetData<IEntity> target)
        {
            if (base.OnAwaitingTaskTargetSet(taskAttributes, target))
                return true;

            else if (SetTargetTaskUI.IsValid() && taskAttributes.data.code == SetTargetTaskUI.Key)
            {
                SetTarget(new SetTargetInputData 
                {
                    target = target,
                    playerCommand = true,
                });

                return true;
            }

            return false;
        }
        #endregion

        #region Editor
#if UNITY_EDITOR
        [SerializeField, HideInInspector]
        private bool showTargetGizmo = true;

        protected virtual void OnDrawGizmosSelected()
        {
            DrawTargetGizmo();
        }

        protected void DrawTargetGizmo()
        {
            if (HasTarget && showTargetGizmo)
            {
                // Target Gizmos
                Gizmos.color = Color.blue;
                Gizmos.DrawWireSphere(
                    Target.instance.IsValid() ? Target.instance.transform.position : Target.position,
                    Target.instance.IsValid() ? Target.instance.Radius : 0.5f);
            }
        }
#endif
        #endregion

    }
}
