using System.Collections;
using System.Collections.Generic;
using SLS.EditorUtilities.ComponentHeaders;
using UnityEditor;
using UnityEngine;

namespace SLS.StateMachineH 
{

    /// <summary>  
    /// Behavior Scripts attached to a <see cref="StateMachineH.State"/>. Inherit from this to create functionality.  
    /// </summary>  
    [RequireComponent(typeof(State))]
    public abstract class StateBehavior : MonoBehaviour
    {
        /// <summary>  
        /// The <see cref="StateMachine"/> owning this behavior. Likely the most important field you'll be referencing a lot.  
        /// Override with the "new" keyword with an expression like "=> M as MyStateMachine" to get a custom <see cref="StateMachine"/>.  
        /// </summary>  
        public StateMachine Machine => State.Machine;

        /// <summary>  
        /// The current <see cref="StateMachineH.State"/>. Useful for referencing this SubObject.  
        /// </summary>  
        [field: SerializeField, HeaderItem(true)] public State State { get; internal set; }

        /// <summary>  
        /// An indirection to access the <see cref="StateMachine"/>'s <see cref="GameObject"/> property.  
        /// </summary>  
        public new GameObject gameObject => Machine.gameObject;

        /// <summary>  
        /// An indirection to access the <see cref="StateMachine"/>'s <see cref="Transform"/> property.  
        /// </summary>  
        public new Transform transform => Machine.transform;

        /// <summary>  
        /// Sets up the <see cref="StateBehavior"/> and its serialized references with the specified <see cref="State"/> and marks it dirty if required.  
        /// </summary>  
        /// <param name="state">The <see cref="State"/> to associate with this behavior.</param>  
        /// <param name="makeDirty">Whether to mark the behavior as dirty in the editor.</param>  
        public void Setup(State @state, bool makeDirty = false)
        {
            this.State = @state;

            this.OnSetup();

#if UNITY_EDITOR
            if (makeDirty) EditorUtility.SetDirty(this);
#endif
        }

        /// <summary>  
        /// Called during the <see cref="StateMachine"/>'s setup process. Override to add custom setup logic. 
        /// </summary>  
        protected virtual void OnSetup() { }

        /// <summary>  
        /// Happens when the <see cref="StateBehavior"/> is created/reset in the editor. Override to add custom reset logic. (Remember to call base.Reset())
        /// <br /> Resets the <see cref="StateBehavior"/> to its default state.  
        /// <br /> Ensures the <see cref="State"/> and <see cref="StateMachine"/> references are properly initialized.  
        /// </summary>  
        protected virtual void Reset()
        {
            if (State == null) State = GetComponent<State>();
        }

        /// <summary>  
        /// Called during the Awake phase of the <see cref="StateMachine"/>. Override to add custom logic.
        /// </summary>  
        protected virtual void OnAwake() { }
        internal void DoAwake() => OnAwake();

        /// <summary>  
        /// Called when this <see cref="StateBehavior"/>'s <see cref="State"/> is active during the <see cref="StateMachine"/>'s Update phase. Override to add custom logic. 
        /// </summary>  
        protected virtual void OnUpdate() { }
        internal void DoUpdate() => OnUpdate();

        /// <summary>  
        /// Called when this <see cref="StateBehavior"/>'s <see cref="State"/> is active during the <see cref="StateMachine"/>'s FixedUpdate. Override to add custom logic.  
        /// </summary>  
        protected virtual void OnFixedUpdate() { }
        internal void DoFixedUpdate() => OnFixedUpdate();

        /// <summary>  
        /// Called when entering this <see cref="StateBehavior"/>'s <see cref="State"/>. Override to add custom logic. 
        /// </summary>  
        /// <param name="prev">The previous <see cref="State"/> being left in this transition process.</param>  
        /// <param name="isFinal">Whether this is the end <see cref="State"/> being entered or if it has children.</param>
        protected virtual void OnEnter(State prev, bool isFinal) { }
        internal void DoEnter(State prev, bool isFinal)=> OnEnter(prev, isFinal);

        /// <summary>  
        /// Called when exiting this <see cref="StateBehavior"/>'s <see cref="State"/>. Override to add custom logic. 
        /// </summary>  
        /// <param name="next">The new <see cref="State"/> being entered in this transition process.</param>  
        protected virtual void OnExit(State next) { }
        internal void DoExit(State next) => OnExit(next);

        /// <summary>  
        /// Retrieves a <see cref="Component"/> from the associated <see cref="StateMachine"/>.  
        /// </summary>  
        /// <typeparam name="C">The type of <see cref="Component"/> to retrieve.</typeparam>  
        /// <returns>The <see cref="Component"/> of type <typeparamref name="C"/>.</returns>  
        public C GetComponentFromMachine<C>() where C : Component => Machine.GetComponent<C>();

        /// <summary>  
        /// Attempts to retrieve a <see cref="Component"/> from the associated <see cref="StateMachine"/>.  
        /// </summary>  
        /// <typeparam name="C">The type of <see cref="Component"/> to retrieve.</typeparam>  
        /// <param name="result">The retrieved <see cref="Component"/>, if found.</param>  
        /// <returns>True if the <see cref="Component"/> was found; otherwise, false.</returns>  
        public bool TryGetComponentFromMachine<C>(out C result) where C : Component => Machine.TryGetComponent(out result);

        /// <summary>  
        /// Gets whether the <see cref="StateMachineH.State"> is currently active. 
        /// </summary>  
        public static implicit operator bool(StateBehavior B) => B != null && B.State.Active;
    }
}