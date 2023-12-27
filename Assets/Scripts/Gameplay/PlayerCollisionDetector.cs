using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem.LowLevel;

namespace WASD.Runtime.Gameplay
{
    public class PlayerCollisionDetector : MonoBehaviour
    {
        #region Enums
        public enum CollisionConcept
        {
            KillPlayer,
            Win,
            BluePlatform,
            RedPlatform,
            Invincibility,
        }
        #endregion

        #region Properties
        public Collider Collider { get => _Collider; }
        public CollisionConcept Concept { get => _CollisionConcept; set => _CollisionConcept = value; }
        public bool IgnoresInvincibility => _IgnoreInvincibility;
        #endregion

        #region Fields
        [SerializeField] private Collider _Collider;
        [SerializeField] private string _PlayerTag;
        [SerializeField] private CollisionConcept _CollisionConcept;
        [SerializeField] private bool _IgnoreInvincibility;
        #endregion

        #region Events
        public delegate void CollisionDelegate(GameObject collidedObject, PlayerCollisionDetector collisionDetector);
        public static event CollisionDelegate OnCollisionEnterEvent;
        public static event CollisionDelegate OnCollisionStayEvent;
        public static event CollisionDelegate OnCollisionExitEvent;

        public static event CollisionDelegate OnTriggerEnterEvent;
        public static event CollisionDelegate OnTriggerStayEvent;
        public static event CollisionDelegate OnTriggerExitEvent;
        #endregion

        #region MonoBehaviour
        private void OnCollisionEnter(Collision collision) => CheckColliderEvent(
            eventReference: ref OnCollisionEnterEvent,
            collision: collision);

        private void OnCollisionStay(Collision collision) => CheckColliderEvent(
            eventReference: ref OnCollisionStayEvent,
            collision: collision);

        private void OnCollisionExit(Collision collision) => CheckColliderEvent(
            eventReference: ref OnCollisionExitEvent,
            collision: collision);

        private void OnTriggerEnter(Collider other) => CheckCollisionEvent(
            eventReference: ref OnTriggerEnterEvent,
            other: other);
        private void OnTriggerStay(Collider other) => CheckCollisionEvent(
           eventReference: ref OnTriggerStayEvent,
           other: other);
        private void OnTriggerExit(Collider other) => CheckCollisionEvent(
           eventReference: ref OnTriggerExitEvent,
           other: other);
        #endregion


        private void CheckColliderEvent(ref CollisionDelegate eventReference, Collision collision)
        {
            if (eventReference != null && collision.gameObject.CompareTag(_PlayerTag))
            {
                eventReference(collision.gameObject,this);
            }
        }

        private void CheckCollisionEvent(ref CollisionDelegate eventReference, Collider other)
        {
            if (eventReference != null && other.gameObject.CompareTag(_PlayerTag))
            {
                eventReference(other.gameObject, this);
            }
        }
    }

}
