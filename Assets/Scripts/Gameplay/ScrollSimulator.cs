using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;
using WASD.Runtime.Levels;
using WASD.Runtime.Managers;
using static WASD.Runtime.Levels.LevelInformation;
using static WASD.Runtime.Levels.ObstaclePathData;
using static WASD.Runtime.Gameplay.PlayerCollisionDetector;

namespace WASD.Runtime.Gameplay
{
    public class ScrollSimulator : MonoBehaviour
    {
        #region Properties

        public bool IsActive { get => _IsActive; set
            {
                _IsActive = value;
                _OnChangeActiveState.Invoke(arg0: _IsActive);
            }
        }

        #endregion
        
        #region Fields

        [Header("Editor")]
        [SerializeField] private bool _ForceLevelSelectedOnEditor;

        //[SerializeField] private bool _DecorationOrderIsRandom;
        [Header("Simulation")]
        [SerializeField] private int _MaxActiveDecorations = 2;
        [SerializeField] private float _HeightValue = 3f;
        [SerializeField] private float _PlatformSizeNormal = 1f;
        [SerializeField] private float _PlatformSizeShort = 0.5f;
        [SerializeField] private float _PlatformSizeLong = 2f;
        [SerializeField] private float _MaxSimulationVelocity;
        [SerializeField] private bool _IsActive;
        [SerializeField] private Material _LeftMaterial;
        [SerializeField] private Material _RightMaterial;

        [Header("Origins")]
        [SerializeField] private float _OriginsDisappearOffset;
        [SerializeField] private Transform _DecorationsOrigin;
        [SerializeField] private Transform _LeftPathOrigin;
        [SerializeField] private Transform _RightPathOrigin;

        [Header("Props Identifiers")]
        [SerializeField] private string _PlatformGroundIdentifier = "PlatformGround";
        [SerializeField] private string _PlatformAirIdentifier = "PlatformGround";
        [SerializeField] private string _DecorationsIdentifier = "Decoration";
        [SerializeField] private string _ObstacleJumpIdentifier = "ObstacleJump";
        [SerializeField] private string _ObstacleSlideIdentifier = "ObstacleSlide";
        [SerializeField] private string _ObstacleCubeIdentifier = "ObstacleCubes";
        [SerializeField] private string _EndPortalIdentifier = "EndPortal";

        private readonly List<SpawnableProp> _GroundPlatformList = new();
        private readonly List<SpawnableProp> _AirPlatformList = new();
        private readonly List<SpawnableProp> _DecorationsList = new();
        private readonly List<SpawnableProp> _ActiveProps = new();
        private int _ActiveDecorations;

        private readonly List<SpawnableProp> _ObstacleJumpList = new();
        private readonly List<SpawnableProp> _ObstacleSlideList = new();
        private readonly List<SpawnableProp> _ObstacleCubesList = new();
        private readonly List<SpawnableProp> _EndPortalList = new();

        private bool _IsPaused;
        private float _GlobalVelocity = 0;

        private LevelInformation _CurrentLevel;
        private LevelInformation _NextLevel;
        private int _LastLevelInformationPathData;
        /// <summary>
        /// 0: LeftHeight, 1: RightHeight, 2: InvertedColors
        /// </summary>
        private string[] _LevelPathDataFlags = new string[3];
        private int[] _PropListsCounts = new int[4];
        private bool ExecuteNextLevelPathStepSuccess;

        private SpawnableProp _LastLeftPlatform;
        private SpawnableProp _LastRightPlatform;
        private SpawnableProp _LastDecoration;
        #endregion

        #region Events
        [SerializeField] private UnityEvent _OnFinishPrepare;
        [SerializeField] private UnityEvent<bool> _OnChangeActiveState;
        #endregion

        #region MonoBehaviour

        private void Start()
        {
            DecorationsRefresher();
        }

        private void FixedUpdate()
        {
            Acceleration();
            MoveActiveProps();
        }

        private void Update()
        {
            DecorationsRefresher();
            TryExecuteNextLevelPathStep();
        }
        #endregion

        private void Acceleration()
        {
            if (!_IsActive)
            {
                _GlobalVelocity = 0;
            }
            else
            {
                _GlobalVelocity = _MaxSimulationVelocity;
            }
        }

        private void MoveActiveProps()
        {
            if (!_IsActive)
            {
                return;
            }

            Vector3 newPos;
            SpawnableProp prop;

            for (int i = 0; i < _ActiveProps.Count; i++)
            {
                prop = _ActiveProps[i];
                newPos = prop.transform.position;
                if(!_IsPaused) newPos.z -= _GlobalVelocity * Time.fixedDeltaTime;


                if (prop.EndingPoint.z < _DecorationsOrigin.position.z - _OriginsDisappearOffset)
                {
                    prop.Hide();
                    i--;
                }
                else
                {
                    prop.transform.position = newPos;
                }

            }
        }

        private void DecorationsRefresher()
        {
            if (!_IsActive)
            {
                return;
            }

            if (_ActiveDecorations < _MaxActiveDecorations && _DecorationsList.Count != 0)
            {
                SpawnableProp newDecoration = _DecorationsList[0];
                newDecoration.Show(position: _LastDecoration == null
                    ? _DecorationsOrigin.position
                    : _LastDecoration.EndingPoint);
                _LastDecoration = newDecoration;
            }
        }

        private void TryExecuteNextLevelPathStep()
        {
            if (!_IsActive)
            {
                return;
            }

            //Decoration

            if(_CurrentLevel == null || _CurrentLevel.Data == null || _CurrentLevel.Data.Length == 0)
            {
                return;
            }

            if(_LastLevelInformationPathData >= _CurrentLevel.Data.Length)
            {
                _LastLevelInformationPathData = 0;
            }

            ExecuteNextLevelPathStepSuccess = false;
            switch (_CurrentLevel.Data[_LastLevelInformationPathData].Type)
            {
                case Enums.LevelPathStep.Path:
                    ExecuteNextLevelPathStepSuccess = HandlePathLevelStep(pathData: _CurrentLevel.Data[_LastLevelInformationPathData]);
                    break;
                case Enums.LevelPathStep.ChangeHeight:
                    _LevelPathDataFlags[0] = $"{_CurrentLevel.Data[_LastLevelInformationPathData].SetLeftSideHeight}";
                    _LevelPathDataFlags[1] = $"{_CurrentLevel.Data[_LastLevelInformationPathData].SetRightSideHeight}";
                    ExecuteNextLevelPathStepSuccess = true;
                    break;
                case Enums.LevelPathStep.SwitchColors:
                    _LevelPathDataFlags[2] = _LevelPathDataFlags[2] == "true" ? "false" : "true";
                    ExecuteNextLevelPathStepSuccess = true;
                    break;
                case Enums.LevelPathStep.Decorations:
                    ExecuteNextLevelPathStepSuccess = true;
                    break;
            }

            if (ExecuteNextLevelPathStepSuccess)
            {
                _LastLevelInformationPathData++;
            }
        }

        private bool HandlePathLevelStep(LevelInformation.PathData pathData)
        {
            if((_LevelPathDataFlags[0] == "0" ? _GroundPlatformList.Count == 0 : _AirPlatformList.Count == 0) &&
                (_LevelPathDataFlags[1] == "0" ? _GroundPlatformList.Count == 0 : _AirPlatformList.Count == 0))
            {
                Debug.LogWarning("Not enough Platforms");
                return false;
            }

            _PropListsCounts[0] = _ObstacleJumpList.Count;
            _PropListsCounts[1] = _ObstacleSlideList.Count;
            _PropListsCounts[2] = _ObstacleCubesList.Count;
            _PropListsCounts[3] = _EndPortalList.Count;

            bool fValidateObstacleAvailability(Obstacle[] obstacles)
            {
                int id;
                foreach (Obstacle obstacle in obstacles)
                {
                     id =
                        obstacle.Type == Enums.LevelObstacleType.Jump ? 0 :
                        obstacle.Type == Enums.LevelObstacleType.Slide ? 1 :
                        obstacle.Type == Enums.LevelObstacleType.Cubes ? 2 :
                        obstacle.Type == Enums.LevelObstacleType.Ship ? -1 :
                        obstacle.Type == Enums.LevelObstacleType.EndPortal ? 3 :
                        -1;

                    if(id == -1)
                    {
                        continue;
                    }

                    _PropListsCounts[id]--;
                    if (_PropListsCounts[id] <= 0)
                    {
                        return false;
                    }
                }

                return true;
            }

            bool canSpawnLeftObstacles = fValidateObstacleAvailability(
                obstacles: pathData.ObstaclePath == null || pathData.UseCustomObstaclePath ?
                pathData.CustomLeftSide : pathData.ObstaclePath.LeftSide);
            bool canSpawnRightObstacles = fValidateObstacleAvailability(
                obstacles: pathData.ObstaclePath == null || pathData.UseCustomObstaclePath ?
                pathData.CustomRightSide : pathData.ObstaclePath.RightSide);

            if(!canSpawnLeftObstacles && !canSpawnRightObstacles)
            {
                Debug.LogWarning("Not enough Obstacles");
                return false;
            }

            ShowSpawnablePlatforms(size:
                pathData.Size == Enums.LevelPathSize.Short ? _PlatformSizeShort :
                pathData.Size == Enums.LevelPathSize.Normal ? _PlatformSizeNormal :
                pathData.Size == Enums.LevelPathSize.Long ? _PlatformSizeLong :
                pathData.Size == Enums.LevelPathSize.Custom ? pathData.PathCustomSize : _PlatformSizeNormal);

            ShowSpawnableObstacles(
                obstacles: pathData.ObstaclePath == null || pathData.UseCustomObstaclePath ?
                pathData.CustomLeftSide : pathData.ObstaclePath.LeftSide,
                platform: _LastLeftPlatform);
            ShowSpawnableObstacles(
                obstacles: pathData.ObstaclePath == null || pathData.UseCustomObstaclePath ?
                pathData.CustomRightSide : pathData.ObstaclePath.RightSide,
                platform: _LastRightPlatform);

            return true;
        }

        private void ShowSpawnablePlatforms(float size)
        {
            int heightLeft = int.Parse(s: _LevelPathDataFlags[0]);
            int heightRight = int.Parse(s: _LevelPathDataFlags[1]);

            SpawnableProp spawnedLeft = heightLeft == 0 ? _GroundPlatformList[0] : _AirPlatformList[0];
            SpawnableProp spawnedRight = heightRight== 0 ? _GroundPlatformList[0 + (heightLeft > 0 ? 0 : 1)] : _AirPlatformList[0 + (heightLeft == 0 ? 0 : 1)];

            bool invertPlatformColors = _LevelPathDataFlags[2] == "true";

            Vector3 leftPosition = _LastLeftPlatform != null ? _LastLeftPlatform.EndingPoint : _LeftPathOrigin.position;
            leftPosition.y = _LeftPathOrigin.position.y + (heightLeft * _HeightValue);
            Vector3 rightPosition = _LastRightPlatform != null ? _LastRightPlatform.EndingPoint : _RightPathOrigin.position;
            rightPosition.y = _RightPathOrigin.position.y + (heightRight * _HeightValue);

            spawnedLeft.SetPlayerCollisionConcept(concept: !invertPlatformColors ? CollisionConcept.BluePlatform : CollisionConcept.RedPlatform);
            spawnedLeft.Show(
                position: leftPosition,
                size: size,
                neonMaterial: !invertPlatformColors ? _LeftMaterial : _RightMaterial);
            _LastLeftPlatform = spawnedLeft;

            spawnedRight.SetPlayerCollisionConcept(concept: invertPlatformColors ? CollisionConcept.BluePlatform : CollisionConcept.RedPlatform);
            spawnedRight.Show(
                position: rightPosition,
                size: size,
                neonMaterial: invertPlatformColors ? _LeftMaterial : _RightMaterial);
            _LastRightPlatform = spawnedRight;
        }

        private void ShowSpawnableObstacles(Obstacle[] obstacles, SpawnableProp platform)
        {
            if(obstacles.Length == 0)
            {
                return;
            }

            bool isCountEven = obstacles.Length % 2 == 0;
            float inbetween = obstacles.Length == 1 || isCountEven ? platform.EndingPoint.z / (obstacles.Length + 1) : platform.EndingPoint.z / (obstacles.Length - 1);

            SpawnableProp obstacle;
            Vector3 position;

            for (int i = 0; i < obstacles.Length; i++)
            {
                obstacle = null;
                switch (obstacles[i].Type)
                {
                    case Enums.LevelObstacleType.Jump:
                        obstacle = _ObstacleJumpList[i];
                        break;
                    case Enums.LevelObstacleType.Slide:
                        obstacle = _ObstacleSlideList[0];
                        break;
                    case Enums.LevelObstacleType.Cubes:
                        obstacle = _ObstacleCubesList[0];
                        break;
                    case Enums.LevelObstacleType.Ship:
                        continue;
                    //break;
                    case Enums.LevelObstacleType.EndPortal:
                        obstacle = _EndPortalList[0];
                        break;
                }

                position = platform.transform.position;
                if (obstacles[i].AutomaticPosition)
                {
                    position.z += inbetween * (i + (isCountEven ? 1 : 0));
                }
                else
                {
                    position.z = platform.EndingPoint.z * obstacles[i].PositionOnPath;
                }
                
                obstacle.Show(
                    position: position,
                    neonMaterial: obstacle.Identifier == _EndPortalIdentifier ? null :
                        platform == _LastLeftPlatform ? _RightMaterial : _LeftMaterial);
            }
        }

        public void PrepareSimulation(List<SpawnableProp> spawnedProps)
        {
            _IsActive = false;
            _GroundPlatformList.Clear();
            _AirPlatformList.Clear();
            _DecorationsList.Clear();
            _ObstacleJumpList.Clear();
            _ObstacleSlideList.Clear();
            _ObstacleCubesList.Clear();
            _EndPortalList.Clear();

            void fAddPropToList(List<SpawnableProp> list, List<SpawnableProp> props)
            {
                foreach (SpawnableProp prop in props)
                {
                    prop.OnSetActive += (prop) =>
                    {
                        if (!prop.IgnoreSimulation)
                        {
                            _ActiveProps.Add(item: prop);
                            if(prop.Identifier == _DecorationsIdentifier)
                            {
                                _ActiveDecorations++;
                            }
                        }
                        list.Remove(item: prop);
                    };
                    prop.OnSetInactive += (prop) =>
                    {
                        if (!prop.IgnoreSimulation)
                        {
                            _ActiveProps.Remove(item: prop);
                            if (prop.Identifier == _DecorationsIdentifier)
                            {
                                _ActiveDecorations--;
                            }
                        }
                        list.Add(item: prop);
                    };

                    list.Add(item: prop);
                }
            }

            fAddPropToList(list: _GroundPlatformList, props: spawnedProps.Where(prop => prop.Identifier == _PlatformGroundIdentifier).ToList());
            fAddPropToList(list: _AirPlatformList, props: spawnedProps.Where(prop => prop.Identifier == _PlatformAirIdentifier).ToList());
            fAddPropToList(list: _DecorationsList, props: spawnedProps.Where(prop => prop.Identifier == _DecorationsIdentifier).ToList());
            fAddPropToList(list: _ObstacleJumpList, props: spawnedProps.Where(prop => prop.Identifier == _ObstacleJumpIdentifier).ToList());
            fAddPropToList(list: _ObstacleSlideList, props: spawnedProps.Where(prop => prop.Identifier == _ObstacleSlideIdentifier).ToList());
            fAddPropToList(list: _ObstacleCubesList, props: spawnedProps.Where(prop => prop.Identifier == _ObstacleCubeIdentifier).ToList());
            fAddPropToList(list: _EndPortalList, props: spawnedProps.Where(prop => prop.Identifier == _EndPortalIdentifier).ToList());

            _OnFinishPrepare.Invoke();
        }

        public void BeginSimulation(LevelInformation levelInfo) => BeginSimulation(levelInfo, null);
        public void BeginSimulation(LevelInformation levelInfo, LevelInformation nextLevelInfo)
        {
            _LevelPathDataFlags[0] = "0";
            _LevelPathDataFlags[1] = "0";
            _LevelPathDataFlags[2] = "false";
            if (Application.isEditor && _ForceLevelSelectedOnEditor)
            {
                _CurrentLevel = levelInfo;
            }
            else
            {
                _CurrentLevel = GameManager.LevelActive;
            }
            
            _NextLevel = nextLevelInfo;
            _IsActive = true;
        } 

        public void SetPauseValue(bool value)
        {
            _IsPaused = value;
        }
    }
}

