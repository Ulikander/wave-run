using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using TMPro;
using WASD.Runtime.Managers;

namespace WASD.Runtime.Popups
{
    public class WinPopup : BasePopup
    {
        #region Fields
        [Header("Win")]
        [SerializeField] private float _SwitchLevelTime;
        [SerializeField] private float _AudioBgmPitch;
        [SerializeField] [TextArea(minLines: 2, maxLines: 5)] private string _NextLevelMessage;
        [SerializeField] [TextArea(minLines: 2, maxLines: 5)] private string _FinalLevelMessage;
        [SerializeField] private TextMeshProUGUI _TimerText;

        private UnityTask _SwitchLevelTimerTask;
        #endregion

        #region Events
        [SerializeField] private UnityEvent _OnSwitchLevel;
        #endregion

        public override void Populate()
        {
            if (_AudioBgmPitch > 0 && _AudioBgmPitch <= 3) GameManager.Audio.FadeBgmPitch(target: _AudioBgmPitch);
            _TimerText.text = _NextLevelMessage + $"\n{_SwitchLevelTime}";
            _SwitchLevelTimerTask = new(c: SwitchLevelOnTimerTask());
        }

        public override void Hide()
        {
            Utils.StopUnityTask(task: ref _SwitchLevelTimerTask);
            base.Hide();
        }

        public override void GoToMainMenuScene(bool closePopup = false)
        {
            Utils.StopUnityTask(task: ref _SwitchLevelTimerTask);
            base.GoToMainMenuScene(closePopup);
        }

        private IEnumerator SwitchLevelOnTimerTask()
        {
            float time = _SwitchLevelTime;
            while(time > 0)
            {
                time -= Time.deltaTime;
                _TimerText.text = _NextLevelMessage + $"\n{Mathf.CeilToInt(f: time)}";
                yield return null;
            }

            _TimerText.text = _NextLevelMessage;
            GameManager.Audio.FadeBgmPitch(target: 1f);
            _OnSwitchLevel?.Invoke();
        }
    }
}

