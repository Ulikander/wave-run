using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using WASD.Runtime.Managers;
using Zenject;
using WASD.Interfaces;

namespace WASD.Runtime
{
    public class SimulatedNeonText : MonoBehaviour, IUseTasks
    {
        #region Properties
        public bool IsOn
        {
            get
            {
                return _IsOn;
            }
            set
            {
                _TextOn.enabled = value;
                _TextOff.enabled = !value;
                ChangeAllMeshRenderersMaterial(value ? _MeshMaterialLit : _MeshMaterialUnlit);

                if (value)
                {
                    StartNeonTasks();
                }
                else
                {
                    StopAllTasks();
                    
                }

                _IsOn = value;
            }
        }
        #endregion

        #region Fields
        [Header("Customize")]
        [SerializeField] private string _Label;
        [SerializeField] Material _TextMaterialLit;
        [SerializeField] private bool _IsOn = true;
        [SerializeField] private Vector2 _LitDurationRange;
        [SerializeField] private Vector2 _UnLitDurationRange;
        [SerializeField] private Vector2 _PulsingRange;
        [SerializeField] private float _PulsingTime;

        [Header("Meshes")]
        [SerializeField] private Material _MeshMaterialLit;
        [SerializeField] private Material _MeshMaterialUnlit;
        [SerializeField] private MeshRenderer[] _AffectedMeshRenderers;
        [SerializeField] private int[] _AffectedMeshRenderersMaterialIndex;


        [Header("References")]
        [SerializeField] private TextMeshPro _TextOn;
        [SerializeField] private TextMeshPro _TextOff;
        #endregion

        private UnityTask _PulsingTask;
        private UnityTask _LitAndUnlitTask;

        [Inject]
        private TaskManager _TaskManager;

        #region MonoBehaviour
        private void Start()
        {
            UpdateAllDisplayValues();

            if (_IsOn)
            {
                StartNeonTasks();
            }
        }

        private void OnDisable()
        {
            StopAllTasks();
        }
        #endregion


        private bool ValidateRange(ref Vector2 ranges)
        {
            if(ranges.x > ranges.y || ranges.x < 0f || ranges.y <= 0f)
            {
                Debug.LogError(message: $"Neon Simul Invalid range: {ranges}");
                return false;
            }

            return true;
        }

        private void StartNeonTasks()
        {
            if (
                !Utils.IsUnityTaskRunning(task: ref _LitAndUnlitTask) &&
                ValidateRange(ranges: ref _LitDurationRange) &&
                ValidateRange(ranges: ref _UnLitDurationRange))
            {
                _LitAndUnlitTask = new (manager: _TaskManager, c: LitAndUnlitControl());
            }

            if (
                !Utils.IsUnityTaskRunning(task: ref _PulsingTask) &&
                ValidateRange(ranges: ref _PulsingRange) &&
                _PulsingTime > 0f)
            {
                _PulsingTask = new(manager: _TaskManager, c: PulsingControl());
            }
        }

        private IEnumerator PulsingControl()
        {
            float counter;

            Color pulseColor = _TextOn.color;
            pulseColor.a = _PulsingRange.y;

            _TextOn.color = pulseColor;

            while (true)
            {
                counter = 0;
                while (counter < _PulsingTime / 2f)
                {
                    counter += Time.deltaTime;
                    pulseColor.a = Mathf.Lerp(a: _PulsingRange.y, b: _PulsingRange.x, t: counter / (_PulsingTime / 2f));
                    _TextOn.color = pulseColor;
                    yield return null;
                }

                counter = 0;
                while (counter < _PulsingTime / 2f)
                {
                    counter += Time.deltaTime;
                    pulseColor.a = Mathf.Lerp(a: _PulsingRange.x, b: _PulsingRange.y, t: counter / (_PulsingTime / 2f));
                    _TextOn.color = pulseColor;
                    yield return null;
                }
            }
        }

        private IEnumerator LitAndUnlitControl()
        {
            float randomOnTime;
            float randomOffTime;

            while (true)
            {
                randomOnTime = Random.Range(minInclusive: _LitDurationRange.x, maxInclusive: _LitDurationRange.y);
                randomOffTime = Random.Range(minInclusive: _UnLitDurationRange.x, maxInclusive: _UnLitDurationRange.y);

                _TextOff.enabled = false;
                _TextOn.enabled = true;
                ChangeAllMeshRenderersMaterial(material: _MeshMaterialLit);

                yield return new WaitForSeconds(seconds: randomOnTime);

                _TextOff.enabled = true;
                _TextOn.enabled = false;
                ChangeAllMeshRenderersMaterial(material: _MeshMaterialUnlit);

                yield return new WaitForSeconds(seconds: randomOffTime);
            }
        }

        private void ChangeAllMeshRenderersMaterial(Material material)
        {
            if(
                material == null ||
                _AffectedMeshRenderers.Length == 0 ||
                _AffectedMeshRenderersMaterialIndex.Length == 0 ||
                _AffectedMeshRenderers.Length != _AffectedMeshRenderersMaterialIndex.Length)
            {
                return;
            }

            for(int i = 0; i < _AffectedMeshRenderers.Length; i++)
            {
                Material[] newMaterials = _AffectedMeshRenderers[i].sharedMaterials;
                newMaterials[_AffectedMeshRenderersMaterialIndex[i]] = material;
                _AffectedMeshRenderers[i].sharedMaterials = newMaterials;
            }
        }

        public void UpdateAllDisplayValues()
        {
            if (_TextOn != null)
            {
                _TextOn.text = _Label;
                _TextOn.enabled = _IsOn;
                if (_TextMaterialLit != null)
                {
                    _TextOn.fontSharedMaterial = _TextMaterialLit;
                }
            }

            if (_TextOff != null)
            {
                _TextOff.text = _Label;
                _TextOff.enabled = !_IsOn;
            }

            ChangeAllMeshRenderersMaterial(material: _IsOn ? _MeshMaterialLit : _MeshMaterialUnlit);
        }

        public void ToggleIsOn()
        {
            IsOn = !IsOn;
        }

        public void StopAllTasks()
        {
            Utils.StopUnityTask(task: ref _PulsingTask);
            Utils.StopUnityTask(task: ref _LitAndUnlitTask);
        }
    }
}

