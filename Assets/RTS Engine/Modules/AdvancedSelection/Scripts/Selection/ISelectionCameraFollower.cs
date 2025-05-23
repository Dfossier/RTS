﻿using RTSEngine.Game;

namespace RTSEngine.Selection
{
    public interface ISelectionCameraFollower : IPreRunGameService
    {
        void FollowNextEntity();
        void ResetTarget();
    }
}