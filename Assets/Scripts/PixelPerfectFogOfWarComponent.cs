using System;
using System.Collections.Generic;
using Assets._GAME.Scripts.Components;
/// Component created by Marius van den Oever for RTS Engine 2023.0.1 and Pixel Perfect Fog Of war 1.6.5
/// Component version 1.2 (2023-08-15)
/// Engine 2023.0.1 - https://assetstore.unity.com/packages/tools/game-toolkits/rts-engine-2023-79732
/// Pixel Perfect Fog Of war 1.6.5 https://assetstore.unity.com/packages/vfx/shaders/fullscreen-camera-effects/pixel-perfect-fog-of-war-229484
/// Instructions
///   Add this component to your entity. It will add the required components automatically.
///   Make sure to configure your hider and revealer manually 
///   If you already have a hider or revealer on your unit or buildig, make sure to either remove them OR add this component on the same component
using FOW;
using RTSEngine;
using RTSEngine.Entities;
using RTSEngine.EntityComponent;
using RTSEngine.Event;
using UnityEngine;

[RequireComponent(typeof(FogOfWarRevealer3D))]
[RequireComponent(typeof(FogOfWarHider))]
public class PixelPerfectFogOfWarComponent
    : EntityComponentBase
{
    FogOfWarRevealer _revealer;
    FogOfWarHider _hider;
    IGlobalEventPublisher _globalEventPublisher;
    [SerializeField, Tooltip("Use the default RTS Engine Model Property or override to use a list of Mesh Renderers")]
    private bool OverrideModel = false;
    [SerializeField, Tooltip("Add Mesh or Skinned Mesh Renderers you want to make invisible when hidden in fog")]
    private List<Renderer> Renderers = new();
    [SerializeField, Tooltip("Add Mesh or Skinned Mesh Renderers you want to make invisible when hidden in fog")]
    public List<GameObject> ObjectsToDisable = new();

    [SerializeField, Tooltip("Do you want to completely hide this entity if it's in the fog and not visible? If false, it will be shown according to your Fog of War world setttings.")]
    private bool _hideInFog = true;
    public override bool AllowPreEntityInit => true;
    public bool HideInFog => _hideInFog;
    public bool IsCurrentlyHidden = false;

    protected override void OnInit()
    {
        _revealer = GetComponent<FogOfWarRevealer>();
        _hider = GetComponent<FogOfWarHider>();
        _revealer.enabled = false;
        if (_hider != null)
        {
            _hider.enabled = false;
            _hider.OnActiveChanged += _hider_OnActiveChanged;
        }



        _globalEventPublisher = gameMgr.GetService<IGlobalEventPublisher>();
        Entity.EntityInitiated += Entity_EntityInitiated;
        Entity.FactionUpdateComplete += Entity_FactionUpdateComplete;



        // Building specific events
        if (Entity is Building building)
        {
            building.BuildingBuilt += Building_BuildingBuilt;
        }
        if (Entity is Resource resource)
        {
            resource.WorkerMgr.WorkerAdded += Resource_WorkersUpdated;
            resource.WorkerMgr.WorkerRemoved += Resource_WorkersUpdated;
        }
        _globalEventPublisher.RaiseEntityVisibilityUpdateGlobal(Entity, new VisibilityEventArgs(false));
        base.OnInit();
    }

    private void Building_BuildingBuilt(IBuilding sender, EventArgs args)
    {
        _reEvaluateVisibility();
    }
    public void Resource_WorkersUpdated(IEntity collector, EventArgs eventArgs)
    {
        if (Entity.WorkerMgr.Workers.Count > 0)
        {
            SetHideInFog(false);
        }
        else
        {
            SetHideInFog(true);
        }
    }
    public void SetHideInFog(bool set)
    {
        if (set)
        {
            _hideInFog = true;
        }
        else
        {
            _hideInFog = false;
        }
    }
    private void LateUpdate()
    {
        if (Input.GetKeyDown(KeyCode.F1))
        {
            _reEvaluateVisibility();
        }
    }
    private void OnDestroy()
    {
        if (Entity == null) { return; }

        Entity.EntityInitiated -= Entity_EntityInitiated;
        Entity.FactionUpdateComplete -= Entity_FactionUpdateComplete;
        if (Entity is Building building)
        {
            building.BuildingBuilt -= Building_BuildingBuilt;
        }
        if (Entity is Resource resource)
        {
            resource.WorkerMgr.WorkerAdded -= Resource_WorkersUpdated;
            resource.WorkerMgr.WorkerRemoved -= Resource_WorkersUpdated;
        }
    }

    #region Event listener implementation
    private void Entity_EntityInitiated(IEntity sender, EventArgs args)
    {
        _reEvaluateVisibility();
    }

    private void Entity_FactionUpdateComplete(IEntity sender, RTSEngine.Event.FactionUpdateArgs args)
    {
        _reEvaluateVisibility();
    }
    #endregion

    /// <summary>
    /// This method will do the actual check and set the components active/ inactive based on the settings
    /// </summary>
    private void _reEvaluateVisibility()
    {
        if (Entity is Building building)
        {
            if (building.IsPlacementInstance)
            {
                _globalEventPublisher.RaiseEntityVisibilityUpdateGlobal(Entity, new VisibilityEventArgs(false));
                return;
            }
        }
        // Default behaviour : show for local player and hide if it's not the local player
        if (Entity.IsLocalPlayerFaction() || PixelPerfectFogOfWarWorldComponent.ShowAllEntities)
        {
            this.IsCurrentlyHidden = false;
            _revealer.enabled = true;
            if (_hider != null)
            {
                _hider.enabled = false;
            }

        }
        else
        {
            this.IsCurrentlyHidden = true;
            _revealer.enabled = false;
            if (_hideInFog)
            {
                if (_hider != null)
                {
                    // Simple trick :)
                    _hider.enabled = false;
                    _hider.enabled = true;
                }
            }
        }
    }

    /// <summary>
    /// This method currently disables the full model, which is not -always- what you want
    /// Will have to iterate on this later
    /// </summary>
    /// <param name="isActive"></param>
    private void _hider_OnActiveChanged(bool isActive)
    {
        // Should not be visible..
        this.IsCurrentlyHidden = isActive;
        if (OverrideModel)
        {
            if (Renderers.Count > 0)
            {
                foreach (Renderer renderer in Renderers)
                    if (renderer != null)
                        renderer.enabled = isActive;
            }
            if(ObjectsToDisable.Count > 0)
            {
                foreach(GameObject obj in ObjectsToDisable)
                    if (obj != null)
                        obj.SetActive(isActive);
            }
        }
        else
        {
            Entity.Model.SetActive(isActive);
        }
        _globalEventPublisher.RaiseEntityVisibilityUpdateGlobal(Entity, new VisibilityEventArgs(isActive));
    }
}