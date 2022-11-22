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
        [SerializeField] private Image _MusicIcon;
        [SerializeField] private Image _SfxIcon;
        [SerializeField] private Color _AudioIconOn;
        [SerializeField] private Color _AudioIconOff;
        #endregion

        #region Events
        [SerializeField] private UnityEvent<bool> _OnPause;
        #endregion

        public override void Populate()
        {
            _OnPause.Invoke(arg0: true);

            _MusicIcon.color = GameManager.Audio.BgmMuted ? _AudioIconOff : _AudioIconOn;
            _SfxIcon.color = GameManager.Audio.SfxMuted ? _AudioIconOff : _AudioIconOn;
        }

        public override void Hide()
        {
            _OnPause.Invoke(arg0: false);
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
    }

}
