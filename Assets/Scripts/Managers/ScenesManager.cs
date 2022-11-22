using System.Collections;
using System.Collections.Generic;
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

        private Queue<string> _QueuedScenes = new Queue<string>(capacity: 1);
        private UnityTask _LoadSceneCoroutine;
        private WaitForSeconds _WaitForBackgroundFade;
        private WaitForSeconds _WaitForTextFade;
        //private string _QueuedScene;
        #endregion

        #region MonoBehaviour
        private void Awake()
        {
            _Canvas.enabled = false;
            _WaitForBackgroundFade = new WaitForSeconds(seconds: _BgFadeTime);
            _WaitForTextFade = new WaitForSeconds(seconds: _TextAndSliderFadeTime);
        }
        #endregion

        public void LoadScene(string sceneId, bool doSynchronously = false)
        {
            if(sceneId == SceneManager.GetActiveScene().name)
            {
                Debug.LogError(message: "Tried to load the currently Active Scene! Stupeh!");
                return;
            }

            if (Utils.IsUnityTaskRunning(task: ref _LoadSceneCoroutine))
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
                _LoadSceneCoroutine = new UnityTask(c: LoadSceneAsync(sceneId: sceneId));
            }

        }


        private IEnumerator LoadSceneAsync(string sceneId)
        {
            _MainCanvasGroup.alpha = 0f;
            _TextAndSliderCanvasGroup.alpha = 0f;
            _ProgressSlider.value = 0f;

            _Canvas.enabled = true;

        _MainCanvasGroup.LeanAlpha(to: 1f, time: _BgFadeTime);
            yield return _WaitForBackgroundFade;

        _TextAndSliderCanvasGroup.LeanAlpha(to: 1f, time: _TextAndSliderFadeTime);
            yield return _WaitForTextFade;

            AsyncOperation asyncSceneLoading = SceneManager.LoadSceneAsync(sceneName: sceneId);
            asyncSceneLoading.allowSceneActivation = false;

            while (asyncSceneLoading.progress < 0.9f)
            {
                yield return null;
                _ProgressSlider.value = asyncSceneLoading.progress;
            }

            _ProgressSlider.value = 1f;

            _TextAndSliderCanvasGroup.LeanAlpha(to: 0f, time: _TextAndSliderFadeTime);
            yield return _WaitForTextFade;

            asyncSceneLoading.allowSceneActivation = true;

            yield return new WaitUntil(predicate: () => asyncSceneLoading.isDone);
            GameManager.RefreshMainCamera();

            _MainCanvasGroup.LeanAlpha(to: 0f, time: _BgFadeTime);
            yield return _WaitForBackgroundFade;

            _Canvas.enabled = false;

            if (_QueuedScenes.Count != 0)
            {
                _LoadSceneCoroutine = null;
                LoadScene(sceneId: _QueuedScenes.Dequeue());
            }
        }

        public void StopAllTasks()
        {
            Utils.StopUnityTask(task: ref _LoadSceneCoroutine);
        }
    }
}

