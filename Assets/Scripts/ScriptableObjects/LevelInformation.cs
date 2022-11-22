using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using WASD.Enums;
using WASD.Runtime.Audio;
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
            [Header("Size")]
            public LevelPathStep Type;
            [Space(height: 20f)]
            [Header("Obstacle")]
            public LevelPathSize Size;
            public float PathCustomSize;
            public ObstaclePathData ObstaclePath;
            public bool UseCustomObstaclePath;
            public Obstacle[] CustomLeftSide;
            public Obstacle[] CustomRightSide;
            public bool InvertObstacleValues;
            [Space(height: 20f)]
            [Header("Height")]
            public int SetLeftSideHeight;
            public int SetRightSideHeight;

            //No Decoration Options yet
        }
        #endregion

        #region Fields
        public LevelDifficulty LevelDifficulty;
        public int CoreLevelValue;
        public AudioContainer Music;
        public PathData[] Data;
        #endregion

        public void ClearUnusedValues()
        {

        }
    }
}

