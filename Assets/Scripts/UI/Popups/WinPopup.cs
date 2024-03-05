using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
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
        [SerializeField] private bool _forceAnimate;

        private CancellationTokenSource _SwitchLevelOnTimerCancelToken;
        
        #endregion

        #region Events
        [SerializeField] private UnityEvent _OnSwitchLevel;
        #endregion

        #region Monobehaviour

        protected override void OnDestroy()
        {
            base.OnDestroy();
            Utils.CancelTokenSourceRequestCancelAndDispose(ref _SwitchLevelOnTimerCancelToken);
        }

        #endregion

        public override void Populate()
        {
            if (_AudioBgmPitch > 0 && _AudioBgmPitch <= 3) GameManager.Audio.FadeBgmPitch(target: _AudioBgmPitch);
            _TimerText.text = _NextLevelMessage + $"\n{_SwitchLevelTime}";
            SwitchLevelOnTimerTask();
            _Animate = _forceAnimate;
            base.Populate();
        }

        public override void Show(Options options)
        {
            GameManager.SaveData.SetLevelClearState(GameManager.CurrentCoreLevel, true);
            base.Show(options);
        }

        public override void Hide()
        {
            Utils.CancelTokenSourceRequestCancelAndDispose(ref _SwitchLevelOnTimerCancelToken);
            base.Hide();
        }

        public override void GoToMainMenuScene(bool closePopup = false)
        {
            Utils.CancelTokenSourceRequestCancelAndDispose(ref _SwitchLevelOnTimerCancelToken);
            base.GoToMainMenuScene(closePopup);
        }

        private async void SwitchLevelOnTimerTask()
        {
            _SwitchLevelOnTimerCancelToken = new CancellationTokenSource();
            float time = _SwitchLevelTime;
            while(!_SwitchLevelOnTimerCancelToken.IsCancellationRequested && time > 0)
            {
                time -= Time.deltaTime;
                _TimerText.text = _NextLevelMessage + $"\n{Mathf.CeilToInt(f: time)}";
                await UniTask.Yield(_SwitchLevelOnTimerCancelToken.Token).SuppressCancellationThrow();
                if (!Utils.IsCancelTokenSourceActive(ref _SwitchLevelOnTimerCancelToken)) return;
            }

            _TimerText.text = _NextLevelMessage;
            GameManager.Audio.FadeBgmPitch(target: 1f);
            _Frame.interactable = false;
            _OnSwitchLevel?.Invoke();
            Utils.CancelTokenSourceRequestCancelAndDispose(ref _SwitchLevelOnTimerCancelToken);
        }
    }
}

