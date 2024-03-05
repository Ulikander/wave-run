using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WASD.Enums;

namespace WASD.Runtime.Audio
{
    [CreateAssetMenu(fileName = "AudioContainer", menuName = "WASD/Create Audio Container")]
    public class AudioContainer : ScriptableObject
    {
        public AudioContainerType AudioType;
        public AudioClip Clip;
        public AudioLoopType LoopType;
        public float LoopStartTime;
        public float LoopEndTime;
        public float[] StartTimes;
        public string Name;
        public string Author;
        public string SourceUrl;
        public string LockedName;
        public int UnlockLevel;
    }
}

