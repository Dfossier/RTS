﻿
using UnityEngine;
using RTSEngine.Entities;

namespace RTSEngine.Attack
{
    public interface IAttackTargetPositionGetter : IEntityPostInitializable
    {
        Vector3 GetAttackTargetPosition(IEntity source);
    }
}
