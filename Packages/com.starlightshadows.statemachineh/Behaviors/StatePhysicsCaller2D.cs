using UnityEngine;

namespace SLS.StateMachineH
{
    /// <summary>
    /// A behavior that handles 2D physics events for a state machine.
    /// </summary>
    [RequireComponent(typeof(Rigidbody), typeof(StateMachine))]
    public class StatePhysicsCaller2D : StateBehavior
    {
        /// <summary>
        /// Array of components implementing <see cref="IStateBehaviorPhysicsCollision2D"/> for handling collision events.
        /// </summary>
        private IStateBehaviorPhysicsCollision2D[] collisions2D;

        /// <summary>
        /// Array of components implementing <see cref="IStateBehaviorPhysicsTrigger2D"/> for handling trigger events.
        /// </summary>
        private IStateBehaviorPhysicsTrigger2D[] triggers2D;

        /// <summary>
        /// Called during the Awake phase to initialize collision and trigger handlers.
        /// </summary>
        protected override void OnAwake()
        {
            collisions2D = Machine.StateHolder.GetComponentsInChildren<IStateBehaviorPhysicsCollision2D>();
            triggers2D = Machine.StateHolder.GetComponentsInChildren<IStateBehaviorPhysicsTrigger2D>();
        }

        /// <summary>
        /// Called when a collision starts. Invokes <see cref="IStateBehaviorPhysicsCollision2D.OnCollisionEnter2D"/> on active handlers.
        /// </summary>
        /// <param name="collision">The collision data.</param>
        private void OnCollisionEnter2D(Collision2D collision)
        {
            for (int i = 0; i < collisions2D.Length; i++)
                if (collisions2D[i].isActive)
                    collisions2D[i].OnCollisionEnter2D(collision);
        }

        /// <summary>
        /// Called when a collision ends. Invokes <see cref="IStateBehaviorPhysicsCollision2D.OnCollisionExit2D"/> on active handlers.
        /// </summary>
        /// <param name="collision">The collision data.</param>
        private void OnCollisionExit2D(Collision2D collision)
        {
            for (int i = 0; i < collisions2D.Length; i++)
                if (collisions2D[i].isActive)
                    collisions2D[i].OnCollisionExit2D(collision);
        }

        /// <summary>
        /// Called when a trigger starts. Invokes <see cref="IStateBehaviorPhysicsTrigger2D.OnTriggerEnter2D"/> on active handlers.
        /// </summary>
        /// <param name="collision">The collider data.</param>
        private void OnTriggerEnter2D(Collider2D collision)
        {
            for (int i = 0; i < triggers2D.Length; i++)
                if (triggers2D[i].isActive)
                    triggers2D[i].OnTriggerEnter2D(collision);
        }

        /// <summary>
        /// Called when a trigger ends. Invokes <see cref="IStateBehaviorPhysicsTrigger2D.OnTriggerExit2D"/> on active handlers.
        /// </summary>
        /// <param name="collision">The collider data.</param>
        private void OnTriggerExit2D(Collider2D collision)
        {
            for (int i = 0; i < triggers2D.Length; i++)
                if (triggers2D[i].isActive)
                    triggers2D[i].OnTriggerExit2D(collision);
        }
    }

}