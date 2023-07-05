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

        private List<SpawnableProp> _SpawnedProps;

        #region Events
        [SerializeField] private UnityEvent<List<SpawnableProp>> _OnFinishSpawns;
        #endregion

        private void Start()
        {
            StartSpawning();
        }

        public void StartSpawning()
        {
            _SpawnedProps = new(capacity:
                (_MinimumPlatformCount * 2) +
                (_MinimumDecorationCount * _DecorationPrefabs.Length) +
                (_MinimumObstacleCount * _ObstaclePrefabs.Length));
            
            Transform platformContainer = new GameObject(name: "Platforms").transform;
            SpawnProp(
                container: platformContainer,
                original: _GroundPlatformPrefab,
                amount: _MinimumPlatformCount);

            SpawnProp(
                container: platformContainer,
                original: _AirPlatformPrefab,
                amount: _MinimumPlatformCount);

            foreach(SpawnableProp decoration in _DecorationPrefabs)
            {
                SpawnProp(
                    container: new GameObject(name: "Decorations").transform,
                    original: decoration,
                    amount: _MinimumDecorationCount);
            }

            Transform obstacleContainer = new GameObject(name: "Obstacles").transform;
            foreach (SpawnableProp obstacle in _ObstaclePrefabs)
            {
                SpawnProp(
                    container: obstacleContainer,
                    original: obstacle,
                    amount: _MinimumObstacleCount);
            }

            SpawnProp(
                container: new GameObject(name: "EndPortals").transform,
                original: _EndPortalPrefab,
                amount: 2);

            foreach (SpawnableProp prop in _SpawnedProps)
            {
                prop.Hide();
            }

            _OnFinishSpawns.Invoke(arg0: _SpawnedProps);
        }
        
        void SpawnProp(Transform container, SpawnableProp original, int amount)
        {
            for (int i = 0; i < amount; i++)
            {
                _SpawnedProps.Add(item: Instantiate(original: original, parent: container)); 
            }
        }
    }
}

