using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
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
        [SerializeField] private MusicPlayerSongContainer[] musicContainers;
        [SerializeField] private Button musicPrevButton;
        [SerializeField] private Button musicNextButton;
        [SerializeField] private Button goToCreditsButton;
        [SerializeField] private Button goToMainMenuButton;
        [Space(10)]
        [SerializeField] private UnityEvent onReturnToCredits;
        [SerializeField] private UnityEvent onReturnToMainMenu;
        
        private int musicCurrentPage;
        private bool canPressPlayAgain;
        private List<AudioContainer> musicList;
        private CancellationTokenSource playDelayCancelToken;

        private float playPressDelay;

        protected override void Awake()
        {
            canPressPlayAgain = true;
            playPressDelay = GameManager.Audio.DefaultFadeOutTime + 0.1f;
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

            playDelayCancelToken =
                CancellationTokenSource.CreateLinkedTokenSource(this.GetCancellationTokenOnDestroy());
            canPressPlayAgain = true;
            
            musicCurrentPage = 0;
            ShowVisibleTrackContainers();
        }

        public override void Hide()
        {
            base.Hide();
            Utils.CancelTokenSourceRequestCancelAndDispose(ref playDelayCancelToken);
            
            string menuMusic = GameManager.SaveData.MainMenuMusic;
            if (menuMusic != "" && GameManager.Audio.CurrentBgm.Name != menuMusic)
            {
                AudioContainer bgm = GameManager.MusicList.First(m => m.Name == menuMusic);
                GameManager.Audio.PlayBgm(bgm, randomizeStart: true, fadeOutTime: 0.2f);
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

        private void ShowVisibleTrackContainers()
        {
            foreach (MusicPlayerSongContainer container in musicContainers)
            {
                container.Hide();
            }
            musicPrevButton.interactable = musicCurrentPage > 0;
            musicNextButton.interactable =
                musicCurrentPage < Mathf.FloorToInt(musicList.Count / (float)musicContainers.Length);

           
            for (int i = 0; i < musicContainers.Length; i++)
            {
                int trueIndex = i + (musicCurrentPage * musicContainers.Length);
                if (trueIndex >= musicList.Count) break;
                musicContainers[i].Show(musicList[trueIndex], trueIndex + 1, canPressPlayAgain);
            }
        }

        private void HandlePlayBgmButton()
        {
            if (!canPressPlayAgain) return;
            HandlePlayBgmButtonAsync(playDelayCancelToken.Token).Forget();
        }

        private async UniTaskVoid HandlePlayBgmButtonAsync(CancellationToken cancelToken)
        {
            canPressPlayAgain = false;
            goToCreditsButton.interactable = false;
            goToMainMenuButton.interactable = false;
            ShowVisibleTrackContainers();

            await Utils.UniTaskDelay(playPressDelay, cancelToken);

            canPressPlayAgain = true;
            goToCreditsButton.interactable = true;
            goToMainMenuButton.interactable = true;
            ShowVisibleTrackContainers();
        }
    }
}