using UnityEngine;

using RTSEngine.Entities;
using RTSEngine.Event;
using RTSEngine.Movement;
using System.Linq;
using RTSEngine.ResourceExtension;
using RTSEngine.DevTools.ResourceExtension;
using System.Collections.Generic;

namespace RTSEngine.EntityComponent
{
    public class Converter : FactionEntityTargetProgressComponent<IFactionEntity>
    {
        #region Class Attributes
        [SerializeField, Tooltip("When assigned a target, this is the stopping distance that the converter will have when moving towards the target"), Min(0.0f)]
        private float stoppingDistance = 5.0f;
        private int requiredFoodAmount = 0;

        [SerializeField, Tooltip("Define the faction entities that can be converted by this converter.")]
        private AdvancedFactionEntityTargetPicker targetPicker = new AdvancedFactionEntityTargetPicker();
        #endregion

        #region Updating Component State
        protected override bool MustStopProgress()
        {
            return Target.instance.Health.IsDead
                || RTSHelper.IsSameFaction(Target.instance, factionEntity)
                || (InProgress && !IsTargetInRange(factionEntity.transform.position, Target))
                || !HaveEnoughFood(out _);
        }

        protected override bool CanEnableProgress()
        {
            return IsTargetInRange(factionEntity.transform.position, Target);
        }

        protected override bool CanProgress() => true;

        protected override bool MustDisableProgress() => false;
        #endregion

        #region Handling Progress
        protected override void OnInProgressEnabled()
        {
            base.OnInProgressEnabled();

            globalEvent.RaiseEntityComponentTargetStartGlobal(this, new TargetDataEventArgs(Target));
        }

        protected override void OnProgress()
        {
            if (CustomAnimalHerdingLogic() == false)
                return;

            Target.instance.SetFaction(factionEntity, factionEntity.FactionID); //convert target unit

            Stop(); //cancel conversion job.
        }

        private bool HaveEnoughFood(out ResourceTypeInfo foodRef)
        {
            if (resourceMgr.TryGetResourceTypeWithKey("food", out ResourceTypeInfo food))
            {
                foodRef = food;
            }
            else
            {
                foodRef = null;
                return false;
            }

            int foodAmount = resourceMgr
                .FactionResources[factionEntity.FactionID]
                .ResourceHandlers[foodRef]
                .Amount;

            if (foodAmount < requiredFoodAmount)
                return false;

            return true;
        }


        private bool CustomAnimalHerdingLogic()
        {
            ResourceTypeInfo foodRef;

            if (!HaveEnoughFood(out foodRef))
            {
                return false;
            }

            if (resourceMgr.TryGetResourceTypeWithKey("food", out ResourceTypeInfo food))
            {
                // Success: you can use "food"
                Debug.Log("Found resource type: " + food.Key);
                foodRef = food;
            }
            else
            {
                // Failure: you cannot use "food"
                Debug.Log("Resource type not found.");
                return false;
            }

            int foodAmount = resourceMgr.FactionResources[factionEntity.FactionID].ResourceHandlers[foodRef].Amount;

            Debug.Log("Current food amount: " + foodAmount);
            if (foodAmount < requiredFoodAmount)
            {
                Debug.Log("Food is low!");
                return false;
            }


            string prefabName = Target.instance.gameObject.name.Replace("(Clone)", "").Trim();
            string[] animals = { "cow", "deer", "horse" };
            if (animals.Contains(prefabName))
            {
                Debug.Log("Converted a " + prefabName + "!");
                // so if it's a animal, we need to change it's behavior to follow the converter and stuff like that

                // first we assign the converter as the new owner inside the animal's script
                AnimalsOwnerController animal = Target.instance.GetComponent<AnimalsOwnerController>();
                animal.Owner = factionEntity.gameObject;

                resourceMgr.UpdateResource(
                    factionEntity.FactionID,
                    new ResourceInput
                    {
                        type = foodRef,
                        value = new ResourceTypeValue
                        {
                            amount = -requiredFoodAmount,
                            capacity = 0
                        }
                    },
                    add: true
                );
                return true;
            }
            return false;
        }
        #endregion

        #region Searching/Updating Target
        public override ErrorMessage IsTargetValid(SetTargetInputData data)
        {
            TargetData<IFactionEntity> potentialTarget = data.target;

            if (!potentialTarget.instance.IsValid())
                return ErrorMessage.invalid;
            // In the case of a building that is yet to be constructed, we check using the CanLaunchTask property of the target (which takes into accoun the construction status in case target is a building).
            else if (potentialTarget.instance.IsDummy)
                return ErrorMessage.uninteractable;
            else if (RTSHelper.IsSameFaction(potentialTarget.instance, factionEntity))
                return ErrorMessage.factionIsFriendly;
            else if (potentialTarget.instance.IsFactionLocked)
                return ErrorMessage.factionLocked;
            else if (!targetPicker.IsValidTarget(sourceComponent: this, potentialTarget.instance))
                return ErrorMessage.targetPickerUndefined;
            else if (potentialTarget.instance.Health.IsDead)
                return ErrorMessage.healthDead;
            else if (!factionEntity.CanMove() && !IsTargetInRange(factionEntity.transform.position, potentialTarget))
                return ErrorMessage.targetOutOfRange;

            return ErrorMessage.none;
        }

        protected override void OnTargetPostLocked(SetTargetInputData input, bool sameTarget)
        {
            globalEvent.RaiseEntityComponentTargetLockedGlobal(this, new TargetDataEventArgs(Target));

            if (!factionEntity.CanMove())
                return;

            if (!IsTargetInRange(factionEntity.transform.position, Target))
                factionEntity.MovementComponent.SetTarget(
                    Target,
                    stoppingDistance,
                    new MovementSource
                    {
                        sourceTargetComponent = this,

                        playerCommand = false,

                        isMoveAttackRequest = input.isMoveAttackRequest
                    });
            else
                factionEntity.MovementComponent.UpdateRotationTarget(Target.instance, Target.position);
        }
        #endregion

        #region Stopping
        protected override void OnProgressStop()
        {
            if (factionEntity.MovementComponent.IsValid())
                factionEntity.MovementComponent.UpdateRotationTarget(factionEntity.transform.rotation);
        }
        #endregion

        private void OnDestroy()
        {
            // If the converter (herder) dies, all animals following it must return to neutral.

            AnimalsOwnerController[] allAnimals = FindObjectsOfType<AnimalsOwnerController>();

            if (allAnimals.Length > 0)
            {
                foreach (var animal in allAnimals)
                {
                    if(!animal.IsValid()) continue;
                    // Was this animal owned by this converter?
                    if (animal.Owner == factionEntity.gameObject)
                    {
                        // Remove the ownership reference
                        animal.Owner = null;

                        // Reset the faction back to neutral (0 or NeutralFactionID)
                        FactionEntity entity = animal.GetComponent<FactionEntity>();
                        if (entity != null)
                        {
                            entity.SetFaction(entity, -1); // set back to no faction
                        }
                    }
                }

            }

            //Debug.Log("Converter destroyed. All converted animals returned to neutral.");
        }

    }
}
