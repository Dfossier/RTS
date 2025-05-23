﻿using UnityEngine;
using UnityEditor;

using RTSEngine.EntityComponent;
using RTSEngine.Utilities;

namespace RTSEngine.EditorOnly.EntityComponent
{

    [CustomEditor(typeof(UnitAttack))]
    public class UnitAttackEditor : FactionEntityAttackEditor<UnitAttack>
    {
        private string[][] toolbars = new string[][] {
            new string [] { "General", "Launcher", "Damage",},
            new string [] { "Weapon", "LOS", "Attack-Move" },
            new string [] { "UI", "Audio", "Events", "Debug" }
        };

        public override void OnInspectorGUI()
        {
            EditorGUILayout.LabelField($"Entity Component (Source: IUnit - Target: IFactionEntity)", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            base.OnInspectorGUI(toolbars);
        }

        protected override void OnGeneralInspectorGUI()
        {
            EditorGUILayout.PropertyField(SO.FindProperty("code"));
            EditorGUILayout.PropertyField(SO.FindProperty("priority"));
            EditorGUILayout.PropertyField(SO.FindProperty("isLocked"));
            EditorGUILayout.PropertyField(SO.FindProperty("isActive"));
            EditorGUILayout.PropertyField(SO.FindProperty("requireIdleEntity"));
            EditorGUILayout.PropertyField(SO.FindProperty("revert"));

            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(SO.FindProperty("formation"), true);

            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(SO.FindProperty("engageOptions"), true);
            EditorGUILayout.PropertyField(SO.FindProperty("moveOnAttack"));

            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(SO.FindProperty("targetFinderData"), true);
            EditorGUILayout.PropertyField(SO.FindProperty("followDistance"));


            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(SO.FindProperty("requireTarget"));
            EditorGUILayout.PropertyField(SO.FindProperty("allowMultipleTerrainAttacks"));
            EditorGUILayout.PropertyField(SO.FindProperty("targetPicker"), true);

            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(SO.FindProperty("reloadDuration"));

            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(SO.FindProperty("progressDuration"), new GUIContent("Delay Time"));
            EditorGUILayout.PropertyField(SO.FindProperty("delayTriggerEnabled"));

            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(SO.FindProperty("cooldown"), true);

            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(SO.FindProperty("progressOverrideController"), new GUIContent("Attack Override Controller"), true);
        }

        protected override void OnMoveAttackInspectorGUI()
        {
            EditorGUILayout.PropertyField(SO.FindProperty("attackMoveEnabled"), true);
            EditorGUILayout.PropertyField(SO.FindProperty("attackMoveTaskUI"), true);
        }

    }

    [CustomEditor(typeof(BuildingAttack))]
    public class BuildingAttackEditor : FactionEntityAttackEditor<BuildingAttack>
    {
        public override void OnInspectorGUI()
        {
            EditorGUILayout.LabelField($"Entity Component (Source: IBuilding - Target: IFactionEntity)", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            base.OnInspectorGUI();
        }

        protected override void OnGeneralInspectorGUI()
        {
            EditorGUILayout.PropertyField(SO.FindProperty("code"));
            EditorGUILayout.PropertyField(SO.FindProperty("priority"));
            EditorGUILayout.PropertyField(SO.FindProperty("isLocked"));
            EditorGUILayout.PropertyField(SO.FindProperty("isActive"));
            EditorGUILayout.PropertyField(SO.FindProperty("requireIdleEntity"));
            EditorGUILayout.PropertyField(SO.FindProperty("revert"));

            EditorGUILayout.Space();
            //EditorGUILayout.PropertyField(SO.FindProperty("progressMaxDistance"), new GUIContent("Old Attack Range"));
            EditorGUILayout.PropertyField(SO.FindProperty("attackDistance"), new GUIContent("Attack Range"));

            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(SO.FindProperty("engageOptions"), true);

            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(SO.FindProperty("targetFinderData"), true);

            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(SO.FindProperty("requireTarget"));
            EditorGUILayout.PropertyField(SO.FindProperty("allowMultipleTerrainAttacks"));
            EditorGUILayout.PropertyField(SO.FindProperty("targetPicker"), true);

            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(SO.FindProperty("reloadDuration"));

            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(SO.FindProperty("progressDuration"), new GUIContent("Delay Time"));
            EditorGUILayout.PropertyField(SO.FindProperty("delayTriggerEnabled"));

            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(SO.FindProperty("cooldown"), true);
        }
    }

    public class FactionEntityAttackEditor<T> : TabsEditorBase<T> where T : FactionEntityAttack
    {
        protected override Int2D tabID {
            get => comp.tabID;
            set => comp.tabID = value;
        }

        private string[][] toolbars = new string[][] {
            new string [] { "General", "Launcher", "Damage", "Weapon" },
            new string [] { "LOS", "UI", "Audio", "Events", "Debug" }
        };

        public override void OnInspectorGUI()
        {
            OnInspectorGUI(toolbars);
        }

        protected override void OnTabSwitch(string tabName)
        {
            switch (tabName)
            {
                case "General":
                    OnGeneralInspectorGUI();
                    break;
                case "Launcher":
                    OnLauncherInspectorGUI();
                    break;
                case "Damage":
                    OnDamageInspectorGUI();
                    break;
                case "Weapon":
                    OnWeaponInspectorGUI();
                    break;
                case "LOS":
                    OnLOSInspectorGUI();
                    break;
                case "Attack-Move":
                    OnMoveAttackInspectorGUI();
                    break;
                case "UI":
                    OnUIInspectorGUI();
                    break;
                case"Audio":
                    OnAudioInspectorGUI();
                    break;
                case "Events":
                    OnEventsInspectorGUI();
                    break;
                case "Debug":
                    OnDebugInspectorGUI();
                    break;
            }
        }

        protected virtual void OnGeneralInspectorGUI()
        {
            EditorGUILayout.PropertyField(SO.FindProperty("code"));
            EditorGUILayout.PropertyField(SO.FindProperty("priority"));
            EditorGUILayout.PropertyField(SO.FindProperty("isLocked"));
            EditorGUILayout.PropertyField(SO.FindProperty("isActive"));
            EditorGUILayout.PropertyField(SO.FindProperty("requireIdleEntity"));
            EditorGUILayout.PropertyField(SO.FindProperty("revert"));

            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(SO.FindProperty("engageOptions"), true);

            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(SO.FindProperty("targetFinderData"), true);

            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(SO.FindProperty("requireTarget"));
            EditorGUILayout.PropertyField(SO.FindProperty("allowMultipleTerrainAttacks"));
            EditorGUILayout.PropertyField(SO.FindProperty("targetPicker"), true);

            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(SO.FindProperty("reloadDuration"));

            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(SO.FindProperty("progressDuration"), new GUIContent("Delay Time"));
            EditorGUILayout.PropertyField(SO.FindProperty("delayTriggerEnabled"));

            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(SO.FindProperty("cooldown"), true);

            EditorGUILayout.Space();
        }

        protected virtual void OnLauncherInspectorGUI()
        {
            EditorGUILayout.PropertyField(SO.FindProperty("launcher.useAttackObjects"));
            if (SO.FindProperty("launcher.useAttackObjects").boolValue)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(SO.FindProperty("launcher.launchType"));
                EditorGUILayout.PropertyField(SO.FindProperty("launcher.sources"), true);
                EditorGUI.indentLevel--;
            }
        }

        protected virtual void OnDamageInspectorGUI()
        {
            EditorGUILayout.PropertyField(SO.FindProperty("damage.enabled"), new GUIContent("Deal Damage"));

            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(SO.FindProperty("damage.data"), true);

            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(SO.FindProperty("damage.areaAttackEnabled"));
            if (SO.FindProperty("damage.areaAttackEnabled").boolValue == true)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(SO.FindProperty("damage.areaAttackData"), true);
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(SO.FindProperty("damage.dotEnabled"));
            if (SO.FindProperty("damage.dotEnabled").boolValue == true)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(SO.FindProperty("damage.dotData"), new GUIContent("DoT Data"), true);
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(SO.FindProperty("damage.hitEffects"), true);

            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(SO.FindProperty("damage.resetDamageDealt"));
        }

        protected virtual void OnWeaponInspectorGUI()
        {
            EditorGUILayout.PropertyField(SO.FindProperty("weapon.toggableObjects"), true);
            EditorGUILayout.PropertyField(SO.FindProperty("weapon.toggleType"), true);
            EditorGUILayout.Space();

            EditorGUILayout.PropertyField(SO.FindProperty("inProgressObject"), new GUIContent("Weapon Object"));
            if (SO.FindProperty("inProgressObject").objectReferenceValue == null)
                return;

            EditorGUILayout.PropertyField(SO.FindProperty("weapon.updateRotation"));
            if (!SO.FindProperty("weapon.updateRotation").boolValue)
                return;

            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(SO.FindProperty("weapon.rotateInRangeOnly"), new GUIContent("Rotate In Attack Range Only"));
            EditorGUILayout.PropertyField(SO.FindProperty("weapon.smoothRotation"));
            if (SO.FindProperty("weapon.smoothRotation").boolValue == true)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(SO.FindProperty("weapon.rotationDamping"));
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(SO.FindProperty("weapon.freezeRotationX"));
            EditorGUILayout.PropertyField(SO.FindProperty("weapon.freezeRotationY"));
            EditorGUILayout.PropertyField(SO.FindProperty("weapon.freezeRotationZ"));

            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(SO.FindProperty("weapon.forceIdleRotation"));
            if (SO.FindProperty("weapon.forceIdleRotation").boolValue == true)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(SO.FindProperty("weapon.idleAngles"));
                EditorGUI.indentLevel--;
            }
        }

        protected virtual void OnLOSInspectorGUI()
        {
            EditorGUILayout.PropertyField(SO.FindProperty("lineOfSight.enabled"));
            if (SO.FindProperty("lineOfSight.enabled").boolValue == false)
                return;

            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(SO.FindProperty("lineOfSight.useWeaponObject"));
            EditorGUILayout.PropertyField(SO.FindProperty("lineOfSight.angle"));
            EditorGUILayout.PropertyField(SO.FindProperty("lineOfSight.obstacleLayerMask"));

            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(SO.FindProperty("lineOfSight.ignoreRotationX"));
            EditorGUILayout.PropertyField(SO.FindProperty("lineOfSight.ignoreRotationY"));
            EditorGUILayout.PropertyField(SO.FindProperty("lineOfSight.ignoreRotationZ"));
        }

        protected virtual void OnMoveAttackInspectorGUI()
        {
        }

        protected virtual void OnUIInspectorGUI()
        {
            EditorGUILayout.PropertyField(SO.FindProperty("setTargetTaskUI"), true);
            EditorGUILayout.PropertyField(SO.FindProperty("setTargetCooldownUIData"), true);

            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(SO.FindProperty("switchTaskUI"), new GUIContent("Switch Attack Task UI"));
            EditorGUILayout.PropertyField(SO.FindProperty("switchAttackCooldownUIData"), true);

            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(SO.FindProperty("cancelTaskUI"), new GUIContent("Cancel Attack Task UI"));
        }

        protected virtual void OnAudioInspectorGUI()
        {
            EditorGUILayout.PropertyField(SO.FindProperty("progressEnabledAudio"), new GUIContent("Attack Range Enter Audio"), true);
            EditorGUILayout.PropertyField(SO.FindProperty("orderAudio"), true);
            EditorGUILayout.PropertyField(SO.FindProperty("attackCompleteAudio"), true);
        }

        protected virtual void OnEventsInspectorGUI()
        {
            EditorGUILayout.PropertyField(SO.FindProperty("attackRangeEnterEvent"));
            EditorGUILayout.PropertyField(SO.FindProperty("targetLockedEvent"));
            EditorGUILayout.PropertyField(SO.FindProperty("damage.damageDealtEvent"));
            EditorGUILayout.PropertyField(SO.FindProperty("launcher.attackIterationLaunchEvent"));
            EditorGUILayout.PropertyField(SO.FindProperty("completeEvent"));
        }

        protected virtual void OnDebugInspectorGUI()
        {
            if(!Application.isPlaying)
            {
                EditorGUILayout.LabelField("Debug fields only visible when application is running!");
                return;
            }

            GUI.enabled = false;
            EditorGUILayout.LabelField("General", EditorStyles.boldLabel);
            EditorGUILayout.Toggle("Is Active", comp.IsActive);
            EditorGUILayout.Toggle("Is Component Idle", comp.IsIdle);
            EditorGUILayout.Toggle("Is Entity Idle", comp.Entity.IsValid() ? comp.Entity.IsIdle : true);
            EditorGUILayout.Toggle("Is Locked", comp.IsLocked);

            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Target", EditorStyles.boldLabel);
            EditorGUILayout.Toggle("Has Target", comp.HasTarget);
            EditorGUILayout.ObjectField("Target Object", comp.Target.instance?.gameObject, typeof(GameObject), allowSceneObjects: true);
            EditorGUILayout.Vector3Field("Target Position", comp.Target.position);
            EditorGUILayout.Vector3Field("Target (Optional) Position", comp.Target.opPosition);
            GUI.enabled = true;
            EditorGUILayout.PropertyField(SO.FindProperty("showTargetGizmo"));
            GUI.enabled = false;

            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Target Search", EditorStyles.boldLabel);
            EditorGUILayout.Toggle("Can Search", comp.CanSearch);
            EditorGUILayout.Toggle("Search Enabled", comp.TargetFinderData.enabled);
            EditorGUILayout.Toggle("Entity Can Launch Tasks", comp.Entity.IsValid() ? comp.Entity.CanLaunchTask : false);
            EditorGUILayout.Toggle("Search When Entity Idle Only", comp.TargetFinderData.idleOnly);
            EditorGUILayout.FloatField("Search Range", comp.TargetFinderData.range);
            EditorGUILayout.FloatField("Reload Time", comp.TargetFinderData.reloadTime);
            EditorGUILayout.FloatField("Current Reload Value", comp.TargetFinderCurrReloadValue);

            EditorGUILayout.Space();

            var losError = comp.HasTarget ? comp.LineOfSight.IsInSight(comp.Target) : ErrorMessage.targetUnassigned;

            EditorGUILayout.LabelField("Attack Iteration", EditorStyles.boldLabel);
            EditorGUILayout.Toggle("Cooldown Enabled", comp.IsCooldownActive);
            EditorGUILayout.FloatField("Current Cooldown Value", comp.CurrCooldownValue);
            EditorGUILayout.PropertyField(SO.FindProperty("isAttackReady"));
            EditorGUILayout.PropertyField(SO.FindProperty("terrainAttackActive"));
            EditorGUILayout.Toggle("Move Attack Active", comp.IsAttackMoveActive);
            EditorGUILayout.Toggle("Is In Target Range", comp.IsInTargetRange);
            EditorGUILayout.PropertyField(SO.FindProperty("reloadDuration"));
            EditorGUILayout.FloatField("Current Reload Value", comp.CurrReloadValue);
            EditorGUILayout.EnumFlagsField("Line Of Sight Error", losError);

            EditorGUILayout.Toggle("Can Launch Attack", comp.HasTarget && comp.CanStartAttackIteration() == ErrorMessage.none);
            GUI.enabled = true;
            EditorGUILayout.PropertyField(SO.FindProperty("showAttackIterationGizmos"));
            GUI.enabled = false;

            EditorGUILayout.PropertyField(SO.FindProperty("delayTriggerEnabled"));
            EditorGUILayout.PropertyField(SO.FindProperty("attackIterationTriggered"));

            EditorGUILayout.PropertyField(SO.FindProperty("damage.resetDamageDealt"));
            EditorGUILayout.IntField(comp.Damage.DamageDealt);

            GUI.enabled = true;
        }

    }
}
