using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;
using WASD.Runtime.Managers;
using static WASD.Runtime.Gameplay.PlayerCollisionDetector;

namespace WASD.Runtime.Gameplay
{
    public class CharacterControl : MonoBehaviour
    {
        #region Properties
        private float SwitchPositionsRotateToZeroDelay => _SwitchPositionsTime - (_SwitchPositionsTime / 3f);

        #endregion

        #region Fields
        [Header("References")]
        [SerializeField] private Vector3 _CharacterCentraltPoint;
        [SerializeField] private float _CharacterRespawnHeight;
        [SerializeField] private float _CharacterRespawnTime;
        [SerializeField] private float _CharacterPositionDifference;
        [SerializeField] private GameObject _CharacterBlue;
        [SerializeField] private Animator _CharacterBlueAnimator;
        [SerializeField] private Rigidbody _CharacterBlueRigidBody;
        [SerializeField] private GameObject _CharacterRed;
        [SerializeField] private Rigidbody _CharacterRedRigidBody;
        [SerializeField] private Animator _CharacterRedAnimator;
        [SerializeField] private string _RunAnimId = "Run";
        [SerializeField] private string _JumpTriggerId = "Jump";
        [SerializeField] private string _SlideTriggerId = "Slide";
        [SerializeField] private string _DeadTriggerId = "Dead";
        [SerializeField] private string _RespawnTriggerId = "Respawn";
        
        [Header("Stats")]
        [SerializeField] private float _JumpSpeed = 3f;
        [SerializeField] private float _SwitchPositionsTime;
        [SerializeField] private float _SwitchPositionsRotation;
        [SerializeField] private float _SwipeMinimumDistance = .2f;
        [SerializeField] private float _SwipeMaximumTime = 1f;
        [SerializeField] private float _InvincibilityDuration;
        [SerializeField] private float _RespawnInvincibilityDuration;
        [SerializeField, Range(min: 0f, max: 1f)] private float _DirectionThreshold = .9f;

        private Vector2 _SwipeStartPosition;
        private float _SwipeStartTime;
        private Vector2 _SwipeEndPosition;
        private float _SwipeEndTime;

        [SerializeField]private bool _PositionsAreInverted;
        [SerializeField]private bool _BlueGrounded;
        [SerializeField]private bool _RedGrounded;
        [SerializeField]private bool _IsInvincible;
        private bool _IsPaused;
        private bool _IsDead;
        private Vector3 _BlueStoredVelocityOnPause;
        private Vector3 _RedStoredVelocityOnPause;

        private Sequence _BlueSwitchSideSequence;
        private Sequence _RedSwitchSideSequence;

        private CancellationTokenSource _InvincibilityCancelToken;
        private CancellationTokenSource _CancelToken;
        
        #endregion

        #region Events
        [Space(10)]
        [SerializeField] private UnityEvent _OnKill;
        [SerializeField] private UnityEvent _OnWin;
        [SerializeField] private UnityEvent _OnRespawnTimeFinish;
        [SerializeField] private UnityEvent _OnRespawnInvincibilityFinish;
        //[SerializeField] UnityEvent<string> _OnRedSetAnimTrigger;
        //[SerializeField] UnityEvent<string> _OnBlueSetAnimTrigger;
        #endregion

        #region MonoBehaviour
        private void OnEnable()
        {
            InputManager.OnStartTouch += SwipeStart;
            InputManager.OnEndTouch += SwipeEnd;

            PlayerCollisionDetector.OnTriggerEnterEvent += OnTriggerEnterEvent;
            PlayerCollisionDetector.OnCollisionStayEvent += OnCollisionStayEvent;
            PlayerCollisionDetector.OnCollisionExitEvent += OnCollisionExitEvent;
            _CancelToken = new CancellationTokenSource();
        }

        private void OnDisable()
        {
            InputManager.OnStartTouch -= SwipeStart;
            InputManager.OnEndTouch -= SwipeEnd;

            PlayerCollisionDetector.OnTriggerEnterEvent -= OnTriggerEnterEvent;
            PlayerCollisionDetector.OnCollisionStayEvent -= OnCollisionStayEvent;
            PlayerCollisionDetector.OnCollisionExitEvent -= OnCollisionExitEvent;
            Utils.CancelTokenSourceRequestCancelAndDispose(ref _CancelToken);
        }
        #endregion

        private void OnTriggerEnterEvent(GameObject collidedObject, PlayerCollisionDetector collisionDetector)
        {
            if (collisionDetector.Concept == CollisionConcept.Invincibility)
            {
                StartInvincibility();
            }
            
            if(collisionDetector.Concept is CollisionConcept.KillPlayer)
            {
                Kill(collisionDetector);
            }

            if (collisionDetector.Concept is CollisionConcept.Win)
            {
                Win();
            }
        }

        private void OnCollisionStayEvent(GameObject collidedObject, PlayerCollisionDetector collisionDetector)
        {
            if (collisionDetector.Concept is CollisionConcept.KillPlayer ||
                (collidedObject == _CharacterBlue && collisionDetector.Concept is CollisionConcept.RedPlatform) ||
                (collidedObject == _CharacterRed && collisionDetector.Concept is CollisionConcept.BluePlatform))
            {
                if (Kill(collisionDetector))
                {
                    return;  
                }
            }

            if (collisionDetector.Concept is CollisionConcept.BluePlatform or CollisionConcept.RedPlatform)
            {
                if (collidedObject == _CharacterBlue) _BlueGrounded = true;
                if (collidedObject == _CharacterRed) _RedGrounded = true;
            }
        }

        private void OnCollisionExitEvent(GameObject collidedObject, PlayerCollisionDetector collisionDetector)
        {
            if (collisionDetector.Concept is CollisionConcept.BluePlatform or CollisionConcept.RedPlatform)
            {
                if (collidedObject == _CharacterBlue) _BlueGrounded = false;
                if (collidedObject == _CharacterRed) _RedGrounded = false;
            }
        }

        //private void OnCollisionExitEvent(GameObject obj, CollisionConcept concept)
        //{

        //}

        private void SwipeStart(Vector2 position, float time)
        {
            _SwipeStartPosition = position;
            _SwipeStartTime = time;
        }

        private void SwipeEnd(Vector2 position, float time)
        {
            _SwipeEndPosition = position;
            _SwipeEndTime = time;
            DetectSwipe();
        }

        private void DetectSwipe()
        {
            if (_IsPaused) return;

            if(Vector3.Distance(a: _SwipeStartPosition, b: _SwipeEndPosition) >= _SwipeMinimumDistance &&
               (_SwipeEndTime - _SwipeStartTime) <= _SwipeMaximumTime)
            {
                Vector3 dir = _SwipeEndPosition - _SwipeStartPosition;
                Vector2 dir2D = new Vector2(dir.x, dir.y).normalized;
                SwipeDirection(direction: dir2D);
            }
        }

        private void SwipeDirection(Vector2 direction)
        {
            float midPosition = Screen.width / 2f;

            if(Vector2.Dot(lhs: Vector2.up, rhs: direction) > _DirectionThreshold)
            {
                CharacterJump(fromLeft: _SwipeStartPosition.x <= midPosition);
            }
            else if (Vector2.Dot(lhs: Vector2.down, rhs: direction) > _DirectionThreshold)
            {
                CharacterSlide(fromLeft: _SwipeStartPosition.x <= midPosition);
            }
            else if (Vector2.Dot(lhs: Vector2.left, rhs: direction) > _DirectionThreshold)
            {
                SwitchCharacterPositions();
            }
            else if (Vector2.Dot(lhs: Vector2.right, rhs: direction) > _DirectionThreshold)
            {
                SwitchCharacterPositions();
            }
        }

        private bool RunAnimIsPlayingAndGetPlayerComponents(bool fromLeft, out Animator anim, out Rigidbody rb, out bool isGrounded)
        {
            float getFrom = _CharacterCentraltPoint.x;
            getFrom += _CharacterPositionDifference * (fromLeft ? -1f : 1f);

            bool isRed = _CharacterRed.transform.position.x == getFrom;
            anim = isRed ? _CharacterRedAnimator : _CharacterBlueAnimator;
            rb = isRed ? _CharacterRedRigidBody : _CharacterBlueRigidBody;
            isGrounded = isRed ? _RedGrounded : _BlueGrounded; 
            return anim.GetCurrentAnimatorStateInfo(layerIndex: 0).IsName(name: _RunAnimId);
        }

        private void CharacterJump(bool fromLeft)
        {
            if(!RunAnimIsPlayingAndGetPlayerComponents(fromLeft: fromLeft, out Animator anim, out Rigidbody rb, out bool isGrounded) || !isGrounded)
            {
                return;
            }
            anim.SetTrigger(name: _JumpTriggerId);
            rb.AddForce(force: new Vector3(x: 0, y: _JumpSpeed, z: 0), mode: ForceMode.Impulse);
        }

        private void CharacterSlide(bool fromLeft)
        {
            if (!RunAnimIsPlayingAndGetPlayerComponents(fromLeft: fromLeft, out Animator anim, out Rigidbody _, out bool isGrounded) || !isGrounded)
            {
                return;
            }
            anim.SetTrigger(name: _SlideTriggerId);
        }

        private void SwitchCharacterPositions()
        {
            if( !RunAnimIsPlayingAndGetPlayerComponents(false, out _, out _, out _) || 
                !RunAnimIsPlayingAndGetPlayerComponents(true, out _, out _, out _) ||
                !_BlueGrounded || !_RedGrounded)
            {
                return;
            }

            float blueMultiplySign = _CharacterBlue.transform.position.x < _CharacterRed.transform.position.x ? 1 : -1;
            float redMultiplySign = blueMultiplySign * -1f;

            _BlueSwitchSideSequence = CreateSwitchSidesSequence(_CharacterBlue.transform, blueMultiplySign);
            _RedSwitchSideSequence = CreateSwitchSidesSequence(_CharacterRed.transform, redMultiplySign);
            
            CharacterJump(fromLeft: false);
            CharacterJump(fromLeft: true);

            _PositionsAreInverted = !_PositionsAreInverted;
        }

        private Sequence CreateSwitchSidesSequence(Transform target, float multiplySign)
        {
            Vector3 initialRotation = _CharacterBlue.transform.eulerAngles;
            Vector3 finalRotation = initialRotation;
            finalRotation.y += _SwitchPositionsRotation * multiplySign;
            
            var sequence = DOTween.Sequence();
            sequence.Append(target.DOMoveX(
                _CharacterCentraltPoint.x + (_CharacterPositionDifference * multiplySign), _SwitchPositionsTime));
            sequence.Join(target.DORotate(finalRotation, _SwitchPositionsTime / 4f));
            sequence.Join(target.DORotate(initialRotation, _SwitchPositionsTime / 3f).SetDelay(SwitchPositionsRotateToZeroDelay));
            return sequence;
        }
        
        public bool Kill(PlayerCollisionDetector collisionDetector)
        {
            if (_IsInvincible)
            {
                if (collisionDetector.IgnoresInvincibility)
                {
                    _IsDead = true;
                    _OnKill.Invoke();
                    _CharacterRedAnimator.SetTrigger(_DeadTriggerId);
                    _CharacterBlueAnimator.SetTrigger(_DeadTriggerId);
                }

                return false;
            }

            Utils.CancelTokenSourceRequestCancelAndDispose(ref _InvincibilityCancelToken);
            _IsInvincible = false;
            _IsDead = true;
            _OnKill.Invoke();
            _CharacterRedAnimator.SetTrigger(_DeadTriggerId);
            _CharacterBlueAnimator.SetTrigger(_DeadTriggerId);
            return true;
        }

        public void Win()
        {
            _OnWin.Invoke();
        }

        public void SetPauseValue(bool value)
        {
            _IsPaused = value;

            if (!_IsDead)
            {
                _CharacterBlueAnimator.speed = value ? 0 : 1;
                _CharacterRedAnimator.speed = value ? 0 : 1;
            }
            
            _BlueSwitchSideSequence?.TogglePause();
            _RedSwitchSideSequence?.TogglePause();
            
            if (value)
            {
                _BlueStoredVelocityOnPause = _CharacterBlueRigidBody.velocity;
                _CharacterBlueRigidBody.isKinematic = true;

                _RedStoredVelocityOnPause = _CharacterRedRigidBody.velocity;
                _CharacterRedRigidBody.isKinematic = true;
            }
            else
            {
                _CharacterBlueRigidBody.isKinematic = false;
                if (!_IsDead) _CharacterBlueRigidBody.velocity = _BlueStoredVelocityOnPause;

                _CharacterRedRigidBody.isKinematic = false;
                if (!_IsDead) _CharacterRedRigidBody.velocity = _RedStoredVelocityOnPause;
            }
        }

        private void StartInvincibility()
        {
            Utils.CancelTokenSourceRequestCancelAndDispose(ref _InvincibilityCancelToken);
            _InvincibilityCancelToken = CancellationTokenSource.CreateLinkedTokenSource(_CancelToken.Token);
            InvincibilitySequence(_InvincibilityCancelToken.Token).Forget();
        }

        private async UniTaskVoid InvincibilitySequence(CancellationToken cancelToken)
        {
            float timeAdvice = _InvincibilityDuration * 0.25f;
            
            _IsInvincible = true;
            GameManager.Audio.FadeBgmPitch(2, 1f);
            await UniTask.Delay((int)((_InvincibilityDuration - timeAdvice) * 1000), cancellationToken: cancelToken)
                .SuppressCancellationThrow();
            
            GameManager.Audio.FadeBgmPitch(1.5f, 1f);
            await UniTask.Delay((int)(timeAdvice * 1000), cancellationToken: cancelToken)
                .SuppressCancellationThrow();
            
            GameManager.Audio.FadeBgmPitch(1, 1f);
            _IsInvincible = false;
            
            Utils.CancelTokenSourceRequestCancelAndDispose(ref _InvincibilityCancelToken);
        }

        public void Respawn()
        {
            //Ads
            RespawnSequence(_CancelToken.Token).Forget();
        }

        private async UniTaskVoid RespawnSequence(CancellationToken cancelToken)
        {
            _BlueSwitchSideSequence?.Kill(true);
            _RedSwitchSideSequence?.Kill(true);
            
            Vector3 newPos = _CharacterCentraltPoint;
            newPos.x += _CharacterPositionDifference;
            newPos.y += _CharacterRespawnHeight;

            if (_PositionsAreInverted)
            {
                _CharacterRedRigidBody.position = newPos;
                newPos.x *= -1;
                _CharacterBlueRigidBody.position = newPos;
            }
            else
            {
                _CharacterBlueRigidBody.position = newPos;
                newPos.x *= -1;
                _CharacterRedRigidBody.position = newPos;
            }
            
            _CharacterRedAnimator.SetTrigger(_RespawnTriggerId);
            _CharacterBlueAnimator.SetTrigger(_RespawnTriggerId);
            
            _CharacterRedAnimator.ResetTrigger(_DeadTriggerId);
            _CharacterBlueAnimator.ResetTrigger(_DeadTriggerId);
            
            _IsInvincible = true;
            
            await UniTask.Delay((int)(_CharacterRespawnTime * 1000), cancellationToken: cancelToken)
                .SuppressCancellationThrow();
            _OnRespawnTimeFinish?.Invoke();
            _IsDead = false;

            await UniTask.Delay((int)(_RespawnInvincibilityDuration * 1000), cancellationToken: cancelToken)
                .SuppressCancellationThrow();
            _OnRespawnInvincibilityFinish?.Invoke();
            _IsInvincible = _InvincibilityCancelToken != null;
        }
    }
}
