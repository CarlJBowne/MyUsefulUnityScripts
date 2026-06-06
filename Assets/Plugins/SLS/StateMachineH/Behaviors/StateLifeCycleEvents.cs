using SLS.StateMachineH;
using UnityEngine;


#if ULT_EVENTS
using EVENT = UltEvents.UltEvent;
#else
using EVENT = UnityEngine.Events.UnityEvent;
#endif

namespace SLS.StateMachineH
{
    /// <summary>  
    /// A behavior that invokes inspector-usable events for the lifecycle phases of a state in the hierarchical <see cref="StateMachine"/>.
    /// </summary>  
    public class StateLifeCycleEvents : StateBehavior
    {
        /// <summary>  
        /// Event invoked during the setup phase of the state.  
        /// </summary>  
        public EVENT onSetup = new();

        /// <summary>  
        /// Event invoked during the awake phase of the state.  
        /// </summary>  
        public EVENT onAwake = new();

        /// <summary>  
        /// Event invoked when entering the state.  
        /// </summary>  
        public EVENT onEnter = new();

        /// <summary>  
        /// Event invoked when exiting the state.  
        /// </summary>  
        public EVENT onExit = new();

        /// <summary>  
        /// Event invoked when entering or exiting the state.  
        /// </summary>  


#if ULT_EVENTS
        public UltEvents.UltEvent<bool> onStateChange = new();
#else
        public UnityEngine.Events.UnityEvent<bool> onStateChange = new();
#endif






        /// <summary>  
        /// Array of GameObjects to activate when entering the state and deactivate when exiting the state.  
        /// </summary>  
        public GameObject[] activateObjects;

        /// <summary>  
        /// Invoked during the setup phase of the state.  
        /// </summary>  
        protected override void OnSetup() => onSetup?.Invoke();

        /// <summary>  
        /// Invoked during the awake phase of the state.  
        /// </summary>  
        protected override void OnAwake() => onAwake?.Invoke();

        /// <summary>  
        /// Invoked when entering the state. Activates associated GameObjects.  
        /// </summary>  
        /// <param name="prev">The previous state.</param>  
        /// <param name="isFinal">Indicates whether this is the final state.</param>  
        protected override void OnEnter(State prev, bool isFinal)
        {
            onEnter?.Invoke();
            onStateChange?.Invoke(true);
            if (activateObjects != null)
                for (int i = 0; i < activateObjects.Length; i++)
                    activateObjects[i].SetActive(true);
        }

        /// <summary>  
        /// Invoked when exiting the state. Deactivates associated GameObjects.  
        /// </summary>  
        /// <param name="next">The next state.</param>  
        protected override void OnExit(State next)
        {
            onExit?.Invoke();
            onStateChange?.Invoke(false);
            if (activateObjects != null)
                for (int i = 0; i < activateObjects.Length; i++)
                    activateObjects[i].SetActive(false);
        }
    }
}