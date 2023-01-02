using System;
using System.Collections;
using System.Collections.Generic;
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

        protected Action _OnShow;
        protected Action _OnHide;
        [SerializeField] protected bool _Animate;

        private UnityTask _FrameTransitionTask;
        private WaitForSeconds _WaitForFrameTransition;
        #endregion

        #region MonoBehaviour
        protected virtual void Awake()
        {
            _Canvas.enabled = false;
            _WaitForFrameTransition = new WaitForSeconds(seconds: _FrameTransitionTime);
        }
        #endregion

        public void Show() => Show(options: null);
        public virtual void Show(Options options = null)
        {
            if (Utils.IsUnityTaskRunning(task: ref _FrameTransitionTask) || _Canvas.enabled)
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
                _FrameTransitionTask = new UnityTask(c: FrameTransitionAsync(isShow: true));
            }
            else
            {
                if (_OnShowSound != null)
                {
                    GameManager.Audio.PlaySFX(sfx: _OnShowSound);
                }
                _Canvas.enabled = true;
                _Frame.alpha = 1;
                _Frame.gameObject.LeanScale(to: Vector3.one * _FrameTransitionScaleRange.y, time: 0f);
                _Frame.interactable = true;
                _Canvas.enabled = true;
                _OnShow?.Invoke();

            }
        }

        public abstract void Populate();

        public virtual void Hide()
        {
            if (Utils.IsUnityTaskRunning(task: ref _FrameTransitionTask) || !_Canvas.enabled)
            {
                return;
            }

            if (_Animate)
            {
                _FrameTransitionTask = new UnityTask(c: FrameTransitionAsync(isShow: false));
            }
            else
            {
                if (_OnHideSound != null)
                {
                    GameManager.Audio.PlaySFX(sfx: _OnHideSound);
                }
                _Frame.alpha = 0;
                _Frame.gameObject.LeanScale(to: Vector3.one * _FrameTransitionScaleRange.x, time: 0f);
                _Frame.interactable = false;
                _Canvas.enabled = false;
                _OnHide?.Invoke();
            }
        }

        protected virtual IEnumerator FrameTransitionAsync(bool isShow)
        {
            _Frame.alpha = isShow ? 0f : 1f;

            _Frame.gameObject.LeanScale(to: Vector3.one * (isShow ? _FrameTransitionScaleRange.x : _FrameTransitionScaleRange.y), time: 0f);


            _Frame.interactable = false;
            _Canvas.enabled = true;

            _Frame.LeanAlpha(to: isShow ? _FrameAlphaRange.y : _FrameAlphaRange.x, time: _FrameTransitionTime);


            if (isShow)
            {
                _Frame.gameObject.LeanScale(to: Vector3.one * (isShow ? _FrameTransitionScaleRange.y : _FrameTransitionScaleRange.x),
               time: _FrameTransitionTime / 2f);
            }

            yield return _WaitForFrameTransition;

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
            GameManager.Audio.StopBGM(skipFadeOut: false);
        }
    }
}

