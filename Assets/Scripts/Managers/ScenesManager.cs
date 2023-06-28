using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

using WASD.Interfaces;


namespace WASD.Runtime.Managers
{
    public class ScenesManager : MonoBehaviour, IUseTasks
    {
        #region Constants
        public const string cSCENEID_INIT = "Init";
        public const string cSCENEID_MAINMENU = "MainMenu";
        public const string cSCENEID_GAMEPLAY = "GameplayCore";
        #endregion

        #region Fields
        [SerializeField] private Canvas _Canvas;
        [SerializeField] private CanvasGroup _MainCanvasGroup;
        [SerializeField] private CanvasGroup _TextAndSliderCanvasGroup;
        [SerializeField] private Slider _ProgressSlider;
        [SerializeField] private float _BgFadeTime = 0.75f;
        [SerializeField] private float _TextAndSliderFadeTime = .35f;

        private readonly Queue<string> _QueuedScenes = new(capacity: 1);
        private CancellationTokenSource _LoadSceneCancelToken;
        
        #endregion

        #region MonoBehaviour
        private void Awake()
        {
            _Canvas.enabled = false;
        }
        #endregion

        public void LoadScene(string sceneId, bool doSynchronously = false)
        {
            if(sceneId == SceneManager.GetActiveScene().name)
            {
                Debug.LogError(message: "Tried to load the currently Active Scene! Stupeh!");
                return;
            }

            if (Utils.IsCancelTokenSourceActive(ref _LoadSceneCancelToken))
            {
                if(_QueuedScenes.Count == 0)
                {
                    _QueuedScenes.Enqueue(item: sceneId);
                }
                else
                {
                    Debug.LogError(message: "Tried to queue more than one scene! Stupeh!");
                    return;
                }
            }
            
            if (doSynchronously)
            {
                SceneManager.LoadScene(sceneName: sceneId);
            }
            else
            {
                LoadSceneAsync(sceneId: sceneId);
            }

        }


        private async void LoadSceneAsync(string sceneId)
        {
            _LoadSceneCancelToken = new CancellationTokenSource();
            _MainCanvasGroup.alpha = 0f;
            _TextAndSliderCanvasGroup.alpha = 0f;
            _ProgressSlider.value = 0f;

            _Canvas.enabled = true;

            DOTween.To(() => _MainCanvasGroup.alpha, x => _MainCanvasGroup.alpha = x, 1f, _BgFadeTime);
            await UniTask.Delay((int)(_BgFadeTime * 1000), cancellationToken: _LoadSceneCancelToken.Token)
                .SuppressCancellationThrow();

            DOTween.To(() => _TextAndSliderCanvasGroup.alpha, x => _TextAndSliderCanvasGroup.alpha = x, 1f,
                _TextAndSliderFadeTime);
            await UniTask.Delay((int)(_TextAndSliderFadeTime * 1000), cancellationToken: _LoadSceneCancelToken.Token)
                .SuppressCancellationThrow();

            AsyncOperation asyncSceneLoading = SceneManager.LoadSceneAsync(sceneName: sceneId);
            asyncSceneLoading.allowSceneActivation = false;

            while (asyncSceneLoading.progress < 0.9f)
            {
                await UniTask.Yield(_LoadSceneCancelToken.Token).SuppressCancellationThrow();
                _ProgressSlider.value = asyncSceneLoading.progress;
            }

            _ProgressSlider.value = 1f;

            DOTween.To(() => _TextAndSliderCanvasGroup.alpha, x => _TextAndSliderCanvasGroup.alpha = x, 0f,
                _TextAndSliderFadeTime);
            await UniTask.Delay((int)(_TextAndSliderFadeTime * 1000), cancellationToken: _LoadSceneCancelToken.Token);

            asyncSceneLoading.allowSceneActivation = true;

            await UniTask
                .WaitUntil(predicate: () => asyncSceneLoading.isDone, cancellationToken: _LoadSceneCancelToken.Token)
                .SuppressCancellationThrow();
            GameManager.RefreshMainCamera();

            DOTween.To(() => _MainCanvasGroup.alpha, x => _MainCanvasGroup.alpha = x, 0f, _BgFadeTime);
            await UniTask.Delay((int)(_BgFadeTime * 1000), cancellationToken: _LoadSceneCancelToken.Token);

            _Canvas.enabled = false;

            Utils.CancelTokenSourceRequestCancelAndDispose(ref _LoadSceneCancelToken);
            if (_QueuedScenes.Count != 0)
            {
                LoadScene(sceneId: _QueuedScenes.Dequeue());
            }
        }

        public void StopAllTasks()
        {
            Utils.CancelTokenSourceRequestCancelAndDispose(ref _LoadSceneCancelToken);
        }
    }
}

