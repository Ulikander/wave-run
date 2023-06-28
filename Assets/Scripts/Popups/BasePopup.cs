using System;
using System.Collections;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;
using WASD.Runtime.Audio;
using WASD.Runtime.Managers;

namespace WASD.Runtime.Popups
{
    public abstract class BasePopup : MonoBehaviour
    {
        #region Types
        public class Options
        {
            public Action OnShow;
            public Action OnHide;
            public bool Animate = true;
        }
        #endregion

        #region Fields
        [SerializeField] protected Canvas _Canvas;
        [SerializeField] protected CanvasGroup _Frame;
        [SerializeField] private Vector2 _FrameTransitionScaleRange;
        [SerializeField] private Vector2 _FrameAlphaRange;
        [SerializeField] private float _FrameTransitionTime = 0.4f;
        [SerializeField] protected AudioContainer _OnShowSound;
        [SerializeField] protected AudioContainer _OnHideSound;
        
        protected bool _Animate;
        protected Action _OnShow;
        protected Action _OnHide;

        private CancellationTokenSource _FrameTransitionCancelToken;

        
        #endregion

        #region MonoBehaviour
        protected virtual void Awake()
        {
            _Canvas.enabled = false;
        }

        protected virtual void OnDestroy()
        {
            Utils.CancelTokenSourceRequestCancelAndDispose(ref _FrameTransitionCancelToken);
        }

        #endregion

        public void Show() => Show(options: null);
        public virtual void Show(Options options)
        {
            if (Utils.IsCancelTokenSourceActive(ref _FrameTransitionCancelToken) || _Canvas.enabled)
            {
                return;
            }

            if (options != null)
            {
                _OnShow = options.OnShow;
                _OnHide = options.OnHide;
                _Animate = options.Animate;
            }

            Populate();

            if (_Animate)
            {
                FrameTransitionAsync(true);
            }
            else
            {
                if (_OnShowSound != null)
                {
                    GameManager.Audio.PlaySfx(sfx: _OnShowSound);
                }
                _Canvas.enabled = true;
                _Frame.alpha = 1;
                _Frame.gameObject.transform.DOScale(Vector3.one * _FrameTransitionScaleRange.y, 0f);
                _Frame.interactable = true;
                _Canvas.enabled = true;
                _OnShow?.Invoke();

            }
        }

        public abstract void Populate();

        public virtual void Hide()
        {
            if (Utils.IsCancelTokenSourceActive(ref _FrameTransitionCancelToken) || !_Canvas.enabled)
            {
                return;
            }

            if (_Animate)
            {
                FrameTransitionAsync(false);
            }
            else
            {
                if (_OnHideSound != null)
                {
                    GameManager.Audio.PlaySfx(sfx: _OnHideSound);
                }
                _Frame.alpha = 0;
                _Frame.gameObject.transform.DOScale(Vector3.one * _FrameTransitionScaleRange.x, 0f);
                _Frame.interactable = false;
                _Canvas.enabled = false;
                _OnHide?.Invoke();
            }
        }

        protected virtual async void FrameTransitionAsync(bool isShow)
        {
            _FrameTransitionCancelToken = new CancellationTokenSource();

            _Frame.alpha = isShow ? 0f : 1f;
            _Frame.gameObject.transform.DOScale(
                Vector3.one * (isShow ? _FrameTransitionScaleRange.x : _FrameTransitionScaleRange.y), 0f);
            _Frame.interactable = false;
            _Canvas.enabled = true;

            DOTween.To(() => _Frame.alpha, (x) => _Frame.alpha = x, isShow ? _FrameAlphaRange.y : _FrameAlphaRange.x,
                _FrameTransitionTime);

            if (isShow)
            {
                _Frame.transform.DOScale(
                    Vector3.one * (isShow ? _FrameTransitionScaleRange.y : _FrameTransitionScaleRange.x),
                    _FrameTransitionTime / 2f);
            }

            await UniTask.Delay((int)(_FrameTransitionTime * 1000),
                    cancellationToken: _FrameTransitionCancelToken.Token)
                .SuppressCancellationThrow();

            _Frame.interactable = isShow;
            _Canvas.enabled = isShow;

            if (isShow)
            {
                _OnShow?.Invoke();
            }
            else
            {
                _OnHide?.Invoke();
            }
            
            Utils.CancelTokenSourceRequestCancelAndDispose(ref _FrameTransitionCancelToken);
        }

        public virtual void GoToMainMenuScene(bool closePopup = false)
        {
            if (closePopup)
            {
                Hide();
            }
            else
            {
                _Frame.interactable = false;
            }

            GameManager.Scenes.LoadScene(sceneId: ScenesManager.cSCENEID_MAINMENU);
            GameManager.Audio.FadeBgmPitch(target: 1f);
            GameManager.Audio.StopBgm(skipFadeOut: false);
        }
    }
}

