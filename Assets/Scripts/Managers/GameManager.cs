using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using WASD.Runtime.Managers;

namespace WASD.Runtime
{
    [RequireComponent(typeof(AudioManager), typeof(InputManager), typeof(ScenesManager))]
    [RequireComponent(typeof(TaskManager))]
    public class GameManager : MonoBehaviour
    {
        #region Properties
        public static GameManager Instance { get; private set; }
        public static AudioManager Audio { get => Instance._AudioManager; }
        public static ScenesManager Scenes { get => Instance._ScenesManager; }
        public static TaskManager Tasks { get => Instance._TaskManager; }

        public static Camera MainCamera { get => Instance._MainCamera; }
        public static int LastCoreLevelUnlocked { get => Instance._LastCoreLevelUnlocked; }
        #endregion

        #region Constants
        public const string cPprefBgmMuted = "mute_bgm";
        public const string cPprefSFXMuted = "mute_sfx";
        public const string cPprefCoreLevel = "level_lastunlock";
        #endregion

        #region Fields
        [Header("Managers")]
        [SerializeField] private AudioManager _AudioManager;
        [SerializeField] private ScenesManager _ScenesManager;
        [SerializeField] private TaskManager _TaskManager;

        [Header("Camera")]
        [SerializeField] private Canvas _MainCanvas;
        [SerializeField] private Camera _MainCamera;
        [SerializeField] private int _TargetFrameRate = 60;

        private int _LastCoreLevelUnlocked;
        #endregion

        #region MonoBehaviour
        private void Awake()
        {
            if(Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(target: gameObject);

                Destroy(obj: _MainCanvas.worldCamera.gameObject);
                Application.targetFrameRate = _TargetFrameRate;
                RefreshMainCamera();
                _LastCoreLevelUnlocked = PlayerPrefs.GetInt(key: cPprefCoreLevel, defaultValue: 1);
            }
            else
            {
                Destroy(obj: gameObject);
            }
        }
        #endregion

        public static void RefreshMainCamera()
        {
            Instance._MainCamera = GameObject.FindGameObjectWithTag(tag: "MainCamera").GetComponent<Camera>();
            Instance._MainCanvas.worldCamera = Instance._MainCamera;
        }

    }
}

