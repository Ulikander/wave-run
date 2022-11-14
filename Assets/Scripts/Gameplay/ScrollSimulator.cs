using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;
using WASD.Runtime.Levels;

namespace WASD.Runtime.Gameplay
{
    public class ScrollSimulator : MonoBehaviour
    {
        #region Fields
        public bool IsActive { get => _IsActive; set => _IsActive = value; }

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

        private readonly List<SpawnableProp> _ObstacleJumpList = new();
        private readonly List<SpawnableProp> _ObstacleSlideList = new();
        private readonly List<SpawnableProp> _ObstacleCubesList = new();
        private readonly List<SpawnableProp> _EndPortalList = new();

        private float _GlobalVelocity = 0;

        private LevelInformation _CurrentLevel;
        private LevelInformation _NextLevel;
        private int _LastLevelInformationPathData;
        /// <summary>
        /// 0: LeftHeight, 1: RightHeight, 2: InvertedColors
        /// </summary>
        private string[] _LevelPathDataFlags = new string[3];

        private SpawnableProp _LastLeftPlatform;
        private SpawnableProp _LastRightPlatform;
        private SpawnableProp _LastDecoration;
        #endregion

        #region Events
        [SerializeField] private UnityEvent _OnFinishPrepare;
        #endregion

        #region MonoBehaviour
        private void OnEnable()
        {
            PlayerCollisionDetector.OnTriggerEnterEvent += PlayerCollisionConceptHandler;
            PlayerCollisionDetector.OnCollisionEnterEvent += PlayerCollisionConceptHandler;
        }

        private void OnDisable()
        {
            PlayerCollisionDetector.OnTriggerEnterEvent -= PlayerCollisionConceptHandler;
            PlayerCollisionDetector.OnCollisionEnterEvent -= PlayerCollisionConceptHandler;
        }

        private void FixedUpdate()
        {
            Acceleration();
            MoveActiveProps();
        }

        private void Update()
        {
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
            for (int i = 0; i < _ActiveProps.Count; i++)
            {
                SpawnableProp prop = _ActiveProps[i];
                newPos = prop.transform.position;
                newPos.z -= _GlobalVelocity * Time.fixedDeltaTime;

                if (prop.EndingPoint.z < _DecorationsOrigin.position.z)
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

        private void TryExecuteNextLevelPathStep()
        {
            if (!_IsActive)
            {
                return;
            }

            //Decoration


            if (_CurrentLevel.Data != null && _CurrentLevel.Data == null && _LastLevelInformationPathData >= _CurrentLevel.Data.Length)
            {
                _CurrentLevel = _NextLevel;
                _LastLevelInformationPathData = 0;
            }

            if(_CurrentLevel == null || _CurrentLevel.Data == null || _CurrentLevel.Data.Length == 0)
            {
                return;
            }

            bool success = false;

            LevelInformation.PathData pathData = _CurrentLevel.Data[_LastLevelInformationPathData];

            switch (pathData.Type)
            {
                case Enums.LevelPathStep.Path:
                    success = HandlePathLevelStep(pathData: pathData);
                    break;
                case Enums.LevelPathStep.ChangeHeight:
                    _LevelPathDataFlags[0] = $"{pathData.SetLeftSideHeight}";
                    _LevelPathDataFlags[1] = $"{pathData.SetRightSideHeight}";
                    success = true;
                    break;
                case Enums.LevelPathStep.SwitchColors:
                    _LevelPathDataFlags[2] = _LevelPathDataFlags[2] == "true" ? "false" : "true";
                    success = true;
                    break;
                case Enums.LevelPathStep.Decorations:
                    success = true;
                    break;
            }

            if (success)
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

            int[] listsCounts = new int[4];
            listsCounts[0] = _ObstacleJumpList.Count;
            listsCounts[1] = _ObstacleSlideList.Count;
            listsCounts[2] = _ObstacleCubesList.Count;
            listsCounts[3] = _EndPortalList.Count;

            List<Enums.LevelObstacleType> requiredObstacles = new();
            bool fValidateObstacleAvailability(ObstaclePathData.Obstacle[] obstacles)
            {
                foreach (ObstaclePathData.Obstacle obstacle in obstacles)
                {
                    int id =
                        obstacle.Type == Enums.LevelObstacleType.Jump ? 0 :
                        obstacle.Type == Enums.LevelObstacleType.Slide ? 1 :
                        obstacle.Type == Enums.LevelObstacleType.Cubes ? 2 :
                        obstacle.Type == Enums.LevelObstacleType.EndPortal ? 3 :
                        0;

                    listsCounts[id]--;
                    if (listsCounts[id] <= 0)
                    {
                        return false;
                    }
                    requiredObstacles.Add(item: obstacle.Type);
                }

                return true;
            }

            bool canSpawnLeftObstacles = fValidateObstacleAvailability(obstacles: pathData.ObstaclePath == null || pathData.UseCustomObstaclePath ?
                    pathData.CustomLeftSide : pathData.ObstaclePath.LeftSide);

            bool canSpawnRightObstacles = fValidateObstacleAvailability(obstacles: pathData.ObstaclePath == null || pathData.UseCustomObstaclePath ?
               pathData.CustomRightSide : pathData.ObstaclePath.RightSide);

            if(!canSpawnLeftObstacles && !canSpawnRightObstacles)
            {
                Debug.LogWarning("Not enough Obstacles");
                return false;
            }

            float fGetSize(Enums.LevelPathSize enumSize)
            {
                return enumSize switch
                {
                    Enums.LevelPathSize.Short => _PlatformSizeShort,
                    Enums.LevelPathSize.Normal => _PlatformSizeNormal,
                    Enums.LevelPathSize.Long => _PlatformSizeLong,
                    Enums.LevelPathSize.Custom => pathData.PathCustomSize,
                    _ => 1,
                };
            }

            ShowSpawnablePlatforms(size: fGetSize(pathData.Size));
            ShowSpawnableObstacles(types: requiredObstacles);

            return true;
        }

        private void ShowSpawnablePlatforms(float size)
        {
            int heightLeft = int.Parse(s: _LevelPathDataFlags[0]);
            int heightRight = int.Parse(s: _LevelPathDataFlags[1]);

            SpawnableProp spawnedLeft = heightLeft == 0 ? _GroundPlatformList[0] : _AirPlatformList[0];
            SpawnableProp spawnedRight = heightRight== 0 ? _GroundPlatformList[0 + (heightLeft > 0 ? 0 : 1)] : _AirPlatformList[0 + (heightLeft == 0 ? 0 : 1)];

            bool invertPlatformColors = _LevelPathDataFlags[2] == "true";

            float heightLeftValue = _LeftPathOrigin.position.y + (heightLeft * _HeightValue);
            float heightRightValue = _RightPathOrigin.position.y + (heightRight * _HeightValue);

            spawnedLeft.SetPlayerCollisionConcept(concept: !invertPlatformColors ? "LeftPlatform" : "RightPlatform");
            spawnedLeft.Show(
                position: _LastLeftPlatform != null ? _LastLeftPlatform.EndingPoint : _LeftPathOrigin.position,
                height: heightLeftValue,
                size: size,
                neonMaterial: !invertPlatformColors ? _LeftMaterial : _RightMaterial);
            _LastLeftPlatform = spawnedLeft;

            spawnedRight.SetPlayerCollisionConcept(concept: invertPlatformColors ? "LeftPlatform" : "RightPlatform");
            spawnedRight.Show(
                position: _LastRightPlatform != null ? _LastRightPlatform.EndingPoint : _RightPathOrigin.position,
                height: heightRightValue,
                size: size,
                neonMaterial: invertPlatformColors ? _LeftMaterial : _RightMaterial);
            _LastRightPlatform = spawnedRight;
        }

        private void ShowSpawnableObstacles(List<Enums.LevelObstacleType> types)
        {
            foreach(Enums.LevelObstacleType type in types)
            {
                switch (type)
                {
                    case Enums.LevelObstacleType.Jump:
                        break;
                    case Enums.LevelObstacleType.Slide:
                        break;
                    case Enums.LevelObstacleType.Cubes:
                        break;
                    case Enums.LevelObstacleType.Ship:
                        break;
                    case Enums.LevelObstacleType.EndPortal:
                        break;
                }
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
                        }
                        list.Remove(item: prop);
                    };
                    prop.OnSetInactive += (prop) =>
                    {
                        if (!prop.IgnoreSimulation)
                        {
                            _ActiveProps.Remove(item: prop);
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
        public void BeginSimulation(LevelInformation levelInfo, LevelInformation nextLevelInfo = null)
        {
            _LevelPathDataFlags[0] = "0";
            _LevelPathDataFlags[1] = "0";
            _LevelPathDataFlags[2] = "false";
            _CurrentLevel = levelInfo;
            _NextLevel = nextLevelInfo;
            _IsActive = true;
        }

        public void PlayerCollisionConceptHandler(GameObject obj, string concept)
        {

        }
    }
}

