using UnityEngine;

namespace SLS.StateMachineH
{
    /// <summary>
    /// A behavior that handles physics-related events for states within a <see cref="StateMachine"/>.
    /// </summary>
    [RequireComponent(typeof(Rigidbody), typeof(StateMachine))]
    public class StatePhysicsCaller : StateBehavior
    {
        /// <summary>
        /// Array of components implementing <see cref="IStateBehaviorPhysicsCollision"/> for handling collision events.
        /// </summary>
        private IStateBehaviorPhysicsCollision[] collisions;

        /// <summary>
        /// Array of components implementing <see cref="IStateBehaviorPhysicsTrigger"/> for handling trigger events.
        /// </summary>
        private IStateBehaviorPhysicsTrigger[] triggers;

        /// <summary>
        /// Called during the Awake phase to initialize collision and trigger handlers.
        /// </summary>
        protected override void OnAwake()
        {
            collisions = Machine.StateHolder.GetComponentsInChildren<IStateBehaviorPhysicsCollision>();
            triggers = Machine.StateHolder.GetComponentsInChildren<IStateBehaviorPhysicsTrigger>();
        }

        /// <summary>
        /// Invoked when a collision starts. Delegates the event to active collision handlers.
        /// </summary>
        /// <param name="collision">The collision data.</param>
        private void OnCollisionEnter(Collision collision)
        {
            for (int i = 0; i < collisions.Length; i++)
                if (collisions[i].isActive)
                    collisions[i].OnCollisionEnter(collision);
        }

        /// <summary>
        /// Invoked when a collision ends. Delegates the event to active collision handlers.
        /// </summary>
        /// <param name="collision">The collision data.</param>
        private void OnCollisionExit(Collision collision)
        {
            for (int i = 0; i < collisions.Length; i++)
                if (collisions[i].isActive)
                    collisions[i].OnCollisionExit(collision);
        }

        /// <summary>
        /// Invoked when a trigger is entered. Delegates the event to active trigger handlers.
        /// </summary>
        /// <param name="other">The collider that entered the trigger.</param>
        private void OnTriggerEnter(Collider other)
        {
            for (int i = 0; i < triggers.Length; i++)
                if (triggers[i].isActive)
                    triggers[i].OnTriggerEnter(other);
        }

        /// <summary>
        /// Invoked when a trigger is exited. Delegates the event to active trigger handlers.
        /// </summary>
        /// <param name="other">The collider that exited the trigger.</param>
        private void OnTriggerExit(Collider other)
        {
            for (int i = 0; i < triggers.Length; i++)
                if (triggers[i].isActive)
                    triggers[i].OnTriggerExit(other);
        }
    }

}
