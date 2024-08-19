using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WASD.Runtime.Levels;
using WASD.Runtime.Managers;

namespace WASD.Runtime.Popups
{
    public class LevelSelectorPopup : BasePopup
    {
        #region Fields
        [SerializeField] private LevelInfoDisplayButton[] _LevelInfoDisplayButtons;
        [SerializeField] private LevelInformation[] _AllLevelsInformation;
        #endregion

        public override void Populate()
        {
            base.Populate();
            for(int i = 0; i < _LevelInfoDisplayButtons.Length; i++)
            {
                if(i >= _AllLevelsInformation.Length)
                {
                    _LevelInfoDisplayButtons[i].Hide();
                    continue;
                }

                _LevelInfoDisplayButtons[i].Populate(info: _AllLevelsInformation[i], onSelect: PlayLevel);
            }
        }

        public void PlayLevel(LevelInformation info)
        {
            _Frame.interactable = false;
            GameManager.CurrentCoreLevel = info.CoreLevelValue;
            GameManager.CurrentCoreLevelRetries = 0;
            GameManager.Scenes.LoadScene(sceneId: ScenesManager.cSCENEID_GAMEPLAY);
        }
    }
}

