using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using WASD.Runtime.Levels;

namespace WASD.Runtime.Gameplay
{
    public class SpawnController : MonoBehaviour
    {
        #region Fields
        [Header("Ground")]
        [SerializeField] private SpawnableProp _GroundPlatformPrefab;
        [SerializeField] private SpawnableProp _AirPlatformPrefab;
        [SerializeField] private int _MinimumPlatformCount;
        [Header("Decorations")]
        [SerializeField] private SpawnableProp[] _DecorationPrefabs;
        [SerializeField] private int _MinimumDecorationCount;
        [Header("Obstacles")]
        [SerializeField] private SpawnableProp[] _ObstaclePrefabs;
        [SerializeField] private int _MinimumObstacleCount;
        [SerializeField] private SpawnableProp _EndPortalPrefab;
        #endregion

        #region Events
        [SerializeField] private UnityEvent<List<SpawnableProp>> _OnFinishSpawns;
        #endregion

        private void Start()
        {
            StartSpawning();
        }

        public void StartSpawning()
        {
            List<SpawnableProp> spawnedProps = new(capacity:
                (_MinimumPlatformCount * 2) +
                (_MinimumDecorationCount * _DecorationPrefabs.Length) +
                (_MinimumObstacleCount * _ObstaclePrefabs.Length));

            void fSpawnProp(Transform container, SpawnableProp original, int amount)
            {
                for (int i = 0; i < amount; i++)
                {
                    spawnedProps.Add(item: Instantiate(original: original, parent: container)); 
                }
            }

            Transform platformContainer = new GameObject(name: "Platforms").transform;
            fSpawnProp(
                container: platformContainer,
                original: _GroundPlatformPrefab,
                amount: _MinimumPlatformCount);

            fSpawnProp(
                container: platformContainer,
                original: _AirPlatformPrefab,
                amount: _MinimumPlatformCount);

            foreach(SpawnableProp decoration in _DecorationPrefabs)
            {
                fSpawnProp(
                    container: new GameObject(name: "Decorations").transform,
                    original: decoration,
                    amount: _MinimumDecorationCount);
            }

            Transform obstacleContainer = new GameObject(name: "Obstacles").transform;
            foreach (SpawnableProp obstacle in _ObstaclePrefabs)
            {
                fSpawnProp(
                    container: obstacleContainer,
                    original: obstacle,
                    amount: _MinimumDecorationCount);
            }

            fSpawnProp(
                container: new GameObject(name: "EndPortals").transform,
                original: _EndPortalPrefab,
                amount: 2);

            foreach (SpawnableProp prop in spawnedProps)
            {
                prop.Hide();
            }

            _OnFinishSpawns.Invoke(arg0: spawnedProps);
        }
    }
}

