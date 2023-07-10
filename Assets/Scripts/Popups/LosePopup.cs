using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

using WASD.Runtime.Managers;

namespace WASD.Runtime.Popups
{
    public class LosePopup : BasePopup
    {
        #region Fields
        [Header("Lose")]
        [SerializeField] private float _AudioBgmPitch;
        [SerializeField] private Button _ExtraLifeButton;
        [SerializeField] private bool _forceAnimate;
        #endregion

        public override void Populate()
        {
            _Animate = _forceAnimate;
            if (_AudioBgmPitch > 0 && _AudioBgmPitch <= 3) GameManager.Audio.FadeBgmPitch(target: _AudioBgmPitch);
            _ExtraLifeButton.interactable = false;
        }

        public void OnTapRestart()
        {
            GameManager.Scenes.LoadScene(ScenesManager.cSCENEID_GAMEPLAY);
            GameManager.Audio.FadeBgmPitch(target: 1f);
        }

        public void OnTapExtraLife()
        {

        }
    }

}
