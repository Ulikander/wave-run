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

        public AudioContainer CurrentBgm
        {
            get => _CurrentBgm;
        }

        public float CurrentBgmTime
        {
            get => _CurrentBgm != null ? _BgmAudioSource.time : 0f;
        }
        
        public float DefaultFadeInTime { get => _DefaultFadeInTime; }
        public float DefaultFadeOutTime { get => _DefaultFadeOutTime; }
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

        private CancellationToken localCancelToken;
        private CancellationTokenSource _PitchFadeTokenSource;
        private CancellationTokenSource _BgmFadeTokenSource;
        private CancellationTokenSource _BgmLoopTokenSource;
        
        #endregion

        #region MonoBehaviour
        private void Awake()
        {
            localCancelToken = this.GetCancellationTokenOnDestroy();
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

        public async UniTaskVoid FadeBgmPitch(float target, float fadeSpeed = 0f, bool immediate = false)
        {
            _TargetPitch = target;

            if (immediate)
            {
                Utils.CancelTokenSourceRequestCancelAndDispose(ref _BgmFadeTokenSource);
                _BgmAudioSource.pitch = _TargetPitch;
                await UniTask.NextFrame(localCancelToken);
                return;
            }

            //if (!_BgmAudioSource.isPlaying) return;
            if(_BgmAudioSource.pitch != _TargetPitch && !Utils.IsCancelTokenSourceActive(ref _PitchFadeTokenSource))
            {
                _PitchFadeTokenSource = CancellationTokenSource.CreateLinkedTokenSource(localCancelToken);
                BgmPitchFadeTask(fadeSpeed <= 0 ? _DefaultPitchFadeSpeed : fadeSpeed,
                    cancelToken: _PitchFadeTokenSource.Token);
            }
        }

        private async void BgmPitchFadeTask(float fadeSpeed, CancellationToken cancelToken)
        {
            while (!cancelToken.IsCancellationRequested && _BgmAudioSource.pitch != _TargetPitch)
            {
                var direction = _BgmAudioSource.pitch > _TargetPitch ? -1f : _BgmAudioSource.pitch < _TargetPitch ? 1f : 0;
                _BgmAudioSource.pitch += fadeSpeed * Time.unscaledDeltaTime * direction;
                if ((direction >= 1 && _BgmAudioSource.pitch > _TargetPitch) ||
                    (direction <= -1 && _BgmAudioSource.pitch < _TargetPitch))
                {
                    _BgmAudioSource.pitch = _TargetPitch;
                }

                await UniTask.Yield(cancelToken).SuppressCancellationThrow();
            }
            
            Utils.CancelTokenSourceRequestCancelAndDispose(ref _PitchFadeTokenSource);
        }

        public void StopBgm(bool skipFadeOut = true)
        {
            PlayBgm(bgm: null, skipFadeOut: skipFadeOut);
        }

        public async void PlayBgm(
            AudioContainer bgm,
            bool skipFadeIn = false,
            bool skipFadeOut = false,
            bool randomizeStart = false,
            bool restartIfSame = false,
            float fadeOutTime = 0f,
            float fadeInTime = 0f)
        {
            if (bgm != null && bgm.AudioType != Enums.AudioContainerType.BGM)
            {
                Debug.LogError(message: $"Tried to play SFX as a BGM, that's not correct. Stupeh!");
                return;
            }

            if (bgm != null && _CurrentBgm != null && bgm.Clip == _CurrentBgm.Clip && !restartIfSame)
            {
                Debug.Log(message: $"Tried to play a BGM that is already playing, but Restart If Same is false");
                return;
            }

            
            Utils.CancelTokenSourceRequestCancelAndDispose(ref _BgmFadeTokenSource);

            if (!skipFadeIn || !skipFadeOut)
            {
                _BgmFadeTokenSource = new CancellationTokenSource();
                FadeBgmRoutine(bgm, skipFadeIn, skipFadeOut, randomizeStart, _BgmFadeTokenSource.Token, fadeOutTime,
                    fadeInTime).Forget();
            }
            else
            {
                FadeBgmRoutine(bgm, skipFadeIn, skipFadeOut, randomizeStart, localCancelToken, fadeOutTime, fadeInTime)
                    .Forget();
            }
            
        }

        private async UniTaskVoid FadeBgmRoutine(
            AudioContainer audioContainer,
            bool skipFadeIn,
            bool skipFadeOut,
            bool randomizeStart,
            CancellationToken cancelToken,
            float fadeOutTime = 0f,
            float fadeInTime = 0f)
        {
            float counter = 0;
            
            if (!skipFadeOut && _BgmAudioSource.isPlaying)
            {
                fadeOutTime = fadeOutTime == 0 ? _DefaultFadeOutTime : fadeOutTime;
                
                float currentVolume = _BgmAudioSource.volume;
                while (!cancelToken.IsCancellationRequested && counter < fadeOutTime)
                {
                    counter += Time.deltaTime;
                    _BgmAudioSource.volume = Mathf.Lerp(a: currentVolume, b: 0f, t: counter / fadeOutTime);
                    await UniTask.Yield(cancelToken).SuppressCancellationThrow();
                }
            }

            if (cancelToken.IsCancellationRequested) return;

            _BgmAudioSource.Stop();
            _BgmAudioSource.volume = 0f;
            Utils.CancelTokenSourceRequestCancelAndDispose(ref _BgmLoopTokenSource);
            
            _BgmAudioSource.clip = audioContainer != null ? audioContainer.Clip : null;
            _CurrentBgm = audioContainer;

            if (audioContainer == null)
            {
                Utils.CancelTokenSourceRequestCancelAndDispose(ref _BgmFadeTokenSource);
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
                _BgmLoopTokenSource = CancellationTokenSource.CreateLinkedTokenSource(localCancelToken);
                LoopBgmRoutine(_BgmLoopTokenSource.Token).Forget();
            }

            if (!skipFadeIn)
            {
                fadeInTime = fadeInTime == 0 ? _DefaultFadeInTime : fadeInTime;
                
                counter = 0;
                while (!cancelToken.IsCancellationRequested && counter < fadeInTime)
                {
                    counter += Time.deltaTime;
                    _BgmAudioSource.volume = Mathf.Lerp(a: 0, b: 1f, t: counter / fadeInTime);
                    await UniTask.Yield(cancelToken).SuppressCancellationThrow();
                }
            }

            Utils.CancelTokenSourceRequestCancelAndDispose(ref _BgmFadeTokenSource);
            _BgmAudioSource.volume = 1f;
        }

        private async UniTaskVoid LoopBgmRoutine(CancellationToken cancelToken)
        {
            while (!cancelToken.IsCancellationRequested)
            {
                await UniTask.WaitWhile(() => _BgmAudioSource.time < _CurrentBgm.LoopEndTime,
                    cancellationToken: cancelToken).SuppressCancellationThrow();
                if (cancelToken.IsCancellationRequested) return;
                if (_BgmAudioSource != null) _BgmAudioSource.time = _CurrentBgm.LoopStartTime; // loopStartSamples;
            }
            
            Utils.CancelTokenSourceRequestCancelAndDispose(ref _BgmLoopTokenSource);
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

