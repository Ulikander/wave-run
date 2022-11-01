using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Zenject;

using WASD.Runtime.Managers;

namespace WASD.Runtime
{
    public class GameManager : MonoBehaviour
    {
        #region Properties
        public Camera MainCamera { get => _MainCamera; }
        #endregion

        #region Constants
        public const string cPprefBgmMuted = "mute_bgm";
        public const string cPprefSFXMuted = "mute_sfx";
        #endregion

        #region Fields
        [SerializeField] private Camera _MainCamera;
        [SerializeField] private int _TargetFrameRate = 60;
        #endregion

        #region MonoBehaviour
        private void Awake()
        {
            RefreshMainCamera();
          
        }
        #endregion

        public void RefreshMainCamera()
        {
            _MainCamera = GameObject.FindGameObjectWithTag(tag: "MainCamera").GetComponent<Camera>();
        }
    }
}

