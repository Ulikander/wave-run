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
        public static InputManager Input { get => Instance._InputManager; }
        public static ScenesManager Scenes { get => Instance._ScenesManager; }
        public static TaskManager Tasks { get => Instance._TaskManager; }

        public static Camera MainCamera { get => Instance._MainCamera; }
        #endregion

        #region Constants
        public const string cPprefBgmMuted = "mute_bgm";
        public const string cPprefSFXMuted = "mute_sfx";
        #endregion

        #region Fields
        [Header("Managers")]
        [SerializeField] private AudioManager _AudioManager;
        [SerializeField] private InputManager _InputManager;
        [SerializeField] private ScenesManager _ScenesManager;
        [SerializeField] private TaskManager _TaskManager;

        [Header("Other Stuff")]
        [SerializeField] private Camera _MainCamera;
        [SerializeField] private int _TargetFrameRate = 60;
        #endregion

        #region MonoBehaviour
        private void Awake()
        {
            if(Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(target: gameObject);

                RefreshMainCamera();
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
        }
    }
}

