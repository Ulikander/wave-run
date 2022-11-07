using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using WASD.Runtime.Audio;

namespace WASD.Runtime.Managers
{
    public class AudioManager : MonoBehaviour
    {
        #region Properties
        public bool BgmMuted
        {
            get => _BgmAudioSource.mute;
            set
            {
                PlayerPrefs.SetInt(key: GameManager.cPprefBgmMuted, value: value ? 1 : 0);
                _BgmAudioSource.mute = value;
            }
        }
        public bool SfxMuted
        {
            get => _SfxAudioSource.mute;
            set
            {
                PlayerPrefs.SetInt(key: GameManager.cPprefSFXMuted, value: value ? 1 : 0);
                _SfxAudioSource.mute = value;
            }
        }
        #endregion

        #region Constants

        #endregion 

        #region Fields
        [SerializeField] private AudioMixer _AudioMixer;
        [SerializeField] private GameObject _AudioContainerObject;
        [SerializeField] private float _DefaultFadeInTime;
        [SerializeField] private float _DefaultFadeOutTime;

        private AudioSource _BgmAudioSource;
        private AudioContainer _CurrentBgm;
        private AudioSource _SfxAudioSource;

        private UnityTask _BgmFadeTask;
        //private WaitForSeconds _WaitForDefaultFadeIn;
        //private WaitForSeconds _WaitForDefaultFateOut;
        private UnityTask _BgmLoopTask;
        private WaitWhile _WaitForLoopEndReached;
        #endregion

        #region MonoBehaviour
        private void Awake()
        {
            _BgmAudioSource = _AudioContainerObject.AddComponent<AudioSource>();
            _BgmAudioSource.outputAudioMixerGroup = _AudioMixer.FindMatchingGroups(subPath: "BGM")[0];
            _SfxAudioSource = _AudioContainerObject.AddComponent <AudioSource>();
            _SfxAudioSource.outputAudioMixerGroup = _AudioMixer.FindMatchingGroups(subPath: "SFX")[0];

            //_WaitForDefaultFadeIn = new WaitForSeconds(seconds: _DefaultFadeInTime);
            //_WaitForDefaultFateOut = new WaitForSeconds(seconds: _DefaultFadeOutTime);
            _WaitForLoopEndReached = new WaitWhile(predicate: () => _BgmAudioSource.time < _CurrentBgm.LoopEndTime);
            HandlePlayerPrefs();
        }

        void HandlePlayerPrefs()
        {
            BgmMuted = PlayerPrefs.GetInt(key: GameManager.cPprefBgmMuted, defaultValue: 0) == 1;
            SfxMuted = PlayerPrefs.GetInt(key: GameManager.cPprefSFXMuted, defaultValue: 0) == 1;
        }

        public void StopBGM(bool skipFadeOut = true)
        {
            PlayBGM(bgm: null, skipFades: new bool[] { true, skipFadeOut });
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="audioContainer"></param>
        /// <param name="skipFades">[0] FadeIn, [1] FadeOut</param>
        public void PlayBGM(AudioContainer bgm, bool[] skipFades, bool randomizeStart = false, bool restartIfSame = false)
        {
            if(bgm != null && bgm.AudioType != Enums.AudioContainerType.BGM)
            {
                Debug.LogError(message: $"Tried to play SFX as a BGM, thats not correct. Stupeh!");
                return;
            }

            if(bgm != null && _CurrentBgm != null && bgm.Clip == _CurrentBgm.Clip && !restartIfSame)
            {
                Debug.Log(message: $"Tried to play a BGM that is already playing, but Restart If Same is false");
                return;
            }

            if (Utils.IsUnityTaskRunning(task: ref _BgmFadeTask))
            {
                if(bgm != null)
                {
                    Debug.LogError(message: "Tried to play a BGM but another one is currently Fading into the AudioSource");
                    return;
                }

                Utils.StopUnityTask(ref _BgmFadeTask);
            }

            _BgmFadeTask = new(c: FadeBgmRoutine(audioContainer: bgm, skipFades: skipFades, randomizeStart: randomizeStart));
        }

        private IEnumerator FadeBgmRoutine(AudioContainer audioContainer, bool[] skipFades, bool randomizeStart)
        {
            float counter = 0;

            if (!skipFades[1] && _BgmAudioSource.isPlaying)
            {
                float currentVolume = _BgmAudioSource.volume;
                while (counter < _DefaultFadeOutTime)
                {
                    counter += Time.deltaTime;
                    _BgmAudioSource.volume = Mathf.Lerp(a: currentVolume, b: 0f, t: counter / _DefaultFadeOutTime);
                    yield return null;
                }
            }

            _BgmAudioSource.Stop();
            _BgmAudioSource.volume = 0f;
            Utils.StopUnityTask(task: ref _BgmLoopTask);
            _BgmAudioSource.clip = audioContainer != null ? audioContainer.Clip : null;
            _CurrentBgm = audioContainer;

            if (audioContainer == null)
            {
                yield break;
            }

            if (randomizeStart && audioContainer.StartTimes.Length > 0)
            {
                _BgmAudioSource.time = audioContainer.StartTimes[Random.Range(minInclusive: 0, maxExclusive: audioContainer.StartTimes.Length)];
            }
            else
            {
                _BgmAudioSource.time = 0f;
            }

            _BgmAudioSource.loop = audioContainer.LoopType == Enums.AudioLoopType.Normal;
            _BgmAudioSource.Play();
            
            if(audioContainer.LoopType == Enums.AudioLoopType.Custom)
            {
                _BgmLoopTask = new UnityTask(c: LoopBgmRoutine());
            }

            if (!skipFades[0])
            {
                counter = 0;
                while (counter < _DefaultFadeInTime)
                {
                    counter += Time.deltaTime;
                    _BgmAudioSource.volume = Mathf.Lerp(a: 0, b: 1f, t: counter / _DefaultFadeInTime);
                    yield return null;
                }
            }

            _BgmAudioSource.volume = 1f;
        }

        private IEnumerator LoopBgmRoutine()
        {
            //float loopStartSamples = _CurrentBgm.Clip.frequency / _CurrentBgm.LoopStartTime;
            //float loopEndSamples = _CurrentBgm.Clip.frequency / _CurrentBgm.LoopEndTime;

            while (true)
            {
                yield return _WaitForLoopEndReached;
                _BgmAudioSource.time = _CurrentBgm.LoopStartTime;// loopStartSamples;
            }
        }

        public void PlaySFX(AudioContainer sfx, bool loop = false)
        {
            if (!loop)
            {
                _SfxAudioSource.PlayOneShot(clip: sfx.Clip);
            }
        }

        #endregion
    }
}

