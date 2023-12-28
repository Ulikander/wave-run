using System;
using System.Collections.Generic;
using UnityEngine;

namespace WASD.Data
{
    public class SaveDataContainer
    {
        #region Classes
        [Serializable]
        public class SaveData
        {
            #region Fields

            public List<bool> clearedLevels;
            public bool hasExtrasUnlocked;

            #endregion

            public SaveData()
            {
                clearedLevels = new List<bool>();
                hasExtrasUnlocked = false;
            }
        }

        #endregion

        #region Constants

        private const string CPprefSaveData = "savedata";

        #endregion

        #region Fields

        private SaveData _SaveData;
        
        #endregion
        
        
        public SaveDataContainer()
        {
            if (!TryPerformLoad(out _SaveData))
            {
                _SaveData = new SaveData();
            }
        }

        public void PerformSave()
        {
            string json = JsonUtility.ToJson(_SaveData);
            PlayerPrefsUtility.SetEncryptedString(CPprefSaveData, json);
            Debug.LogWarning("Saved player data.");
        }

        public bool TryPerformLoad(out SaveData data)
        {
            string json = PlayerPrefsUtility.GetEncryptedString(CPprefSaveData);
            data = null;
            if (string.IsNullOrEmpty(json))
            {
                Debug.Log("No player data to load");
                return false;
            }

            bool loadWasSuccessful = false;
            try
            {
                data = JsonUtility.FromJson<SaveData>(json);//JsonConvert.DeserializeObject<SaveData>(json);
                
                Debug.LogWarning("Loaded player data.");
                loadWasSuccessful = true;
            }
            catch (Exception e)
            {
                Debug.LogError(e);
                Debug.LogError("Could not parse player data");
            }
            
            return loadWasSuccessful;
        }

        public bool IsLevelCleared(int levelIndex)
        {
            if (levelIndex >= _SaveData.clearedLevels.Count) return false;
            return _SaveData.clearedLevels[levelIndex];
        }

        public void SetLevelClearState(int levelIndex, bool isCleared)
        {
            ValidateSaveState(levelIndex);
            _SaveData.clearedLevels[levelIndex] = isCleared;
            PerformSave();
        }

        private void ValidateSaveState(int index)
        {
            while (_SaveData.clearedLevels.Count <= index)
            {
                _SaveData.clearedLevels.Add(false);
            }
        }
    }
}

