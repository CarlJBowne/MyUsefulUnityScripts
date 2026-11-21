using System;
using UnityEngine;

namespace SLS.StateMachineUltimate.Physics
{
    public sealed class StateMachinePhysicsCaller : MonoBehaviour
    {
        public bool DoCollisionEnter;
        public bool DoCollisionExit;
        public bool DoTriggerEnter;
        public bool DoTriggerExit;

        internal event Action<Collision> CollisionEnter;
        internal event Action<Collision> CollisionExit;
        internal event Action<Collider> TriggerEnter;
        internal event Action<Collider> TriggerExit;


        private void OnCollisionEnter(Collision collision)
        {
            if (!DoCollisionEnter) return;
            CollisionEnter?.Invoke(collision);
        }
        private void OnCollisionExit(Collision collision)
        {
            if (!DoCollisionExit) return;
            CollisionExit?.Invoke(collision);
        }
        private void OnTriggerEnter(Collider other)
        {
            if (!DoTriggerEnter) return;
            TriggerEnter(other);
        }
        private void OnTriggerExit(Collider other)
        {
            if (!DoTriggerExit) return;
            TriggerExit(other);
        }
    }
}
