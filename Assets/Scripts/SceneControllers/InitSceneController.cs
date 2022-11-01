using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WASD.Interfaces;
using WASD.Runtime.Audio;
using WASD.Runtime.Managers;
using Zenject;

namespace WASD.Runtime
{
    public class InitSceneController : MonoBehaviour
    {
        #region Fields
        [SerializeField] private Camera _MainCamera;
        [SerializeField] private AudioContainer _Music;

        [Inject] private readonly AudioManager _AudioManager;
        [Inject] private readonly ScenesManager _ScenesManager;
        #endregion

        #region MonoBehaviour
        private void Start()
        {
            _AudioManager.PlayBGM(audioContainer: _Music, skipFades: new bool[] { true, true }, restartIfSame: true);
            _ScenesManager.LoadScene(sceneId: ScenesManager.cSCENEID_MAINMENU);
        }
        #endregion
    }
}

