using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WASD.Interfaces;
using WASD.Runtime.Audio;
using WASD.Runtime.Managers;


namespace WASD.Runtime
{
    public class InitSceneController : MonoBehaviour
    {
        #region Fields
        [SerializeField] private Camera _MainCamera;
        #endregion

        #region MonoBehaviour
        private void Start()
        {
            GameManager.PlayMainMenuMusic(true);
            GameManager.Scenes.LoadScene(sceneId: ScenesManager.cSCENEID_MAINMENU);
        }
        #endregion
    }
}

