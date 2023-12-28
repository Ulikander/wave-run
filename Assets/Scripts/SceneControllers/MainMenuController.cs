using System;
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

        [Header("Main Menu (Buttons)")]
        [SerializeField] private ColliderButton[] _MainMenuButtons;
        [Header("Main Options (Neon Texts)")]
        [SerializeField] private SimulatedNeonText _MusicText;
        [SerializeField] private SimulatedNeonText _SfxText;

        [Header("LevelOptions (Buttons)")]
        [SerializeField] private ColliderButton[] _LevelOptionButtons;
        [Header("LevelOptions (Neon Text)")]
        [SerializeField] private SimulatedNeonText _TutorialText;
        [SerializeField] private SimulatedNeonText _LevelsText;
        [SerializeField] private SimulatedNeonText _InfiniteText;
        [SerializeField] private SimulatedNeonText _ReturnToMainFromLevelsText;

        [Header("Credits (Buttons)")] [SerializeField]
        private ColliderButton[] _CreditsButtons;
        [Header("Credits (Neon Text)")]
        [SerializeField] private SimulatedNeonText[] _WASDlogoTexts;
        [SerializeField] private SimulatedNeonText _ReturnToMainFromCreditsText;
        [SerializeField] private SpriteRenderer _CreditsBackgroundSprite;

        [Header("Camera")]
        [SerializeField] private Camera _Camera;
        [SerializeField] private float _CameraPositionTransitionDelay = 0.25f;
        [SerializeField] private float _CameraPositionTransitionTime;
        [SerializeField] private Transform _MainOptionsCameraPosition;
        [SerializeField] private Transform _PlayLevelsCameraPosition;
        [SerializeField] private Transform _CreditsCameraPosition;
        [SerializeField] private Transform _ExtrasCameraPosition;

        [Header("Popups")]
        [SerializeField] LevelSelectorPopup _LevelSelectorPopup;
        [SerializeField] private ExtrasPopup _ExtrasPopup;

        private CancellationTokenSource _CameraTransitionCancelToken;

        private BasePopup.Options _LevelSelectorPopupOptions;
        #endregion

        #region Events

        private Action _OnCameraMovementTransitionEnd;

        #endregion

        #region MonoBehaviour
        private void Start()
        {
            _Camera.transform.SetPositionAndRotation(
                position: _MainOptionsCameraPosition.position,
                rotation: _MainOptionsCameraPosition.rotation);

            _LevelSelectorPopupOptions = new BasePopup.Options
            {
                OnHide = delegate
                {
                    SetAllColliderButtonsInteractable(value: true, target: _PlayLevelsCameraPosition);
                    SetCameraTransitionApplicableNeonTextsActive(_PlayLevelsCameraPosition);
                },
            };

            GameManager.Audio.PlayBgm(bgm: _Music, randomizeStart: true);
            _MusicText.IsOn = !GameManager.Audio.BgmMuted;
            _SfxText.IsOn = !GameManager.Audio.SfxMuted;

            SetAllColliderButtonsInteractable(value: true, target: _MainOptionsCameraPosition);
            SetCameraTransitionApplicableNeonTextsActive(_MainOptionsCameraPosition);
        }

        private void OnDestroy()
        {
            Utils.CancelTokenSourceRequestCancelAndDispose(ref _CameraTransitionCancelToken);
        }
        #endregion

        public void SetAllColliderButtonsInteractable(bool value, Transform target)
        {
            void FSetCollidersEnabled(ColliderButton[] targetButtons, bool setActive)
            {
                foreach (var button in targetButtons)
                {
                    button.Interactable = setActive;
                }
            }
            
            bool mainOptionsIsTarget = value && target == _MainOptionsCameraPosition;
            FSetCollidersEnabled(_MainMenuButtons, mainOptionsIsTarget);

            bool playLevelsIsTarget =  value && target == _PlayLevelsCameraPosition;
            FSetCollidersEnabled(_LevelOptionButtons, playLevelsIsTarget);

            bool creditsIsTarget = value && target == _CreditsCameraPosition;
            FSetCollidersEnabled(_CreditsButtons, creditsIsTarget);
        }

        private void SetCameraTransitionApplicableNeonTextsActive(Transform target)
        {
            bool onInCreditsAndExtras = target == _CreditsCameraPosition || target== _ExtrasCameraPosition;
            foreach(SimulatedNeonText text in _WASDlogoTexts)
            {
                text.IsOn = onInCreditsAndExtras;
            }
            _CreditsBackgroundSprite.enabled = onInCreditsAndExtras;
            _ReturnToMainFromCreditsText.IsOn = onInCreditsAndExtras;
        }
        
        private async void CameraTransitionTask(Transform target)
        {
            _CameraTransitionCancelToken = new CancellationTokenSource();
            
            SetAllColliderButtonsInteractable(value: false, target: target);
            SetCameraTransitionApplicableNeonTextsActive(target);

            await UniTask.Delay((int)(_CameraPositionTransitionDelay * 1000),
                cancellationToken: _CameraTransitionCancelToken.Token).SuppressCancellationThrow();
            if (!Utils.IsCancelTokenSourceActive(ref _CameraTransitionCancelToken)) return;

            Transform cameraTransform = _Camera.transform;
            cameraTransform.DOMove(target.position, _CameraPositionTransitionTime);
            cameraTransform.DORotate(target.rotation.eulerAngles, _CameraPositionTransitionTime);

            await UniTask.Delay((int)(_CameraPositionTransitionTime * 1000),
                cancellationToken: _CameraTransitionCancelToken.Token).SuppressCancellationThrow();
            if (!Utils.IsCancelTokenSourceActive(ref _CameraTransitionCancelToken)) return;

            SetAllColliderButtonsInteractable(value: true, target: target);
            cameraTransform.position = target.position;
            cameraTransform.rotation = target.rotation;
            
            _OnCameraMovementTransitionEnd?.Invoke();
            _OnCameraMovementTransitionEnd = null;
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
            if (!GameManager.Scenes.IsTransitionActive)
            {
                GameManager.ExitAppAfterDelay(1.25f).Forget();
                _OnCameraMovementTransitionEnd += _ExtrasPopup.Show;
                CameraTransitionTask(_ExtrasCameraPosition);
            }
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

        public void OnExtrasButtonTap()
        {
            _OnCameraMovementTransitionEnd += _ExtrasPopup.Show;
        }

        
        public void OnTutorialButtonTap()
        {
            
        }

        public void OnLevelsButtonTap()
        {
            SetAllColliderButtonsInteractable(value: false, target: null);
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


