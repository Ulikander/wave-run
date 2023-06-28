using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

using WASD.Runtime.Managers;

namespace WASD.Runtime.Popups
{
    public class PausePopup : BasePopup
    {
        #region Fields
        [Header("Pause")]
        [SerializeField] private CanvasGroup _ButtonCanvasGroup;
        [SerializeField] private Image _MusicIcon;
        [SerializeField] private Image _SfxIcon;
        [SerializeField] private Color _AudioIconOn;
        [SerializeField] private Color _AudioIconOff;
        [SerializeField] private float _AudioBgmPitch;
        #endregion

        #region Events
        [SerializeField] private UnityEvent<bool> _OnPause;
        #endregion

        private void Start()
        {
            _OnHide += () =>
            {
                _OnPause.Invoke(arg0: false);
                if (_ButtonCanvasGroup != null) _ButtonCanvasGroup.alpha = 1f;
            };
        }

        public override void Populate()
        {
            if (_AudioBgmPitch > 0 && _AudioBgmPitch <= 3) GameManager.Audio.FadeBgmPitch(target: _AudioBgmPitch);
            _MusicIcon.color = GameManager.Audio.BgmMuted ? _AudioIconOff : _AudioIconOn;
            _SfxIcon.color = GameManager.Audio.SfxMuted ? _AudioIconOff : _AudioIconOn;

            _OnPause.Invoke(arg0: true);
            _Animate = true;

            if (_ButtonCanvasGroup != null) _ButtonCanvasGroup.alpha = 0f;
        }

        public override void Hide()
        {
            GameManager.Audio.FadeBgmPitch(target: 1f);
            _Animate = false;
            base.Hide();
        }

        public void OnTapMusicButton()
        {
            GameManager.Audio.BgmMuted = !GameManager.Audio.BgmMuted;
            _MusicIcon.color = GameManager.Audio.BgmMuted ? _AudioIconOff : _AudioIconOn;
        }
        public void OnTapSfxButton()
        {
            GameManager.Audio.SfxMuted = !GameManager.Audio.SfxMuted;
            _SfxIcon.color = GameManager.Audio.SfxMuted ? _AudioIconOff : _AudioIconOn;
        }

        public void SetButtonVisibility(bool value)
        {
            if (_ButtonCanvasGroup != null) _ButtonCanvasGroup.alpha = value ? 1f : 0f;
        }
    }

}
