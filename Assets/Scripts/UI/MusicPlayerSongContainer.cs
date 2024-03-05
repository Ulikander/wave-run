using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using WASD.Runtime.Audio;
using WASD.Runtime.Managers;

namespace WASD.Runtime.Gameplay
{
    [RequireComponent(typeof(Canvas))]
    [RequireComponent(typeof(GraphicRaycaster))]
    public class MusicPlayerSongContainer : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI artistText;
        [SerializeField] private Button playButton;
        [SerializeField] private Button sourceButton;
    
        private AudioContainer _Audio;
        private Canvas _Canvas;
        private GraphicRaycaster _GraphicRaycaster;

        public event Action OnClickPlayEvent;

        private void Awake()
        {
            _Canvas = GetComponent<Canvas>();
            _GraphicRaycaster = GetComponent<GraphicRaycaster>();
            Hide();
        }

        public void Show(AudioContainer audioContainer, int trackIndex, bool allowPlay)
        {
            _Audio = audioContainer;

            if (audioContainer.UnlockLevel > 0 && !GameManager.SaveData.IsLevelCleared(_Audio.UnlockLevel))
            {
                titleText.text = $"{trackIndex}.- {_Audio.LockedName}";
                artistText.text = $"Se desbloquea completando el nivel {_Audio.UnlockLevel}";
                playButton.gameObject.SetActive(false);
                sourceButton.gameObject.SetActive(false);
            }
            else
            {
                titleText.text = $"{trackIndex}.- {_Audio.Name}";
                artistText.text = _Audio.Author;
                playButton.gameObject.SetActive(true);
                playButton.interactable = GameManager.Audio.CurrentBgm != _Audio && allowPlay;
                sourceButton.gameObject.SetActive(!string.IsNullOrWhiteSpace(_Audio.SourceUrl));
            }
            
            _Canvas.enabled = true;
            _GraphicRaycaster.enabled = true;
        }

        public void Hide()
        {
            _Audio = null;
            _Canvas.enabled = false;
            _GraphicRaycaster.enabled = false;
        }

        public void OnClickPlay()
        {
            GameManager.Audio.PlayBgm(_Audio);
            OnClickPlayEvent?.Invoke();
        }

        public void OnClickSource()
        {
            Application.OpenURL(_Audio.SourceUrl);
        }
    }
}

