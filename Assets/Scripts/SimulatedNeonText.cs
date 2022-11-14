using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using WASD.Runtime.Managers;

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

                Utils.ChangeAllMeshRenderersMaterial(
                    renderers: _AffectedMeshRenderers,
                    indexes: _AffectedMeshRenderersMaterialIndex,
                    material: value ? _MeshMaterialLit : _MeshMaterialUnlit);

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
                _LitAndUnlitTask = new (c: LitAndUnlitControl());
            }

            if (
                !Utils.IsUnityTaskRunning(task: ref _PulsingTask) &&
                ValidateRange(ranges: ref _PulsingRange) &&
                _PulsingTime > 0f)
            {
                _PulsingTask = new(c: PulsingControl());
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
                if(_PulsingTime == 0)
                {
                    yield return null;
                    continue;
                }

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
                Utils.ChangeAllMeshRenderersMaterial(
                    renderers: _AffectedMeshRenderers,
                    indexes: _AffectedMeshRenderersMaterialIndex,
                    material: _MeshMaterialLit);

                yield return new WaitForSeconds(seconds: randomOnTime);

                _TextOff.enabled = true;
                _TextOn.enabled = false;
                Utils.ChangeAllMeshRenderersMaterial(
                    renderers: _AffectedMeshRenderers,
                    indexes: _AffectedMeshRenderersMaterialIndex,
                    material: _MeshMaterialUnlit);

                yield return new WaitForSeconds(seconds: randomOffTime);
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

            Utils.ChangeAllMeshRenderersMaterial(
                  renderers: _AffectedMeshRenderers,
                  indexes: _AffectedMeshRenderersMaterialIndex,
                  material: _IsOn ? _MeshMaterialLit : _MeshMaterialUnlit);
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

