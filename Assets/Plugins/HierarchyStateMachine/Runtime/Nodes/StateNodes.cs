using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SLS.StateMachineH.V5
{
    /// <summary>  
    /// The base interface for every node in the hierarchical state machine. 
    /// </summary>  
    public interface IStateNode
    {
        /// <summary>  
        /// Gets a value indicating whether the node is active.  
        /// </summary>  
        public bool Active { get; }

        /// <summary>  
        /// Gets the behaviors associated with this state node.  
        /// </summary>  
        StateBehavior[] Behaviors { get; }

        /// <summary>  
        /// Indexer to retrieve a specific behavior by its type.  
        /// </summary>  
        /// <param name="T">The type of the behavior to retrieve.</param>  
        /// <returns>The behavior of the specified type, or null if not found.</returns>  
        public StateBehavior this[System.Type T]
        {
            get
            {
                for (int i = 0; i < Behaviors.Length; i++)
                    if (Behaviors[i].GetType() == T)
                        return Behaviors[i];
                return null;
            }
        }

        /// <summary>  
        /// Retrieves a specific behavior of the given type.  
        /// </summary>  
        /// <typeparam name="T">The type of the behavior to retrieve.</typeparam>  
        /// <returns>The behavior of the specified type, or null if not found.</returns>  
        public T Behavior<T>() where T : StateBehavior
        {
            for (int i = 0; i < Behaviors.Length; i++)
                if (Behaviors[i] is T)
                    return Behaviors[i] as T;
            return null;
        }

        /// <summary>  
        /// Sets up the state node with the specified parameters.  
        /// </summary>  
        /// <param name="machine">The state machine to associate with this node.</param>  
        /// <param name="parent">The parent state group.</param>  
        /// <param name="layer">The layer of the node.</param>  
        /// <param name="makeDirty">Indicates whether the node should be marked as dirty.</param>  
        void Setup(StateMachine machine, IStateGroup parent, int layer, bool makeDirty = false);

        /// <summary>  
        /// Called during the awake phase of the node.  
        /// </summary>  
        void DoAwake();

        /// <summary>  
        /// Called during the update phase of the node.  
        /// </summary>  
        void DoUpdate();

        /// <summary>  
        /// Called during the fixed update phase of the node.  
        /// </summary>  
        void DoFixedUpdate();
    }

    /// <summary>  
    /// Interface for group nodes, includes StateGroups and StateMachines, but not States.
    /// </summary>  
    public interface IStateGroup : IStateNode
    {
        /// <summary>  
        /// Gets the child nodes of this state group.  
        /// </summary>  
        public IStateChild[] ChildNodes { get; }

        /// <summary>  
        /// Gets the number of child nodes in this state group.  
        /// </summary>  
        public int ChildCount { get; }

        /// <summary>  
        /// Gets the currently active child node.  
        /// </summary>  
        public IStateChild CurrentChild { get; }
    }

    /// <summary>  
    /// Interface for child nodes, includes States and StateGroups, but not StateMachines.
    /// </summary>  
    public interface IStateChild : IStateNode
    {
        /// <summary>  
        /// Gets the state machine associated with this child node.  
        /// </summary>  
        public StateMachine Machine { get; }

        /// <summary>  
        /// Gets the parent state group of this child node.  
        /// </summary>  
        public IStateGroup Parent { get; }

        /// <summary>  
        /// Gets the layer of this child node.  
        /// </summary>  
        public int Layer { get; }
    }
}