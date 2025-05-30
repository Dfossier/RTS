﻿using RTSEngine.Determinism;
using RTSEngine.Entities;
using RTSEngine.EntityComponent;
using RTSEngine.Event;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace RTSEngine.Task
{
    // Set up actions for when the queue is active (when a new input is added and when it is cleared)
    public enum TasksQueueActionType : byte { addAndLaunchOnEmpty = 0, addNoLaunchOnEmpty = 1, clear = 50, setRunningComponent = 100 }

    public class EntityTasksQueueHandler : EntityComponentBase, IEntityTasksQueueHandler
    {
        #region Attributes
        //[SerializeField, Tooltip("Enable to allow for unlimited tasks to be added to the queue. Disable for limited capacity that can be set in the field right below.")]
        //private bool unlimitedCapacity = true;
        //[SerializeField, Tooltip("Maximum amount of tasks that can be in the queue at once, when the capacity is set to be limited.")]
        //private int maxCapacity = 10;

        [SerializeField, Tooltip("When enabled, the currently active task of the entity will not be stopped when the queue is empty and a new task is added to the queue. This is assuming that the currently active task is not a resource collection!")]
        private bool keepActiveTask = false;
        [SerializeField, Tooltip("When having the 'Keep Active Task' option enabled, enable this field to exclude resource collection tasks from the types of tasks that can remain active when adding the first task to the queue.")]
        private bool excludeResourceCollectionInKeepActiveTask = true;
        // In case the entity has a resource collector component, it is held here.
        private IResourceCollector resourceCollector;

        private List<SetTargetInputData> queue;
        public IEnumerable<SetTargetInputData> Queue => queue;
        public int QueueCount => queue.Count;

        public bool IsRunningQueueTask { private set; get; }
        public string RunningQueueTaskCompCode { private set; get; }

        protected ITaskManager taskMgr { private set; get; }
        #endregion

        #region Initializing/Terminating
        protected override void OnInit()
        {
            queue = new List<SetTargetInputData>();

            Entity.EntityComponentUpgraded += HandleEntityComponentUpgraded;

            this.taskMgr = gameMgr.GetService<ITaskManager>();

            IsRunningQueueTask = false;

            resourceCollector = Entity.IsUnit() ? (Entity as IUnit).CollectorComponent : null;

            Entity.FactionUpdateComplete += HandleFactionUpdateComplete;
        }

        protected override void OnDisabled()
        {
        }
        #endregion

        #region Handle Events
        private void HandleFactionUpdateComplete(IEntity entity, FactionUpdateArgs args)
        {
            ClearLocal();
        }

        private void HandleEntityComponentUpgraded(IEntity entity, EntityComponentUpgradeEventArgs args)
        {
            if (!args.SourceComp.IsValid())
                return;

            if (IsRunningQueueTask && RunningQueueTaskCompCode == args.SourceComp.Code)
            {
                (args.SourceComp as IEntityTargetComponent).TargetStop -= HandleComponentTargetStop;

                RunningQueueTaskCompCode = args.TargetComp.Code;
                (args.TargetComp as IEntityTargetComponent).TargetStop += HandleComponentTargetStop;
            }

            int i = 0;
            while (i < queue.Count)
            {
                SetTargetInputData task = queue[i];
                if (task.componentCode == args.SourceComp.Code)
                    task.componentCode = args.TargetComp.Code;

                queue[i] = task;

                i++;
            }

            //print($"upgarded queue: {queue.Count}");
        }

        private void HandleComponentTargetStop(IEntityTargetComponent sender, TargetDataEventArgs args)
        {
            sender.TargetStop -= HandleComponentTargetStop;

            //print($"stopped queued task: {queue.Count}");
            while (queue.Count > 0)
            {
                if (TryLaunchNext())
                    return;
            }

            if (queue.Count == 0)
                IsRunningQueueTask = false;
        }
        #endregion

        #region Launching Actions
        public override ErrorMessage LaunchActionLocal(byte actionID, SetTargetInputData input)
        {
            switch ((TasksQueueActionType)actionID)
            {
                case TasksQueueActionType.addAndLaunchOnEmpty:
                    return AddLocal(input, launchOnEmpty: true);
                case TasksQueueActionType.addNoLaunchOnEmpty:
                    return AddLocal(input, launchOnEmpty: false);

                case TasksQueueActionType.clear:
                    ClearLocal();
                    return ErrorMessage.none;

                case TasksQueueActionType.setRunningComponent:
                    return SetRunningComponent(input.componentCode);

                default:
                    return base.LaunchActionLocal(actionID, input);
            }
        }
        #endregion

        #region Adding Tasks
        public bool CanAdd(SetTargetInputData input)
        {
            if (IsActive
                && input.playerCommand
                && !input.fromTasksQueue
                && Entity.IsLocalPlayerFaction()
                && taskMgr.IsTaskQueueEnabled)
            {
                //return unlimitedCapacity || QueueCount < maxCapacity;
                return true;
            }
            else
            {
                if (!input.fromTasksQueue)
                    Clear();
                return false;
            }
        }

        public ErrorMessage Add(SetTargetInputData input)
            => LaunchAction((byte)TasksQueueActionType.addAndLaunchOnEmpty, input);

        private ErrorMessage AddLocal(SetTargetInputData input, bool launchOnEmpty = true)
        {
            input.fromTasksQueue = true;
            queue.Add(input);

            bool canStopCollectingResource = resourceCollector.IsValid() && resourceCollector.HasTarget && excludeResourceCollectionInKeepActiveTask;

            if (launchOnEmpty && (!IsRunningQueueTask || canStopCollectingResource) && queue.Count == 1)
            {
                IEntityTargetComponent activeTargetComponent = Entity
                    .EntityTargetComponents
                    .Values
                    .FirstOrDefault(comp => comp.HasTarget && !comp.IsIdle);

                // If there is no active target component or it is the resource collector one and we are allowed to bypass that
                // Then launch the newly added task to the empty queue directly
                if (!activeTargetComponent.IsValid() || !keepActiveTask || canStopCollectingResource || Entity.IsIdle)
                    TryLaunchNext(directLaunch: true);
                else
                {
                    SetRunningComponent(activeTargetComponent.Code, force: true);
                }
            }

            return ErrorMessage.none;
        }

        private bool TryLaunchNext(bool directLaunch = false)
        {
            if (queue.Count == 0)
                return false;

            SetTargetInputData nextInput = queue[0];
            // Mark the task as a player command only if it is marked as the first one to launch in the queue.
            nextInput.playerCommand = directLaunch;

            queue.RemoveAt(0);

            if (!Entity.EntityTargetComponents[nextInput.componentCode].IsTargetValid(nextInput, out ErrorMessage errorMessage))
                return false;

            if((nextInput.playerCommand && Entity.HasAuthority())
                || (!nextInput.playerCommand && RTSHelper.IsMasterInstance()))
                Entity.EntityTargetComponents[nextInput.componentCode].SetTarget(nextInput);

            SetRunningComponent(nextInput.componentCode, force: true);

            return true;
        }

        private ErrorMessage SetRunningComponent(string componentCode, bool force = false)
        {
            if (IsRunningQueueTask && !force)
                return ErrorMessage.invalid;

            Entity.EntityTargetComponents[componentCode].TargetStop += HandleComponentTargetStop;
            RunningQueueTaskCompCode = componentCode;

            IsRunningQueueTask = true;

            return ErrorMessage.none;
        }
        #endregion

        #region Clear Tasks
        public void Clear()
        {
            LaunchAction((byte)TasksQueueActionType.clear, default);
        }

        private void ClearLocal()
        {
            //print("queue cleared");

            queue.Clear();
            IsRunningQueueTask = false;
        }
        #endregion

        #region Active Status Handling
        protected override void OnActiveStatusUpdated()
        {
            if (!IsActive)
                ClearLocal();
        }
        #endregion
    }
}
