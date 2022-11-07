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
            GameManager.Audio.PlayBGM(bgm: _Music, skipFades: new bool[] { true, true }, restartIfSame: true);
            GameManager.Scenes.LoadScene(sceneId: ScenesManager.cSCENEID_MAINMENU);
        }
        #endregion
    }
}

