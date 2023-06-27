#region Using

using System.Threading;
using Cysharp.Threading.Tasks;

using UnityEngine;
using UnityEngine.Audio;
using Random = UnityEngine.Random;

using WASD.Runtime.Audio;

#endregion

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
        [SerializeField] private float _DefaultPitchFadeSpeed;

        private float _TargetPitch;

        private AudioSource _BgmAudioSource;
        private AudioContainer _CurrentBgm;
        private AudioSource _SfxAudioSource;

        private CancellationTokenSource _PitchFadeTokenSource;
        private CancellationTokenSource _BgmFadeTokenSource;
        private CancellationTokenSource _BgmLoopTokenSource;
        
        #endregion

        #region MonoBehaviour
        private void Awake()
        {
            _BgmAudioSource = _AudioContainerObject.AddComponent<AudioSource>();
            _BgmAudioSource.outputAudioMixerGroup = _AudioMixer.FindMatchingGroups(subPath: "BGM")[0];
            _SfxAudioSource = _AudioContainerObject.AddComponent <AudioSource>();
            _SfxAudioSource.outputAudioMixerGroup = _AudioMixer.FindMatchingGroups(subPath: "SFX")[0];
            HandlePlayerPrefs();
        }

        private void OnDestroy()
        {
            Utils.CancelTokenSourceRequestCancelAndDispose(ref _PitchFadeTokenSource);
            Utils.CancelTokenSourceRequestCancelAndDispose(ref _BgmFadeTokenSource);
            Utils.CancelTokenSourceRequestCancelAndDispose(ref _BgmLoopTokenSource);
        }

        #endregion

        void HandlePlayerPrefs()
        {
            BgmMuted = PlayerPrefs.GetInt(key: GameManager.cPprefBgmMuted, defaultValue: 0) == 1;
            SfxMuted = PlayerPrefs.GetInt(key: GameManager.cPprefSFXMuted, defaultValue: 0) == 1;
        }

        public void FadeBgmPitch(float target, bool immediate = false)
        {
            _TargetPitch = target;

            if (immediate)
            {
                _BgmAudioSource.pitch = _TargetPitch;
                return;
            }

            //if (!_BgmAudioSource.isPlaying) return;
            if(_BgmAudioSource.pitch != _TargetPitch && !Utils.IsCancelTokenSourceActive(ref _PitchFadeTokenSource))
            {
                _PitchFadeTokenSource = new CancellationTokenSource();
                BgmPitchFadeTask(_PitchFadeTokenSource.Token);
            }
        }

        private async void BgmPitchFadeTask(CancellationToken cancelToken)
        {
            float direction;
            while (!cancelToken.IsCancellationRequested && _BgmAudioSource.pitch != _TargetPitch)
            {
                direction = _BgmAudioSource.pitch > _TargetPitch ? -1f : _BgmAudioSource.pitch < _TargetPitch ? 1f : 0;
                _BgmAudioSource.pitch += _DefaultPitchFadeSpeed * Time.deltaTime * direction;
                if ((direction >= 1 && _BgmAudioSource.pitch > _TargetPitch) ||
                    (direction <= -1 && _BgmAudioSource.pitch < _TargetPitch))
                {
                    _BgmAudioSource.pitch = _TargetPitch;
                }

                await UniTask.Yield(cancelToken).SuppressCancellationThrow();
            }
        }

        public void StopBgm(bool skipFadeOut = true)
        {
            PlayBgm(bgm: null, skipFadeOut: skipFadeOut);
        }

        public void PlayBgm(AudioContainer bgm, bool skipFadeIn = false, bool skipFadeOut = false, bool randomizeStart = false, bool restartIfSame = false)
        {
            if (bgm != null && bgm.AudioType != Enums.AudioContainerType.BGM)
            {
                Debug.LogError(message: $"Tried to play SFX as a BGM, thats not correct. Stupeh!");
                return;
            }

            if (bgm != null && _CurrentBgm != null && bgm.Clip == _CurrentBgm.Clip && !restartIfSame)
            {
                Debug.Log(message: $"Tried to play a BGM that is already playing, but Restart If Same is false");
                return;
            }

            if (Utils.IsCancelTokenSourceActive(ref _BgmFadeTokenSource))
            {
                if (bgm != null)
                {
                    Debug.LogError(message: "Tried to play a BGM but another one is currently Fading into the AudioSource");
                    return;
                }

                Utils.CancelTokenSourceRequestCancelAndDispose(ref _BgmFadeTokenSource);
            }

            _BgmFadeTokenSource = new CancellationTokenSource();
            FadeBgmRoutine(bgm, skipFadeIn, skipFadeOut, randomizeStart, _BgmFadeTokenSource.Token);
        }

        private async void FadeBgmRoutine(AudioContainer audioContainer, bool skipFadeIn, bool skipFadeOut,
            bool randomizeStart, CancellationToken cancelToken)
        {
            float counter = 0;

            if (!skipFadeOut && _BgmAudioSource.isPlaying)
            {
                float currentVolume = _BgmAudioSource.volume;
                while (!cancelToken.IsCancellationRequested && counter < _DefaultFadeOutTime)
                {
                    counter += Time.deltaTime;
                    _BgmAudioSource.volume = Mathf.Lerp(a: currentVolume, b: 0f, t: counter / _DefaultFadeOutTime);
                    await UniTask.Yield(cancelToken).SuppressCancellationThrow();
                }
            }

            _BgmAudioSource.Stop();
            _BgmAudioSource.volume = 0f;
            Utils.CancelTokenSourceRequestCancelAndDispose(ref _BgmLoopTokenSource);
            _BgmAudioSource.clip = audioContainer != null ? audioContainer.Clip : null;
            _CurrentBgm = audioContainer;

            if (audioContainer == null)
            {
                return;
            }

            if (randomizeStart && audioContainer.StartTimes.Length > 0)
            {
                _BgmAudioSource.time =
                    audioContainer.StartTimes[
                        Random.Range(minInclusive: 0, maxExclusive: audioContainer.StartTimes.Length)];
            }
            else
            {
                _BgmAudioSource.time = 0f;
            }

            _BgmAudioSource.loop = audioContainer.LoopType == Enums.AudioLoopType.Normal;
            _BgmAudioSource.Play();

            if (audioContainer.LoopType == Enums.AudioLoopType.Custom)
            {
                _BgmLoopTokenSource = new CancellationTokenSource();
                LoopBgmRoutine(_BgmLoopTokenSource.Token);
            }

            if (!skipFadeIn)
            {
                counter = 0;
                while (!cancelToken.IsCancellationRequested && counter < _DefaultFadeInTime)
                {
                    counter += Time.deltaTime;
                    _BgmAudioSource.volume = Mathf.Lerp(a: 0, b: 1f, t: counter / _DefaultFadeInTime);
                    await UniTask.Yield(cancelToken).SuppressCancellationThrow();
                }
            }

            _BgmAudioSource.volume = 1f;
        }

        private async void LoopBgmRoutine(CancellationToken cancelToken)
        {
            while (!cancelToken.IsCancellationRequested)
            {
                await UniTask.WaitWhile(() => _BgmAudioSource.time < _CurrentBgm.LoopEndTime,
                    cancellationToken: cancelToken).SuppressCancellationThrow();
                if (_BgmAudioSource != null) _BgmAudioSource.time = _CurrentBgm.LoopStartTime; // loopStartSamples;
            }
        }

        public void PlaySfx(AudioContainer sfx, bool loop = false)
        {
            if (!loop)
            {
                _SfxAudioSource.PlayOneShot(clip: sfx.Clip);
            }
        }
    }
}

