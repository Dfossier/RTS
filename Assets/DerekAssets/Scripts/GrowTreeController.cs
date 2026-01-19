using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using RTSEngine.Game;
using RTSEngine.Event;
using RTSEngine.Entities;
using RTSEngine.EntityComponent;

public class GrowTreeController : MonoBehaviour, IEntityPreInitializable
{
    protected IGameManager GameMgr {  get; private set; }

    protected IEntity Owner { get; private set; }

    [SerializeField] Transform _parentTR = null;



    [SerializeField] bool _startsFullyGrown = true;
    [SerializeField] float growSpeed = 0.0019f;
    [SerializeField] float _maxRegrowTime = 60;

    public void OnEntityPreInit(IGameManager gameMgr, IEntity owner)
    {
        GameMgr = gameMgr;
        Owner = owner;
    }
    public void OnPrefabInit(IEntity owner, IGameManager gameMgr)
    {

    }
    public void Update()
    {
        if (Owner != null)
        {
            if (_startsFullyGrown)
                Owner.Model.transform.localScale = Vector3.one;
            if (Owner.Model.transform.localScale == Vector3.zero || Owner.Model.transform.localScale.x < 1)
            {
                Owner.Model.transform.localScale = Vector3.Lerp(Vector3.zero, Vector3.one, _maxRegrowTime);
            }
        }

    }

    public void Disable()
    {

    }
}
