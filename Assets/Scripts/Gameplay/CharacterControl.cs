using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Events;
using WASD.Runtime.Managers;
using static WASD.Runtime.Gameplay.PlayerCollisionDetector;

namespace WASD.Runtime.Gameplay
{
    public class CharacterControl : MonoBehaviour
    {
        #region Properties
        private float _SwitchPositionsRotateToZeroDelay => _SwitchPositionsTime - (_SwitchPositionsTime / 3f);

        #endregion

        #region Fields
        [SerializeField] private Vector3 _CharacterCentraltPoint;
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
        [SerializeField] private float _JumpSpeed = 3f;
        [SerializeField] private float _SwitchPositionsTime;
        [SerializeField] private float _SwitchPositionsRotation;
        [SerializeField] private float _SwipeMinimumDistance = .2f;
        [SerializeField] private float _SwipeMaximumTime = 1f;
        [SerializeField, Range(min: 0f, max: 1f)] private float _DirectionThreshold = .9f;

        private Vector2 _SwipeStartPosition;
        private float _SwipeStartTime;
        private Vector2 _SwipeEndPosition;
        private float _SwipeEndTime;

        [SerializeField] private bool _PositionsAreInverted;
        [SerializeField] private bool _BlueGrounded;
        [SerializeField] private bool _RedGrounded;
        private bool _IsPaused;
        private Vector3 _BlueStoredVelocityOnPause;
        private Vector3 _RedStoredVelocityOnPause;

        private Sequence _BlueSwitchSideSequence;
        private Sequence _RedSwitchSideSequence;
        
        #endregion

        #region Events
        [SerializeField] UnityEvent _OnKill;
        [SerializeField] UnityEvent _OnWin;
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
        }

        private void OnDisable()
        {
            InputManager.OnStartTouch -= SwipeStart;
            InputManager.OnEndTouch -= SwipeEnd;

            PlayerCollisionDetector.OnTriggerEnterEvent -= OnTriggerEnterEvent;
            PlayerCollisionDetector.OnCollisionStayEvent -= OnCollisionStayEvent;
            PlayerCollisionDetector.OnCollisionExitEvent -= OnCollisionExitEvent;
        }
        #endregion

        private void OnTriggerEnterEvent(GameObject obj, CollisionConcept concept)
        {
            if(concept is CollisionConcept.KillPlayer)
            {
                Kill();
            }

            if (concept is CollisionConcept.Win)
            {
                Win();
            }
        }

        private void OnCollisionStayEvent(GameObject obj, CollisionConcept concept)
        {
            if (concept is CollisionConcept.KillPlayer ||
                (obj == _CharacterBlue && concept is CollisionConcept.RedPlatform) ||
                (obj == _CharacterRed && concept is CollisionConcept.BluePlatform))
            {
                Kill();
                return;
            }

            if (obj == _CharacterBlue && concept is CollisionConcept.BluePlatform)
            {
                _BlueGrounded = true;
                return;
            }
            else if (obj == _CharacterRed && concept is CollisionConcept.RedPlatform)
            {
                _RedGrounded = true;
                return;
            }
        }

        private void OnCollisionExitEvent(GameObject obj, CollisionConcept concept)
        {
            if(obj == _CharacterBlue && concept is CollisionConcept.BluePlatform)
            {
                _BlueGrounded = false;
                
            }
            else if (obj == _CharacterRed && concept is CollisionConcept.RedPlatform)
            {
                _RedGrounded = false;
                
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

        private bool RunAnimIsRunningAndGetPlayerComponents(bool fromLeft, out Animator anim, out Rigidbody rb, out bool isGrounded)
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
            if(!RunAnimIsRunningAndGetPlayerComponents(fromLeft: fromLeft, out Animator anim, out Rigidbody rb, out bool isGrounded) || !isGrounded)
            {
                return;
            }
            anim.SetTrigger(name: _JumpTriggerId);
            rb.AddForce(force: new Vector3(x: 0, y: _JumpSpeed, z: 0), mode: ForceMode.Impulse);
        }

        private void CharacterSlide(bool fromLeft)
        {
            if (!RunAnimIsRunningAndGetPlayerComponents(fromLeft: fromLeft, out Animator anim, out Rigidbody _, out bool isGrounded) || !isGrounded)
            {
                return;
            }
            anim.SetTrigger(name: _SlideTriggerId);
        }

        private void SwitchCharacterPositions()
        {
            if(!_BlueGrounded || !_RedGrounded)
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
            sequence.Join(target.DORotate(initialRotation, _SwitchPositionsTime / 3f).SetDelay(_SwitchPositionsRotateToZeroDelay));
            return sequence;
        }
        
        public void Kill()
        {
            _OnKill.Invoke();
        }

        public void Win()
        {
            _OnWin.Invoke();
        }

        public void SetPauseValue(bool value)
        {
            _IsPaused = value;
            _CharacterBlueAnimator.speed = value ? 0 : 1;
            _CharacterRedAnimator.speed = value ? 0 : 1;

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
                _CharacterBlueRigidBody.velocity = _BlueStoredVelocityOnPause;

                _CharacterRedRigidBody.isKinematic = false;
                _CharacterRedRigidBody.velocity = _RedStoredVelocityOnPause;
              
            }
        }
    }

}
