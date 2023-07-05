using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;
using UnityEngine.SceneManagement;
using WASD.Interfaces;
using WASD.Runtime.Audio;
using WASD.Runtime.Managers;
using WASD.Runtime.Popups;

namespace WASD.Runtime.SceneControllers
{
    public class MainMenuController : MonoBehaviour
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

        private CancellationTokenSource _CameraTransitionCancelToken;

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

            GameManager.Audio.PlayBgm(bgm: _Music, randomizeStart: true);
            _MusicText.IsOn = !GameManager.Audio.BgmMuted;
            _SfxText.IsOn = !GameManager.Audio.SfxMuted;

            SetAllColliderButtonsInteractability(value: true, target: _MainOptionsCameraPosition);
        }

        private void OnDestroy()
        {
            Utils.CancelTokenSourceRequestCancelAndDispose(ref _CameraTransitionCancelToken);
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

        private async void CameraTransitionTask(Transform target)
        {
            _CameraTransitionCancelToken = new CancellationTokenSource();
            
            SetAllColliderButtonsInteractability(value: false, target: target);

            await UniTask.Delay((int)(_CameraPositionTransitionDelay * 1000),
                cancellationToken: _CameraTransitionCancelToken.Token).SuppressCancellationThrow();
            if (_CameraTransitionCancelToken.IsCancellationRequested) return;

            Transform cameraTransform = _Camera.transform;
            cameraTransform.DOMove(target.position, _CameraPositionTransitionTime);
            cameraTransform.DORotate(target.rotation.eulerAngles, _CameraPositionTransitionTime);

            await UniTask.Delay((int)(_CameraPositionTransitionTime * 1000),
                cancellationToken: _CameraTransitionCancelToken.Token).SuppressCancellationThrow();
            if (_CameraTransitionCancelToken.IsCancellationRequested) return;

            SetAllColliderButtonsInteractability(value: true, target: target);
            cameraTransform.position = target.position;
            cameraTransform.rotation = target.rotation;
            
            Utils.CancelTokenSourceRequestCancelAndDispose(ref _CameraTransitionCancelToken);
        }

        public void OnNewCameraPositionButtonTap(Transform target)
        {
            if (Utils.IsCancelTokenSourceActive(ref _CameraTransitionCancelToken))
            {
                Debug.LogWarning("Attempted to do a camera transition, but one is already active!");
                return;
            }
            
            CameraTransitionTask(target);
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
            Application.Quit();
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
    }
}


