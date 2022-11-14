using System;
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
        }
        #endregion

        #region Fields
        public Obstacle[] LeftSide;
        public Obstacle[] RightSide;
        #endregion
    }
}

