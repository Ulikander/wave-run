using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using WASD.Runtime.Audio;
using WASD.Runtime.Gameplay;
using WASD.Runtime.Managers;

namespace WASD.Runtime.Popups
{
    public class ExtrasPopup : BasePopup
    {
        [Header("Music Select")]
        [SerializeField] private MusicPlayerSongContainer[] musicContainers;
        [SerializeField] private Button musicPrevButton;
        [SerializeField] private Button musicNextButton;
        [SerializeField] private Button goToCreditsButton;
        [SerializeField] private Button goToMainMenuButton;

        [Header("Music Player")]
        [SerializeField] private Slider playerTimeSlider;
        [SerializeField] private TextMeshProUGUI playerSongName;
        [SerializeField] private TextMeshProUGUI playerCurrentTime;
        [SerializeField] private TextMeshProUGUI playerTotalTime;
        [SerializeField] private Button playerAssignMainMenuButton;
        [SerializeField] private Button playerBrowserButton;
        [SerializeField] private Button[] playerButtons;
        
        [Space(10)]
        [SerializeField] private UnityEvent onReturnToCredits;
        [SerializeField] private UnityEvent onReturnToMainMenu;
        
        private int musicCurrentPage;
        private bool canPressPlayAgain;
        private List<AudioContainer> musicList;
        private CancellationTokenSource localCancelToken;
        private AudioContainer _CurrentTrack;

        protected override void Awake()
        {
            canPressPlayAgain = true;
            base.Awake();
            musicList = new List<AudioContainer>(GameManager.MusicList);
            
            foreach (MusicPlayerSongContainer container in musicContainers)
            {
                container.OnClickPlayEvent += HandlePlayBgmButton;
            }
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            foreach (MusicPlayerSongContainer container in musicContainers)
            {
                container.OnClickPlayEvent -= HandlePlayBgmButton;
            }
        }
        public override void Populate()
        {
            base.Populate();
            _Animate = true;

            localCancelToken =
                CancellationTokenSource.CreateLinkedTokenSource(this.GetCancellationTokenOnDestroy());
            canPressPlayAgain = true;
            
            _CurrentTrack = GameManager.Audio.CurrentBgm;
            HandledUpdateAsync(localCancelToken.Token).Forget();
            SetAllButtonsInteractable(true);
            SetAllBgmDependantObjects(false);
            
            musicCurrentPage = 0;
            ShowVisibleTrackContainers();
        }

        public override void Hide()
        {
            base.Hide();
            Utils.CancelTokenSourceRequestCancelAndDispose(ref localCancelToken);
            
            foreach (MusicPlayerSongContainer container in musicContainers)
            {
                container.Hide();
            }
            
            string menuMusic = GameManager.SaveData.MainMenuMusic;
            if (menuMusic != "" && GameManager.Audio.CurrentBgm.Name != menuMusic)
            {
                AudioContainer bgm = GameManager.MusicList.First(m => m.Name == menuMusic);
                GameManager.Audio.PlayBgm(bgm, skipFadeOut: true, randomizeStart: true, fadeOutTime: 0.2f);
            }
        }

        public void OnClickMusicNavigationButton(string direction)
        {
            switch (direction)
            {
                case "prev":
                    musicCurrentPage--;
                    break;
                case "next":
                    musicCurrentPage++;
                    break;
            }
            ShowVisibleTrackContainers();
        }

        public void OnClickReturnToCreditsButton()
        {
            if (!canPressPlayAgain) return;
            onReturnToCredits.Invoke();
        }
        
        public void OnClickReturnToMainMenuButton()
        {
            if (!canPressPlayAgain) return;
            onReturnToMainMenu.Invoke();
        }

        public void OnClickRestart(bool random)
        {
            if (!canPressPlayAgain) return;
            GameManager.Audio.PlayBgm(_CurrentTrack, restartIfSame: true, randomizeStart: random, fadeOutTime: random ? 0.15f : 0f);
            HandlePlayBgmButton(_CurrentTrack);
        }

        public void OnClickBrowser()
        {
            Application.OpenURL(_CurrentTrack.SourceUrl);
        }

        public void OnClickAssignToMainMenu()
        {
            GameManager.SaveData.SetMainMenuMusic(_CurrentTrack);
            SetAllBgmDependantObjects(false);
        }

        private void ShowVisibleTrackContainers()
        {
            musicPrevButton.interactable = musicCurrentPage > 0;
            musicNextButton.interactable =
                musicCurrentPage < Mathf.FloorToInt(musicList.Count / (float)musicContainers.Length);
           
            for (int i = 0; i < musicContainers.Length; i++)
            {
                int trueIndex = i + (musicCurrentPage * musicContainers.Length);
                if (trueIndex >= musicList.Count)
                {
                    musicContainers[i].Hide();
                    continue;
                }
                musicContainers[i].Show(musicList[trueIndex], trueIndex + 1, canPressPlayAgain);
            }
        }

        private void HandlePlayBgmButton(AudioContainer audioContainer)
        {
            if (!canPressPlayAgain) return;
            canPressPlayAgain = false;
            _CurrentTrack = audioContainer;
        }

        private async UniTaskVoid HandledUpdateAsync(CancellationToken cancelToken)
        {
            while (!cancelToken.IsCancellationRequested)
            {
                if (!canPressPlayAgain)
                {
                    playerSongName.text = "Cambiando...";
                    
                    SetAllButtonsInteractable(false);
                    SetAllBgmDependantObjects(true);
                    ShowVisibleTrackContainers();

                    await UniTask.WaitUntil(() => GameManager.Audio.CurrentBgm == _CurrentTrack, cancellationToken: cancelToken).SuppressCancellationThrow();
                    await Utils.UniTaskDelay(0.1f, cancelToken).SuppressCancellationThrow();
                    await UniTask.NextFrame(cancelToken).SuppressCancellationThrow();

                    SetAllButtonsInteractable(true);
                    SetAllBgmDependantObjects(false);
                    canPressPlayAgain = true;
                    ShowVisibleTrackContainers();
                }
                
                if (_CurrentTrack != null)
                {
                    float currentTime = GameManager.Audio.CurrentBgmTime;
                    float totalTime = _CurrentTrack.Clip.length;
                    playerTimeSlider.value = currentTime/totalTime;

                    int convertTimeMinutes = Mathf.FloorToInt(currentTime / 60f);
                    int convertTimeSeconds = (int)(currentTime - (convertTimeMinutes * 60));
                    playerCurrentTime.text = $"{convertTimeMinutes:00}:{convertTimeSeconds:00}";
                }

                await UniTask.Yield();
            }
        }

        private void SetAllButtonsInteractable(bool value)
        {
            goToCreditsButton.interactable = value;
            goToMainMenuButton.interactable = value;
            foreach (Button button in playerButtons)
            {
                button.interactable = value;
            }
        }

        private void SetAllBgmDependantObjects(bool forceFalse)
        {
            if (_CurrentTrack == null || forceFalse)
            {
                playerBrowserButton.interactable = false;
                playerAssignMainMenuButton.interactable = false;
                return;
            }

            playerBrowserButton.interactable = !string.IsNullOrWhiteSpace(_CurrentTrack.SourceUrl);
            playerAssignMainMenuButton.interactable = _CurrentTrack.Name != GameManager.SaveData.MainMenuMusic;
            
            playerSongName.text = $"{_CurrentTrack.Name}<size=60%><color=yellow> {_CurrentTrack.Author}";
            float totalTime = _CurrentTrack.Clip.length;
            int convertTimeMinutes = Mathf.FloorToInt(totalTime / 60f); 
            int convertTimeSeconds = (int)(totalTime - (convertTimeMinutes * 60));
            playerTotalTime.text = $"{convertTimeMinutes:00}:{convertTimeSeconds:00}";
        }
    }
}