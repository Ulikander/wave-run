using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using WASD.Interfaces;
using WASD.Runtime.Audio;
using WASD.Runtime.Managers;
using WASD.Runtime.Popups;

namespace WASD.Runtime.SceneControllers
{
    public class MainMenuController : MonoBehaviour, IUseTasks
    {
        #region Fields
        [SerializeField] AudioContainer _Music;

        [Header("Main Options (Buttons)")]
        [SerializeField] private ColliderButton _PlayButton;
        [SerializeField] private ColliderButton _CreditsButton;
        [SerializeField] private ColliderButton _ExitButton;
        [SerializeField] private ColliderButton _MusicButton;
        [SerializeField] private ColliderButton _SfxButton;
        [Header("Main Options (Neon Texts)")]
        [SerializeField] private SimulatedNeonText _PlayText;
        [SerializeField] private SimulatedNeonText _CreditsText;
        [SerializeField] private SimulatedNeonText _ExitText;
        [SerializeField] private SimulatedNeonText _MusicText;
        [SerializeField] private SimulatedNeonText _SfxText;

        [Header("LevelOptions (Buttons)")]
        [SerializeField] private ColliderButton _TutorialButton;
        [SerializeField] private ColliderButton _LevelsButton;
        [SerializeField] private ColliderButton _InfiniteButton;
        [SerializeField] private ColliderButton _ReturnToMainFromLevelsButton;
        [Header("LevelOptions (Neon Text)")]
        [SerializeField] private SimulatedNeonText _TutorialText;
        [SerializeField] private SimulatedNeonText _LevelsText;
        [SerializeField] private SimulatedNeonText _InfiniteText;
        [SerializeField] private SimulatedNeonText _ReturnToMainFromLevelsText;

        [Header("Credits (Buttons)")]
        [SerializeField] private ColliderButton _WASDlogoButton;
        [SerializeField] private ColliderButton _TwitterButton;
        [SerializeField] private ColliderButton _FacebookButton;
        [SerializeField] private ColliderButton _InstagramButton;
        [SerializeField] private ColliderButton _NewgroundsButton;
        [SerializeField] private ColliderButton _ReturnToMainFromCreditsButton;
        
        [Header("Credits (Neon Text)")]
        [SerializeField] private SimulatedNeonText[] _WASDlogoTexts;
        [SerializeField] private SimulatedNeonText _ReturnToMainFromCreditsText;


        [Header("Camera")]
        [SerializeField] private Camera _Camera;
        [SerializeField] private float _CameraPositionTransitionDelay = 0.25f;
        [SerializeField] private float _CameraPositionTransitionTime;
        [SerializeField] private Transform _MainOptionsCameraPosition;
        [SerializeField] private Transform _PlayLevelsCameraPosition;
        [SerializeField] private Transform _CreditsCameraPosition;

        [Header("Popups")]
        [SerializeField] LevelSelectorPopup _LevelSelectorPopup;


        private UnityTask _CameraTransitionTask;
        private WaitForSeconds _WaitForCameraTransitionDelay;
        private BasePopup.Options _LevelSelectorPopupOptions;
        #endregion

        #region MonoBehaviour
        private void Start()
        {
            _Camera.transform.SetPositionAndRotation(
                position: _MainOptionsCameraPosition.position,
                rotation: _MainOptionsCameraPosition.rotation);

            _WaitForCameraTransitionDelay = new WaitForSeconds(seconds: _CameraPositionTransitionDelay);
            _LevelSelectorPopupOptions = new BasePopup.Options
            {
                OnHide = delegate
                {
                    SetAllColliderButtonsInteractability(value: true, target: _PlayLevelsCameraPosition);
                },
            };

            GameManager.Audio.PlayBGM(bgm: _Music, randomizeStart: true);
            _MusicText.IsOn = !GameManager.Audio.BgmMuted;
            _SfxText.IsOn = !GameManager.Audio.SfxMuted;

            SetAllColliderButtonsInteractability(value: true, target: _MainOptionsCameraPosition);
        }

        private void OnDisable()
        {
            StopAllTasks();
        }
        #endregion

        public void SetAllColliderButtonsInteractability(bool value, Transform target)
        {
            _PlayButton.Interactable = value && target == _MainOptionsCameraPosition;
            _CreditsButton.Interactable = value && target == _MainOptionsCameraPosition;
            _ExitButton.Interactable = value && target == _MainOptionsCameraPosition;
            _MusicButton.Interactable = value && target == _MainOptionsCameraPosition;
            _SfxButton.Interactable = value && target == _MainOptionsCameraPosition;

            _TutorialButton.Interactable = value && target == _PlayLevelsCameraPosition;
            _LevelsButton.Interactable = value && target == _PlayLevelsCameraPosition;
            _InfiniteButton.Interactable = value && target == _PlayLevelsCameraPosition;
            _ReturnToMainFromLevelsButton.Interactable = value && target == _PlayLevelsCameraPosition;

            _WASDlogoButton.Interactable = value && target == _CreditsCameraPosition;
            foreach(SimulatedNeonText text in _WASDlogoTexts)
            {
                text.IsOn = target == _CreditsCameraPosition;
            }
            _TwitterButton.Interactable = value && target == _CreditsCameraPosition;
            _FacebookButton.Interactable = value && target == _CreditsCameraPosition;
            _InstagramButton.Interactable = value && target == _CreditsCameraPosition;
            _NewgroundsButton.Interactable = value && target == _CreditsCameraPosition;
            _ReturnToMainFromCreditsButton.Interactable = value && target == _CreditsCameraPosition;
            _ReturnToMainFromCreditsText.IsOn = _ReturnToMainFromCreditsButton.Interactable;
            
        }

        private IEnumerator CameraTransitionRoutine(Transform target)
        {
            SetAllColliderButtonsInteractability(value: false, target: target);

            yield return _WaitForCameraTransitionDelay;

            _Camera.transform.LeanMove(to: target.position, time: _CameraPositionTransitionTime);
            _Camera.transform.LeanRotate(to: target.rotation.eulerAngles, time: _CameraPositionTransitionTime);

            yield return new WaitForSeconds(seconds: _CameraPositionTransitionTime);

            SetAllColliderButtonsInteractability(value: true, target: target);
            _Camera.transform.position = target.position;
            _Camera.transform.rotation = target.rotation;
        }

        public void OnNewCameraPositionButtonTap(Transform target)
        {
            Utils.StopUnityTask(task: ref _CameraTransitionTask);
            _CameraTransitionTask = new(c: CameraTransitionRoutine(target: target));
        }

        public void OnOpenUrlButtonTap(string url)
        {
            if(string.IsNullOrEmpty(value: url))
            {
                return;
            }

            Application.OpenURL(url: url);
        }

        public void OnExitButtonTap()
        {

        }

        public void OnMusicButtonTap()
        {
            GameManager.Audio.BgmMuted = !GameManager.Audio.BgmMuted;
            _MusicText.IsOn = !GameManager.Audio.BgmMuted;
        }

        public void OnSfxButtonTap()
        {
            GameManager.Audio.SfxMuted = !GameManager.Audio.SfxMuted;
            _SfxText.IsOn = !GameManager.Audio.SfxMuted;
        }

        
        public void OnTutorialButtonTap()
        {

        }

        public void OnLevelsButtonTap()
        {
            SetAllColliderButtonsInteractability(value: false, target: null);
            _LevelSelectorPopup.Show(options: _LevelSelectorPopupOptions);
        }

        public void OnInfiniteButtonTap()
        {

        }

        public void GoToInitScene()
        {
            GameManager.Scenes.LoadScene(sceneId: ScenesManager.cSCENEID_INIT);
        }

        public void StopAllTasks()
        {
            Utils.StopUnityTask(task: ref _CameraTransitionTask);
        }
    }
}


