﻿using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

using RTSEngine.Determinism;
using RTSEngine.Entities;
using RTSEngine.Event;
using RTSEngine.Game;
using RTSEngine.UnitExtension;
using RTSEngine.Logging;
using RTSEngine.Model;

namespace RTSEngine.Animation
{
    public class UnitAnimatorController : MonoBehaviour, IAnimatorController, IEntityPostInitializable
    {
        #region Attributes
        public IUnit Unit { private set; get; }

        //[SerializeField, Tooltip("Animator responsible for playing the unit animaiton clips."), Header("General")]
        //private ModelCacheAwareAnimatorInput animatorInput;

        [SerializeField, Tooltip("Animator responsible for playing the unit animaiton clips."), Header("General")]
        private Animator animator = null;
        public Animator Animator => animator;

        private TimeModifiedFloat animatorSpeed;

        public AnimatorState CurrState { private set; get; } = AnimatorState.invalid;

        [SerializeField, Tooltip("The default animator override controller of the unit.")]
        private AnimatorOverrideControllerFetcher animatorOverrideController = new AnimatorOverrideControllerFetcher();

        public bool LockState { set; get; }

        /// <summary>
        /// Using a parameter in the Animator component, this determines whether the unit is currently in the moving animator state or not.
        /// This allows other components to handle movement related actions smoothly and sync them correctly with the unit's movement
        /// </summary>
        public bool IsInMvtState =>
            (animator.gameObject.activeInHierarchy && animator.GetBool(UnitAnimator.Parameters[AnimatorState.movingState]))
            || CurrState == AnimatorState.moving;

        [SerializeField, Tooltip("Play the take damage animation when the unit is damaged?"), Header("Damage Animation")]
        private bool damageAnimationEnabled = false;
        [SerializeField, Tooltip("How long does the take damage animation last for?")]
        private float damageAnimationDuration = 0.2f;

        public bool IsDamageAnimationEnabled => damageAnimationEnabled;
        private Coroutine damageAnimationCoroutine;

        //used for the override reset coroutine which waits for the state to get to idle before resetting the animator state
        private Coroutine overrideResetCoroutine;
        private AnimatorOverrideController currController;

        private WaitWhile resetControllerWaitWhile;

        // Game services
        protected IUnitManager unitMgr { private set; get; }
        protected IGameLoggingService logger { private set; get; }
        protected ITimeModifier timeModifier { private set; get; } 
        #endregion

        #region Initializing/Terminating
        public void OnEntityPostInit(IGameManager gameMgr, IEntity entity)
        {
            this.unitMgr = gameMgr.GetService<IUnitManager>();
            this.logger = gameMgr.GetService<IGameLoggingService>();
            this.timeModifier = gameMgr.GetService<ITimeModifier>();

            this.Unit = entity as IUnit;

            if (!animator.IsValid())
            {
                logger.LogError($"[{GetType().Name}] The 'Animator' field must be assigned!");
                return;
            }

            resetControllerWaitWhile = new WaitWhile(() => CurrState != AnimatorState.idle);

            animatorSpeed = new TimeModifiedFloat(animator.speed);
            animator.speed = animatorSpeed.Value;

            ResetAnimatorState();

            Unit.Health.EntityHealthUpdated += HandleUnitHealthUpdated;

            timeModifier.ModifierUpdated += HandleModifierUpdated;
        }

        public void Disable()
        {
            SetState(AnimatorState.dead);

            Unit.Health.EntityHealthUpdated -= HandleUnitHealthUpdated;

            timeModifier.ModifierUpdated -= HandleModifierUpdated;
        }
        #endregion

        #region Handling Event: Time Modifier Update
        private void HandleModifierUpdated(ITimeModifier sender, EventArgs args)
        {
            animator.speed = animatorSpeed.Value;
        }
        #endregion

        #region Handling Events: Unit
        private void HandleUnitHealthUpdated(IEntity unit, HealthUpdateArgs e)
        {
            //only deal with the case where the unit receives damage.
            if (e.Value >= 0)
                return;

            if (damageAnimationEnabled)
            {
                SetState(AnimatorState.startTakeDamage);

                StartCoroutine(DisableTakeDamageAnimation(damageAnimationDuration));
            }

        }
        #endregion

        #region Updating Animator State
        private void ResetAnimatorState()
        {
            ResetOverrideController();

            SetState(AnimatorState.idle);
        }

        public void SetState(AnimatorState newState)
        {
            if (LockState == true)
                return;

            if (CurrState == AnimatorState.dead)
                return;

            // If the damage animation is active, only allow to change the animation if the next one is a death animation.
            if(newState != AnimatorState.dead && IsStartingOrInTakeDamageState())
            {
                if (CurrState == AnimatorState.startTakeDamage && newState != AnimatorState.inTakeDamage)
                    return;

                else if (CurrState == AnimatorState.inTakeDamage && newState != AnimatorState.idle)
                    return;
            }

            CurrState = newState;

            animator.SetBool(UnitAnimator.Parameters[AnimatorState.startTakeDamage], CurrState == AnimatorState.startTakeDamage);

            // Stop the idle animation in case take damage animation is played since the take damage animation is broken by the idle anim
            animator.SetBool(UnitAnimator.Parameters[AnimatorState.idle], CurrState == AnimatorState.idle);

            // If the new animator state is the taking damage one then do not disable the rest of animations since as soon as the take damage animation is disabled, we want to get back to the last active state
            if (CurrState == AnimatorState.startTakeDamage)
            {
                animator.SetBool(UnitAnimator.Parameters[AnimatorState.inProgress], false);
                return;
            }

            foreach (AnimatorState state  in UnitAnimator.States)
                if (state != AnimatorState.movingState)
                    animator.SetBool(UnitAnimator.Parameters[state], state == CurrState);

            animator.SetBool(UnitAnimator.Parameters[AnimatorState.movingState], CurrState == AnimatorState.moving);
        }

        private bool IsStartingOrInTakeDamageState() => (CurrState == AnimatorState.startTakeDamage || CurrState == AnimatorState.inTakeDamage);

        private IEnumerator DisableTakeDamageAnimation (float delay)
        {
            yield return new WaitUntil(() => animator.GetBool(UnitAnimator.Parameters[AnimatorState.inTakeDamage]) == true);

            SetState(AnimatorState.inTakeDamage);

            yield return new WaitForSeconds(delay);

            SetState(AnimatorState.idle);
        }
        #endregion

        #region Updating Animator Override Controller
        public void SetOverrideController(AnimatorOverrideController newOverrideController)
        {
            // Only if the unit is not in its dead animation state do we reset the override controller
            // And since all parameters reset when the unit is dead and the unit is locked in its death state
            // Reseting the controller makes it start from its "entry state" back to "idle" state, this makes the unit leave its death state while still marked as dead in the currAnimatorState
            if (!newOverrideController.IsValid()
                || CurrState == AnimatorState.dead)
                return;

            if (overrideResetCoroutine.IsValid())
                StopCoroutine(overrideResetCoroutine);

            this.currController = newOverrideController;
            animator.runtimeAnimatorController = newOverrideController;

            // Since changing the override controller resets all parameters, we need to re-set the current animator state
            SetState(CurrState);
        }

        public void ResetAnimatorOverrideControllerOnIdle()
        {
            overrideResetCoroutine = StartCoroutine(HandleResetAnimatorOverrideControllerOnIdle());
        }

        private IEnumerator HandleResetAnimatorOverrideControllerOnIdle()
        {
            yield return resetControllerWaitWhile;

            ResetOverrideController();
        }

        public void ResetOverrideController ()
        {
            if (overrideResetCoroutine.IsValid())
                StopCoroutine(overrideResetCoroutine);

            AnimatorOverrideController nextController = animatorOverrideController.Fetch();
            SetOverrideController(nextController.IsValid() ? nextController : unitMgr.DefaultAnimController);
        }
        #endregion
    }
}
