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
        [SerializeField] private AudioContainer _Music;
        #endregion

        #region MonoBehaviour
        private void Start()
        {
            GameManager.Audio.PlayBgm(bgm: _Music, skipFadeIn: true, skipFadeOut: true, restartIfSame: true);
            GameManager.Scenes.LoadScene(sceneId: ScenesManager.cSCENEID_MAINMENU);
        }
        #endregion
    }
}

