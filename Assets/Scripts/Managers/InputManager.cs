using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Interactions;

using WASD.Input;

namespace WASD.Runtime.Managers
{
    public class InputManager : MonoBehaviour
    {
        #region Events
        public delegate void StartTouchEvent(Vector2 position, float time);
        public static event StartTouchEvent OnStartTouch;
        public delegate void EndTouchEvent(Vector2 position, float time);
        public static event EndTouchEvent OnEndTouch;
        public delegate void TapEvent(Vector2 position);
        public static event TapEvent OnTap;
        #endregion

        #region Fields
        private PlayerControls _PlayerControls;
        #endregion

        #region Monobehaviour
        private void Awake()
        {
            _PlayerControls = new PlayerControls();
        }

        private void OnEnable()
        {
            _PlayerControls.Enable();
        }

        private void OnDisable()
        {
            _PlayerControls.Disable();
        }

        private void Start()
        {
            _PlayerControls.Touch.TouchPress.started += ctx => StartTouch(context: ctx);
            _PlayerControls.Touch.TouchPress.performed += ctx => Tap(context: ctx);
            _PlayerControls.Touch.TouchPress.canceled += ctx => EndTouch(context: ctx);
        }
        #endregion

        private void StartTouch(InputAction.CallbackContext context)
        {
            OnStartTouch?.Invoke(position: _PlayerControls.Touch.TouchPosition.ReadValue<Vector2>(), time: (float)context.startTime);
        }

        private void Tap(InputAction.CallbackContext context)
        {
            if (OnTap != null && context.interaction is TapInteraction)
            {
                OnTap(position: _PlayerControls.Touch.TouchPosition.ReadValue<Vector2>());
            }
        }

        private void EndTouch(InputAction.CallbackContext context)
        {
            OnEndTouch?.Invoke(position: _PlayerControls.Touch.TouchPosition.ReadValue<Vector2>(), time: (float)context.time);
        }
    }
}

