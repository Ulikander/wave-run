using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
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
        public Transform EndingPoint { get => _EndingPoint != null ? _EndingPoint : transform; }
        #endregion

        #region Fields
        [Header("Prop")]
        [SerializeField] private string _Identifier;
        [SerializeField] private bool _IgnoreSimulation;
        [SerializeField] private bool _sizeOverrideAllAxis;
        [SerializeField] private Transform _EndingPoint;
        [SerializeField] private Material _DefaultNeonMaterial;
        [SerializeField] private Material _DefaultLeftMaterial;
        [SerializeField] private Material _DefaultRightMaterial;
        [SerializeField] private Renderer[] _NeonAffectedRenderer;
        [SerializeField] private int[] _NeonAffectedRendererIndex;

        [Header("Collision")]
        [SerializeField] private PlayerCollisionDetector _PlayerCollision;

        private Renderer[] _AllRenderers;
        private Collider[] _AllColliders;
        private bool _IsDefaultLeftMaterialNotNull;
        private bool _IsDefaultRightMaterialNotNull;
        private bool _IsDefaultNeonMaterialNotNull;
        private bool _IsPlayerCollisionNotNull;
        #endregion

        #region Events
        public event Action<SpawnableProp> OnSetActive;
        public event Action<SpawnableProp> OnSetInactive;
        #endregion

        private void Start()
        {
            _IsDefaultNeonMaterialNotNull = _DefaultNeonMaterial != null;
        }

        private void Awake()
        {
            _IsPlayerCollisionNotNull = _PlayerCollision != null;
            _IsDefaultRightMaterialNotNull = _DefaultRightMaterial != null;
            _IsDefaultLeftMaterialNotNull = _DefaultLeftMaterial != null;
            _AllRenderers = GetComponentsInChildren<Renderer>(includeInactive: true);
            _AllColliders = GetComponentsInChildren<Collider>(includeInactive: true);

            Hide();
        }

        public void Hide()
        {
            SetRenderersAndCollidersEnabled(value: false);
        }

        public void Show(Vector3 position, float overrideSize = 1f, Material neonMaterial = default, bool useLeftMaterial = false)
        {
            if (neonMaterial == default)
            {
                if (_IsDefaultLeftMaterialNotNull && _IsDefaultRightMaterialNotNull)
                {
                    Utils.ChangeAllMeshRenderersMaterial(
                        _NeonAffectedRenderer,
                        _NeonAffectedRendererIndex,
                        useLeftMaterial ? _DefaultLeftMaterial : _DefaultRightMaterial);
                }
                else if (_IsDefaultNeonMaterialNotNull)
                {
                    Utils.ChangeAllMeshRenderersMaterial(
                        renderers: _NeonAffectedRenderer,
                        indexes: _NeonAffectedRendererIndex,
                        material: _DefaultNeonMaterial);
                }
            }
            else
            {
                Utils.ChangeAllMeshRenderersMaterial(
                    renderers: _NeonAffectedRenderer,
                    indexes: _NeonAffectedRendererIndex,
                    material: neonMaterial);
            }

            Transform gameObjectTransform = gameObject.transform;
            gameObjectTransform.position = position;

            Vector3 newSize = gameObjectTransform.localScale;
            if (_sizeOverrideAllAxis)
            {
                newSize.x = 1 * overrideSize;
                newSize.y = 1 * overrideSize;
            }
            newSize.z = 1 * overrideSize;
            gameObjectTransform.localScale = newSize;

            SetRenderersAndCollidersEnabled(value: true);
        }

        public void SetPlayerCollisionConcept(CollisionConcept concept)
        {
            if(_IsPlayerCollisionNotNull)
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

#if UNITY_EDITOR
        private string _PathElementValue;

        public void SetGizmoValues(string pathElement)
        {
            _PathElementValue = pathElement;
        }
        
        private void OnDrawGizmos()
        {
            if (IsActive)
            {
                Vector3 position = transform.position;
                Vector3 newPos = position;
                newPos.x -= 3;
                newPos.y += 3;
                Gizmos.DrawLine(position, newPos);
                newPos.y += .15f;
                Handles.Label(newPos, $"PathId: {_PathElementValue} | Id: {Identifier}");
            }
        }
#endif
    }
}

