using System;
using System.Collections.Generic;
using UnityEngine;
using WASD.Enums;

namespace WASD.Runtime.Levels
{
    [CreateAssetMenu(fileName = "ObstacleData_", menuName = "WASD/Create Obstacle Data")]
    [Serializable]
    public class ObstaclePathData : ScriptableObject
    {
        #region Types
        [Serializable]
        public struct Obstacle
        {
            public LevelObstacleType Type;
            public bool AutomaticPosition;
            public float PositionOnPath;
            public float OverrideSize;
        }
        #endregion

        #region Fields
        public List<Obstacle> LeftSide;
        public List<Obstacle> RightSide;
        #endregion
    }
}

