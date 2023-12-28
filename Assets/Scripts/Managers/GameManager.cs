using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using UnityEngine;
using WASD.Data;
using WASD.Runtime.Levels;
using WASD.Runtime.Managers;

namespace WASD.Runtime.Managers
{
    [RequireComponent(typeof(AudioManager), typeof(InputManager), typeof(ScenesManager))]
    public class GameManager : MonoBehaviour
    {
        #region Properties
        
        public static GameManager Instance { get; private set; }
        public static AudioManager Audio { get => Instance._AudioManager; }
        public static ScenesManager Scenes { get => Instance._ScenesManager; }
        public static SaveDataContainer SaveData => Instance._SaveDataContainer;
        
        public static Camera MainCamera { get => Instance._MainCamera; }

        public static int CurrentCoreLevel;
        
        #endregion

        #region Constants
        public const string cPprefBgmMuted = "mute_bgm";
        public const string cPprefSFXMuted = "mute_sfx";
        #endregion

        #region Fields
        [Header("Managers")]
        [SerializeField] private AudioManager _AudioManager;
        [SerializeField] private ScenesManager _ScenesManager;

        [Header("Camera")]
        [SerializeField] private Canvas _MainCanvas;
        [SerializeField] private Camera _MainCamera;
        [SerializeField] private int _TargetFrameRate = 60;

        [Header("Levels")]
        [SerializeField] private LevelInformation[] coreLevels;

        private SaveDataContainer _SaveDataContainer;
        
        #endregion

        #region MonoBehaviour
        private void Awake()
        {
            if(Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(target: gameObject);

                //Destroy(obj: _MainCanvas.worldCamera.gameObject);
                Application.targetFrameRate = _TargetFrameRate;
                RefreshMainCamera();

                _SaveDataContainer = new SaveDataContainer();
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
            //Instance._MainCanvas.worldCamera = Instance._MainCamera;
        }

        public static bool TryGetCoreLevel(int index, out LevelInformation level)
        {
            if (index <= 0 || index >= Instance.coreLevels.Length)
            {
                level = null;
                return false;
            }

            level = Instance.coreLevels.FirstOrDefault(lvl => lvl.CoreLevelValue == index);
            return level != null;
        }

        public static async UniTaskVoid ExitAppAfterDelay(float delay)
        {
            Scenes.FadeScreenToBlack(delay).Forget();
            await UniTask.Delay((int)((delay + 0.25f) * 1000));
            Application.Quit();
        }
    }
}

