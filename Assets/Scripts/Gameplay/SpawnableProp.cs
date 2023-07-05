using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

using static WASD.Runtime.Gameplay.PlayerCollisionDetector;

namespace WASD.Runtime.Gameplay
{
    public class SpawnableProp : MonoBehaviour
    {
        #region Properties
        public string Identifier { get => _Identifier; }
        public bool IgnoreSimulation { get => _IgnoreSimulation; }
        public bool IsActive { get; private set; }
        public Vector3 EndingPoint { get => _EndingPoint != null ? _EndingPoint.position : transform.position; }

        #endregion

        #region Fields
        [Header("Prop")]
        [SerializeField] private string _Identifier;
        [SerializeField] private bool _IgnoreSimulation;
        [SerializeField] private Transform _EndingPoint;
        [SerializeField] private Material _DefaultNeonMaterial;
        [SerializeField] private Renderer[] _NeonAffectedRenderer;
        [SerializeField] private int[] _NeonAffectedRendererIndex;

        [Header("Collision")]
        [SerializeField] private PlayerCollisionDetector _PlayerCollision;

        private Renderer[] _AllRenderers;
        private Collider[] _AllColliders;
        #endregion

        #region Events
        public event Action<SpawnableProp> OnSetActive;
        public event Action<SpawnableProp> OnSetInactive;
        #endregion

        private void Awake()
        {
            _AllRenderers = GetComponentsInChildren<Renderer>(includeInactive: true);
            _AllColliders = GetComponentsInChildren<Collider>(includeInactive: true);

            Hide();
        }

        public void Hide()
        {
            SetRenderersAndCollidersEnabled(value: false);
        }

        public void Show(Vector3 position, float size = 1f, Material neonMaterial = null)
        {
            if(neonMaterial != null)
            {
                Utils.ChangeAllMeshRenderersMaterial(
                    renderers: _NeonAffectedRenderer,
                    indexes: _NeonAffectedRendererIndex,
                    material: neonMaterial);
            }
            else if(_DefaultNeonMaterial != null)
            {
                Utils.ChangeAllMeshRenderersMaterial(
                    renderers: _NeonAffectedRenderer,
                    indexes: _NeonAffectedRendererIndex,
                    material: _DefaultNeonMaterial);
            }
            
            Transform gameObjectTransform = gameObject.transform;
            gameObjectTransform.position = position;

            Vector3 newSize = gameObjectTransform.localScale;
            newSize.z = 1 * size;
            gameObjectTransform.localScale = newSize;

            SetRenderersAndCollidersEnabled(value: true);
        }

        public void SetPlayerCollisionConcept(CollisionConcept concept)
        {
            if(_PlayerCollision != null)
            {
                _PlayerCollision.Concept = concept;
            }
        }

        private void SetRenderersAndCollidersEnabled(bool value)
        {
            foreach (Renderer renderer in _AllRenderers)
            {
                renderer.enabled = value;
            }

            foreach (Collider collider in _AllColliders)
            {
                collider.enabled = value;
            }

            IsActive = value;

            if (IsActive)
            {
                OnSetActive?.Invoke(obj: this);
            }
            else
            {
                OnSetInactive?.Invoke(obj: this);
            }
        }
    }
}

