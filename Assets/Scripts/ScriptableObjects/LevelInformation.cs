using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WASD.Enums;
using static WASD.Runtime.Levels.ObstaclePathData;

namespace WASD.Runtime.Levels
{
    [CreateAssetMenu(fileName = "Level", menuName = "WASD/Create Level Information Asset")]
    public class LevelInformation : ScriptableObject
    {
        #region Types
        [Serializable]
        public struct PathData
        {
            public LevelPathStep Type;
            public LevelPathSize Size;
            public float PathCustomSize;
            public bool UseCustomObstaclePath;
            public ObstaclePathData ObstaclePath;
            public Obstacle[] CustomLeftSide;
            public Obstacle[] CustomRightSide;

            public bool InvertPathValues;
            public int SetLeftSideHeight;
            public int SetRightSideHeight;
            public bool SwitchPathColors;

            //No Decoration Options yet
        }
        #endregion

        #region Fields
        public LevelDifficulty LevelDifficulty;
        public int CoreLevelValue;
        public PathData[] Data;
        #endregion

        public void ClearUnusedValues()
        {

        }
    }
}

