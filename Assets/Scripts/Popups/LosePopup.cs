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
        [SerializeField] private Button _ExtraLifeButton;
        #endregion

        public override void Populate()
        {
            _ExtraLifeButton.interactable = false;
        }

        public void OnTapRestart()
        {

        }

        public void OnTapExtraLife()
        {

        }
    }

}
