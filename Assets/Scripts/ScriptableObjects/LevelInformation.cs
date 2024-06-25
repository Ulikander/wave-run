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
        public class PathData
        {
            [Header("Size")]
            public LevelPathStep Type;
            [Space(height: 20f)]
            [Header("Obstacle")]
            public LevelPathSize Size;
            public float PathCustomSize;
            public ObstaclePathData ObstaclePath;
            public bool UseCustomObstaclePath;
            public List<Obstacle> CustomLeftSide;
            public List<Obstacle> CustomRightSide;
            public bool InvertObstacleValues;
            [Space(height: 20f)]
            [Header("Height")]
            public float SetLeftSideHeight;
            public float SetRightSideHeight;

            //No Decoration Options yet
            public PathData(){}

            public PathData Copy()
            {
                PathData copy = new PathData
                {
                    Type = Type,
                    Size = Size,
                    PathCustomSize = PathCustomSize,
                    ObstaclePath = ObstaclePath,
                    UseCustomObstaclePath = UseCustomObstaclePath,
                    CustomLeftSide = CustomLeftSide,
                    CustomRightSide = CustomRightSide,
                    InvertObstacleValues = InvertObstacleValues,
                    SetLeftSideHeight = SetLeftSideHeight,
                    SetRightSideHeight = SetRightSideHeight,
                };
                
                return copy;
            }
        }
        #endregion

        #region Fields
        public LevelDifficulty LevelDifficulty;
        public int CoreLevelValue;
        public AudioContainer Music;
        public List<PathData> Data;
        #endregion
        
        public void ClearUnusedValues()
        {
            void FClearCustomObstacle(List<Obstacle> list)
            {
                foreach (Obstacle obstacle in list)
                {
                    if (obstacle.AutomaticPosition) obstacle.PositionOnPath = default;
                }
            }
            
            if (LevelDifficulty != LevelDifficulty.Core) CoreLevelValue = default;
            foreach (PathData pathData in Data)
            {
                if (pathData.Type == LevelPathStep.Path)
                {
                    if (pathData.Size != LevelPathSize.Custom) pathData.PathCustomSize = default;
                    switch (pathData.UseCustomObstaclePath)
                    {
                        case true:
                            pathData.ObstaclePath = null;
                            pathData.InvertObstacleValues = default;
                            FClearCustomObstacle(pathData.CustomLeftSide);
                            FClearCustomObstacle(pathData.CustomRightSide);
                            break;
                        case false:
                            pathData.CustomLeftSide?.Clear();
                            pathData.CustomRightSide?.Clear();
                            break;
                    }
                }
                else
                {
                    pathData.Size = default;
                    pathData.PathCustomSize = default;
                    pathData.ObstaclePath = null;
                    pathData.InvertObstacleValues = default;
                    pathData.CustomLeftSide?.Clear();
                    pathData.CustomRightSide?.Clear();
                }

                if (pathData.Type != LevelPathStep.ChangeHeight)
                {
                    pathData.SetLeftSideHeight = default;
                    pathData.SetRightSideHeight = default;
                }
            }
        }
    }
}

