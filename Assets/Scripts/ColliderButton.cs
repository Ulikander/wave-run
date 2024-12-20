using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using WASD.Runtime.Audio;
using WASD.Runtime.Managers;


namespace WASD.Runtime
{
    public class ColliderButton : MonoBehaviour
    {
        #region Properties
        public UnityEvent OnTapEvent { get => _OnTap; }
        public bool Interactable { get => _Interactable; set => _Interactable = value; }
        #endregion

        #region Fields
        [Header("Properties")]
        [SerializeField] private bool _Interactable;
        [SerializeField] private AudioContainer _TapSound;
        [SerializeField] private Collider _Collider;
        [SerializeField] private UnityEvent _OnTap;
        #endregion

        #region MonoBehaviour
        private void OnEnable()
        {
            InputManager.OnTap += Tap;
        }

        private void OnDisable()
        {
            InputManager.OnTap -= Tap;
        }

        private void Tap(Vector2 position)
        {
            if(_Collider == null)
            {
                Debug.LogWarning("Collider button has a Null 'Collider'");
            }

            if (Interactable &&
                Utils.IsTouchPositionHittingCollider(
                    camera: GameManager.MainCamera,
                    position: position,
                    collider: _Collider))
            {
                _OnTap.Invoke();
                if(_TapSound != null)
                {
                    GameManager.Audio.PlaySfx(sfx: _TapSound);
                }
            }
        }
        #endregion
    }
}

