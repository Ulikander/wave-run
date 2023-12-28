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
        [SerializeField] private string _ObstacleShipIdentifier = "ObstacleShip";
        [SerializeField] private string _PowerUpInvinsibilityIdentifier = "PowerUpInvincibility";
        [SerializeField] private string _EndPortalIdentifier = "EndPortal";

        private readonly List<SpawnableProp> _GroundPlatformList = new();
        private readonly List<SpawnableProp> _AirPlatformList = new();
        private readonly List<SpawnableProp> _DecorationsList = new();
        private readonly List<SpawnableProp> _ActiveProps = new();
        private int _ActiveDecorations;

        private readonly List<SpawnableProp> _ObstacleJumpList = new();
        private readonly List<SpawnableProp> _ObstacleSlideList = new();
        private readonly List<SpawnableProp> _ObstacleCubesList = new();
        private readonly List<SpawnableProp> _ObstacleShipList = new();
        private readonly List<SpawnableProp> _EndPortalList = new();
        private readonly List<SpawnableProp> _PowerUpInvincibilityList = new();

        private bool _IsPaused;
        private float _GlobalVelocity = 0;

        private LevelInformation _CurrentLevel;
        private int _NextLevel;
        private int _LastLevelInformationPathData;
        /// <summary>
        /// 0: LeftHeight, 1: RightHeight, 2: InvertedColors
        /// </summary>
        private string[] _LevelPathDataFlags = new string[3];
        private int[] _PropListsCounts = new int[6];
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


                if (prop.EndingPoint.position.z < _DecorationsOrigin.position.z - _OriginsDisappearOffset)
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
#if UNITY_EDITOR
                newDecoration.SetGizmoValues(_LastLevelInformationPathData.ToString());
#endif
                newDecoration.Show(position: _LastDecoration == null
                    ? _DecorationsOrigin.position
                    : _LastDecoration.EndingPoint.position);
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

            if(_CurrentLevel == null || _CurrentLevel.Data == null || _CurrentLevel.Data.Count == 0)
            {
                return;
            }

            if(_LastLevelInformationPathData >= _CurrentLevel.Data.Count)
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

        private bool HandlePathLevelStep(PathData pathData)
        {
            Debug.Log(_LastLevelInformationPathData);
            int neededGroundPlatforms = (_LevelPathDataFlags[0] == "0" ? 1 : 0) + (_LevelPathDataFlags[1] == "0" ? 1 : 0);
            int neededAirPlatforms = 2 - neededGroundPlatforms;

            if(_GroundPlatformList.Count < neededGroundPlatforms || _AirPlatformList.Count < neededAirPlatforms)
            {
                //Debug.LogWarning("Not enough Platforms");
                return false;
            }

            _PropListsCounts[0] = _ObstacleJumpList.Count;
            _PropListsCounts[1] = _ObstacleSlideList.Count;
            _PropListsCounts[2] = _ObstacleCubesList.Count;
            _PropListsCounts[3] = _ObstacleShipList.Count;
            _PropListsCounts[4] = _EndPortalList.Count;
            _PropListsCounts[5] = _PowerUpInvincibilityList.Count;

            bool fValidateObstacleAvailability(List<Obstacle> obstacles)
            {
                int id;
                foreach (Obstacle obstacle in obstacles)
                {
                     id =
                        obstacle.Type == Enums.LevelObstacleType.Jump ? 0 :
                        obstacle.Type == Enums.LevelObstacleType.Slide ? 1 :
                        obstacle.Type == Enums.LevelObstacleType.Cubes ? 2 :
                        obstacle.Type == Enums.LevelObstacleType.Ship ? 3 :
                        obstacle.Type == Enums.LevelObstacleType.EndPortal ? 4 :
                        obstacle.Type == Enums.LevelObstacleType.Invincibility ? 5 :
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
                pathData.ObstaclePath == null || pathData.UseCustomObstaclePath
                    ? pathData.CustomLeftSide
                    : pathData.ObstaclePath.LeftSide,
                _LastLeftPlatform);
            
            ShowSpawnableObstacles(
                pathData.ObstaclePath == null || pathData.UseCustomObstaclePath
                    ? pathData.CustomRightSide
                    : pathData.ObstaclePath.RightSide,
                _LastRightPlatform);

            return true;
        }

        private void ShowSpawnablePlatforms(float size)
        {
            float heightLeft = float.Parse(s: _LevelPathDataFlags[0]);
            float heightRight = float.Parse(s: _LevelPathDataFlags[1]);

            SpawnableProp spawnedLeft = heightLeft == 0f ? _GroundPlatformList[0] : _AirPlatformList[0];
            SpawnableProp spawnedRight = heightRight == 0f ? _GroundPlatformList[heightLeft != 0f ? 0 : 1] : _AirPlatformList[heightLeft == 0f ? 0 : 1];

            bool invertPlatformColors = _LevelPathDataFlags[2] == "true";

            Vector3 leftPosition = _LastLeftPlatform != null ? _LastLeftPlatform.EndingPoint.position : _LeftPathOrigin.position;
            leftPosition.y = _LeftPathOrigin.position.y + (heightLeft * _HeightValue);
            Vector3 rightPosition = _LastRightPlatform != null ? _LastRightPlatform.EndingPoint.position : _RightPathOrigin.position;
            rightPosition.y = _RightPathOrigin.position.y + (heightRight * _HeightValue);

#if UNITY_EDITOR
            spawnedLeft.SetGizmoValues(_LastLevelInformationPathData.ToString());
#endif
            spawnedLeft.SetPlayerCollisionConcept(concept: !invertPlatformColors ? CollisionConcept.BluePlatform : CollisionConcept.RedPlatform);
            spawnedLeft.Show(
                position: leftPosition,
                size: size,
                neonMaterial: !invertPlatformColors ? _LeftMaterial : _RightMaterial);
            _LastLeftPlatform = spawnedLeft;

#if UNITY_EDITOR
            spawnedRight.SetGizmoValues(_LastLevelInformationPathData.ToString());
#endif
            spawnedRight.SetPlayerCollisionConcept(concept: invertPlatformColors ? CollisionConcept.BluePlatform : CollisionConcept.RedPlatform);
            spawnedRight.Show(
                position: rightPosition,
                size: size,
                neonMaterial: invertPlatformColors ? _LeftMaterial : _RightMaterial);
            _LastRightPlatform = spawnedRight;
        }

        private void ShowSpawnableObstacles(List<Obstacle> obstaclesToSpawn, SpawnableProp platform)
        {
            if(obstaclesToSpawn.Count == 0)
            {
                return;
            }

            bool isCountEven = obstaclesToSpawn.Count % 2 == 0;
            float inBetween = isCountEven
                ? platform.EndingPoint.localPosition.z / (obstaclesToSpawn.Count + 1)
                : platform.EndingPoint.localPosition.z / (obstaclesToSpawn.Count - 1);

            SpawnableProp obstacleFromInactiveList = null;

            for (int i = 0; i < obstaclesToSpawn.Count; i++)
            {
                switch (obstaclesToSpawn[i].Type)
                {
                    case Enums.LevelObstacleType.Jump:
                        obstacleFromInactiveList = _ObstacleJumpList[0];
                        break;
                    case Enums.LevelObstacleType.Slide:
                        obstacleFromInactiveList = _ObstacleSlideList[0];
                        break;
                    case Enums.LevelObstacleType.Cubes:
                        obstacleFromInactiveList = _ObstacleCubesList[0];
                        break;
                    case Enums.LevelObstacleType.Ship:
                        obstacleFromInactiveList = _ObstacleShipList[0];
                        break;
                    case Enums.LevelObstacleType.EndPortal:
                        obstacleFromInactiveList = _EndPortalList[0];
                        break;
                    case Enums.LevelObstacleType.Invincibility:
                        obstacleFromInactiveList = _PowerUpInvincibilityList[0];
                        break;
                }

                Vector3 position = platform.transform.position;
                if (obstaclesToSpawn[i].AutomaticPosition)
                {
                    if (obstaclesToSpawn.Count == 1)
                    {
                        position.z += (platform.EndingPoint.localPosition.z * platform.transform.localScale.z) / 2f;;
                    }
                    else
                    {
                        position.z += inBetween * (i + (isCountEven ? 1 : 0)) * platform.transform.localScale.z;
                    }
                }
                else
                {
                    position.z += platform.EndingPoint.localPosition.z * platform.transform.localScale.z *
                                  obstaclesToSpawn[i].PositionOnPath;
                }

#if UNITY_EDITOR
                obstacleFromInactiveList.SetGizmoValues(_LastLevelInformationPathData.ToString());
#endif
                
                obstacleFromInactiveList.Show(
                    position,
                    obstaclesToSpawn[i].OverrideSize == 0 ? 1f : obstaclesToSpawn[i].OverrideSize,
                    obstacleFromInactiveList.Identifier == _EndPortalIdentifier ? null :
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
            _ObstacleShipList.Clear();
            _EndPortalList.Clear();
            _PowerUpInvincibilityList.Clear();

            void fAddPropToList(List<SpawnableProp> list, List<SpawnableProp> props)
            {
                foreach (SpawnableProp selectedProp in props)
                {
                    selectedProp.OnSetActive += (prop) =>
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
                    selectedProp.OnSetInactive += (prop) =>
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

                    list.Add(item: selectedProp);
                }
            }

            fAddPropToList(list: _GroundPlatformList, props: spawnedProps.Where(prop => prop.Identifier == _PlatformGroundIdentifier).ToList());
            fAddPropToList(list: _AirPlatformList, props: spawnedProps.Where(prop => prop.Identifier == _PlatformAirIdentifier).ToList());
            fAddPropToList(list: _DecorationsList, props: spawnedProps.Where(prop => prop.Identifier == _DecorationsIdentifier).ToList());
            fAddPropToList(list: _ObstacleJumpList, props: spawnedProps.Where(prop => prop.Identifier == _ObstacleJumpIdentifier).ToList());
            fAddPropToList(list: _ObstacleSlideList, props: spawnedProps.Where(prop => prop.Identifier == _ObstacleSlideIdentifier).ToList());
            fAddPropToList(list: _ObstacleCubesList, props: spawnedProps.Where(prop => prop.Identifier == _ObstacleCubeIdentifier).ToList());
            fAddPropToList(list: _ObstacleShipList, props: spawnedProps.Where(prop => prop.Identifier == _ObstacleShipIdentifier).ToList());
            fAddPropToList(list: _EndPortalList, props: spawnedProps.Where(prop => prop.Identifier == _EndPortalIdentifier).ToList());
            fAddPropToList(list: _PowerUpInvincibilityList, props: spawnedProps.Where(prop => prop.Identifier == _PowerUpInvinsibilityIdentifier).ToList());

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
            else if (!GameManager.TryGetCoreLevel(GameManager.CurrentCoreLevel, out _CurrentLevel))
            {
                Debug.LogError("ScrollSimulator: Failed to select Current Core Level from GameManager.");
                return;
            }

            GameManager.Audio.PlayBgm(_CurrentLevel.Music, fadeOutTime: .2f);
            _NextLevel = GameManager.CurrentCoreLevel + 1;
            _IsActive = true;
        }

        public void ReloadToNextLevel()
        {
            if (GameManager.TryGetCoreLevel(_NextLevel, out LevelInformation levelInfo))
            {
                GameManager.CurrentCoreLevel = levelInfo.CoreLevelValue;
                GameManager.Scenes.LoadScene(ScenesManager.cSCENEID_GAMEPLAY);
            }
            else
            {
                GameManager.Scenes.LoadScene(ScenesManager.cSCENEID_MAINMENU);
            }
        }

        public void SetPauseValue(bool value)
        {
            _IsPaused = value;
        }
    }
}

