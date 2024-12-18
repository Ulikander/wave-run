using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

using WASD.Runtime.Levels;
using WASD.Runtime.Managers;

namespace WASD.Runtime
{
    public class LevelInfoDisplayButton : MonoBehaviour
    {
        #region Fields
        [SerializeField] private Button _MainButton;
        [SerializeField] private TextMeshProUGUI _LevelText;
        [SerializeField] private TextMeshProUGUI _RetriesText;
        [SerializeField] private Image _FrameImage;
        [SerializeField] private Color _FrameLockedColor;
        [SerializeField] private Color _FrameUnlockedColor;
        [SerializeField] private Image _LockedImage;

        private LevelInformation _LevelInfo;
        #endregion

        #region Events
        private Action<LevelInformation> _OnSelect;
        #endregion

        public void Populate(LevelInformation info, Action<LevelInformation> onSelect)
        {
            _OnSelect = onSelect;

            gameObject.SetActive(value: true);
            _LevelInfo = info;

            _LevelText.text = $"Nivel {info.CoreLevelValue}";

            bool isLevelUnlocked = _LevelInfo.CoreLevelValue == 1 ||
                                   GameManager.SaveData.IsLevelCleared(_LevelInfo.CoreLevelValue -1);
            int retryCount = GameManager.SaveData.LevelRetryCount(_LevelInfo.CoreLevelValue);
            
            _LockedImage.enabled = !isLevelUnlocked;
            _FrameImage.color = _LockedImage.enabled ? _FrameLockedColor : _FrameUnlockedColor;
            _MainButton.interactable = !_LockedImage.enabled;

            _RetriesText.text = !isLevelUnlocked ? "Bloqueado" :
                retryCount == -1 ? "Sin Terminar" :
                retryCount == 0 ? "Perfecto" :
                $"Reintentos: {retryCount}";
        }

        public void Hide()
        {
            _LevelInfo = null;
            gameObject.SetActive(value: false);
        }

        public void OnMainButtonClick()
        {
            _OnSelect?.Invoke(obj: _LevelInfo);
        }
    }
}
