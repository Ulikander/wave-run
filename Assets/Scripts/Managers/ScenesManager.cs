using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

using WASD.Interfaces;
using Zenject;

namespace WASD.Runtime.Managers
{
    public class ScenesManager : MonoBehaviour, IUseTasks
    {
        #region Constants
        public const string cSCENEID_INIT = "Init";
        public const string cSCENEID_MAINMENU = "MainMenu";
        #endregion

        #region Fields
        [SerializeField] private Canvas _Canvas;
        [SerializeField] private CanvasGroup _MainCanvasGroup;
        [SerializeField] private CanvasGroup _TextAndSliderCanvasGroup;
        [SerializeField] private Slider _ProgressSlider;
        [SerializeField] private float _BgFadeTime = 0.75f;
        [SerializeField] private float _TextAndSliderFadeTime = .35f;

        private UnityTask _LoadSceneCoroutine;
        private WaitForSeconds _WaitForBackgroundFade;
        private WaitForSeconds _WaitForTextFade;

        [Inject] private readonly TaskManager _TaskManager;
        [Inject] private readonly GameManager _GameManager;
        [Inject] private readonly ZenjectSceneLoader _ZenjectSceneLoader;
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

            if(Utils.IsUnityTaskRunning(task: ref _LoadSceneCoroutine))
            {
                Debug.LogError(message: "Tried to load a scene when another is being loaded! Stupeh!");
                return;
            }

            if (doSynchronously)
            {
                _ZenjectSceneLoader.LoadScene(sceneName: sceneId, extraBindings: ExtraBindings);
            }
            else
            {
                _LoadSceneCoroutine = new UnityTask(
                    manager: _TaskManager,
                    c: LoadSceneAsync(sceneId: sceneId));
            }

        }

        private void ExtraBindings(DiContainer container)
        {
            container.Bind<ScenesManager>();
            container.Bind<AudioManager>();
        }

        private IEnumerator LoadSceneAsync(string sceneId)
        {
            _MainCanvasGroup.alpha = 0f;
            _TextAndSliderCanvasGroup.alpha = 0f;
            _ProgressSlider.value = 0f;

            _Canvas.enabled = true;
            
            LeanTween.alphaCanvas(canvasGroup: _MainCanvasGroup, to: 1f, time: _BgFadeTime);
            yield return _WaitForBackgroundFade;

            LeanTween.alphaCanvas(canvasGroup: _TextAndSliderCanvasGroup, to: 1f, time: _TextAndSliderFadeTime);
            yield return _WaitForTextFade;

            AsyncOperation asyncSceneLoading = _ZenjectSceneLoader.LoadSceneAsync(sceneName: sceneId, extraBindings: ExtraBindings);
            asyncSceneLoading.allowSceneActivation = false;

            while(asyncSceneLoading.progress < 0.9f)
            {
                yield return null;
                _ProgressSlider.value = asyncSceneLoading.progress;
            }

            _ProgressSlider.value = 1f;

            LeanTween.alphaCanvas(canvasGroup: _TextAndSliderCanvasGroup, to: 0f, time: _TextAndSliderFadeTime);
            yield return _WaitForTextFade;

            asyncSceneLoading.allowSceneActivation = true;
            _GameManager.RefreshMainCamera();

            LeanTween.alphaCanvas(canvasGroup: _MainCanvasGroup, to: 0f, time: _BgFadeTime);
            yield return _WaitForBackgroundFade;

            _Canvas.enabled = false;
        }

        public void StopAllTasks()
        {
            Utils.StopUnityTask(task: ref _LoadSceneCoroutine);
        }
    }
}

