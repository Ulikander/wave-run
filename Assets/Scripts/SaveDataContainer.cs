using System;
using System.Collections.Generic;
using Newtonsoft.Json;
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

            public Dictionary<int, bool> clearedLevels;
            public bool hasExtrasUnlocked;

            #endregion

            public SaveData()
            {
                clearedLevels = new Dictionary<int, bool>();
                hasExtrasUnlocked = false;
            }
        }

        #endregion

        #region Constants

        public static string CPprefSaveData = "savedata";

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
            string json = JsonConvert.SerializeObject(_SaveData);
            PlayerPrefs.SetString(CPprefSaveData, json);
            Debug.LogWarning("Saved player data.");
        }

        public bool TryPerformLoad(out SaveData data)
        {
            string json = PlayerPrefs.GetString(CPprefSaveData);
            data = null;
            if (string.IsNullOrEmpty(json))
            {
                Debug.Log("No player data to load");
                return false;
            }

            bool loadWasSuccessful = false;
            try
            {
                data = JsonConvert.DeserializeObject<SaveData>(json);
                
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
            return _SaveData.clearedLevels.ContainsKey(levelIndex) && _SaveData.clearedLevels[levelIndex];
        }

        public void SetLevelClearState(int levelIndex, bool isCleared)
        {
            if (!_SaveData.clearedLevels.ContainsKey(levelIndex))
            {
                _SaveData.clearedLevels.Add(levelIndex, isCleared);
            }
            else
            {
                _SaveData.clearedLevels[levelIndex] = isCleared;
            }
            
            PerformSave();
        }
    }
}

