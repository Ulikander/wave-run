using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using WASD.Runtime.Levels;

namespace WASD.Runtime
{
    public class LevelInfoDisplayButton : MonoBehaviour
    {
        #region Fields
        [SerializeField] private Button _MainButton;
        [SerializeField] private TextMeshProUGUI _LevelText;
        [SerializeField] private Image _FrameImage;
        [SerializeField] private Color _FrameLockedColor;
        [SerializeField] private Color _FrameUnlockedColor;
        [SerializeField] private Image _LockedImage;

        private LevelInformation _LevelInfo;
        #endregion

        public void Populate(LevelInformation info)
        {
            gameObject.SetActive(value: true);
            _LevelInfo = info;

            _LevelText.text = $"Nivel {info.CoreLevelValue}";

            _LockedImage.enabled = GameManager.LastCoreLevelUnlocked < _LevelInfo.CoreLevelValue;
            _FrameImage.color = _LockedImage.enabled ? _FrameLockedColor : _FrameUnlockedColor;
            _MainButton.interactable = !_LockedImage.enabled;
        }

        public void Hide()
        {
            _LevelInfo = null;
            gameObject.SetActive(value: false);
        }

        public void OnMainButtonClick()
        {

        }
    }
}