using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SLS.StateMachineH
{
    /// <summary>  
    /// Interface for state behaviors that interact with physics.  
    /// </summary>  
    public interface IStateBehaviorPhysics
    {
        /// <summary>  
        /// Indicates whether the state is active.  
        /// </summary>  
        public sealed bool isActive => (this as StateBehavior).State.Active;
    }
    /// <summary>  
    /// Interface for state behaviors handling 3D collision events.  
    /// </summary>  
    public interface IStateBehaviorPhysicsCollision : IStateBehaviorPhysics
    {
        /// <summary>  
        /// Called when a collision starts with a collider attached to the StateMachine.  
        /// </summary>  
        /// <param name="collision">The collision data.</param>  
        void OnCollisionEnter(Collision collision);

        /// <summary>  
        /// Called when a collision ends with a collider attached to the StateMachine.  
        /// </summary>  
        /// <param name="collision">The collision data.</param>  
        void OnCollisionExit(Collision collision);
    }
    /// <summary>  
    /// Interface for state behaviors handling 3D trigger events.  
    /// </summary>  
    public interface IStateBehaviorPhysicsTrigger : IStateBehaviorPhysics
    {
        /// <summary>  
        /// Called when a trigger attached to the StateMachine is entered.   
        /// </summary>  
        /// <param name="other">The collider involved in the trigger.</param>  
        void OnTriggerEnter(Collider other);

        /// <summary>  
        /// Called when a trigger attached to the StateMachine is exited.   
        /// </summary>  
        /// <param name="other">The collider involved in the trigger.</param>  
        void OnTriggerExit(Collider other);
    }
    /// <summary>  
    /// Interface for state behaviors handling 2D collision events.  
    /// </summary>  
    public interface IStateBehaviorPhysicsCollision2D : IStateBehaviorPhysics
    {
        /// <summary>  
        /// Called when a 2D collision starts.  
        /// </summary>  
        /// <param name="collision">The collision data.</param>  
        void OnCollisionEnter2D(Collision2D collision);

        /// <summary>  
        /// Called when a 2D collision ends.  
        /// </summary>  
        /// <param name="collision">The collision data.</param>  
        void OnCollisionExit2D(Collision2D collision);
    }
    /// <summary>  
    /// Interface for state behaviors handling 2D trigger events.  
    /// </summary>  
    public interface IStateBehaviorPhysicsTrigger2D : IStateBehaviorPhysics
    {
        /// <summary>  
        /// Called when a 2D trigger is entered.  
        /// </summary>  
        /// <param name="collision">The collider involved in the trigger.</param>  
        void OnTriggerEnter2D(Collider2D collision);

        /// <summary>  
        /// Called when a 2D trigger is exited.  
        /// </summary>  
        /// <param name="collision">The collider involved in the trigger.</param>  
        void OnTriggerExit2D(Collider2D collision);
    }
}