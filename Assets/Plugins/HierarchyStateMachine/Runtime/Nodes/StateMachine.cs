using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace SLS.StateMachineH
{

    /// <summary>  
    /// The root of the Hierarchical <see cref="StateMachine"/>.
    /// <br /> This class inherits from <see cref="State"/> to manage <see cref="StateBehavior"/>s and relationships, 
    /// <br /> with additional functionality to manage currently active <see cref="State"/>s and enact active functionality.
    /// <br /> Can be inherited from to cache integral components for large systems.
    /// <br /> Use <see cref="StateBehavior"/> for adding additional functionality.  
    /// </summary>  
    public class StateMachine : State, IPrebuildable
    {
        /// <summary>  
        /// A serialized marker of whether the <see cref="StateMachine"/>'s Tree has been built.
        /// </summary>  
        [field: SerializeField] public bool StatesSetup { get; internal set; }

        /// <summary>  
        /// The empty <see cref="Transform"/> that holds the child <see cref="State"/>s.  
        /// </summary>  
        [field: SerializeField] public Transform StateHolder { get; internal set; }

        /// <summary>  
        /// Indicates whether this <see cref="StateMachine"/> is active. Derived from <see cref="Behaviour.enabled"/>
        /// </summary>  
        public override bool Active => enabled;

        /// <summary>  
        /// The currently active end-<see cref="State"/> within this <see cref="StateMachine"/>.  
        /// </summary>  
        public State CurrentState { get; internal set; }

        /// <summary>  
        /// A subscribable <see cref="System.Action"/> invoked when the <see cref="StateMachine"/>'s initialization is complete.  
        /// </summary>  
        public System.Action waitforMachineInit;

        /// <summary>  
        /// The type of this <see cref="State"/>, which is <see cref="StateType.Machine"/>.  
        /// </summary>  
        public override StateType Type => StateType.Machine;

        /// <summary>
        /// The layer index of this <see cref="StateMachine"/>, which should always be -1.
        /// </summary>
        public override int Layer => -1;

        /// <summary>
        /// The <see cref="StateMachineH.SignalManager"/> associated with this <see cref="StateMachine" (OPTIONAL.)/>.
        /// </summary>
        public Signals.SignalManager SignalManager
        {
            get
            {
                if (_singalManager != null) return _singalManager;
                if (TryGetComponent(out _singalManager)) return _singalManager;
                return null;
            }
            private set => _singalManager = value;
        }
        [SerializeField, HideInInspector] private Signals.SignalManager _singalManager;

        /// <summary>
        /// This <see cref="StateMachine"/>.
        /// </summary>
        public override StateMachine Machine => this;




        /// <summary>  
        /// Updates the state machine. Invokes <see cref="DoUpdate"/>.  
        /// </summary>  
        protected virtual void Update() => DoUpdate();

        /// <summary>  
        /// Fixed update for the state machine. Invokes <see cref="DoFixedUpdate"/>.  
        /// </summary>  
        protected virtual void FixedUpdate() => DoFixedUpdate();

        #region Initialization  

        /// <summary>  
        /// Initializes the state machine during the Awake phase.  
        /// Sets up states if not already done, invokes <see cref="OnAwake"/> and <see cref="DoAwake"/>,  
        /// and transitions to the initial state.  
        /// </summary>  
        private void Awake()
        {
            if (!StatesSetup) Setup(this, this, -1, false);
            OnAwake();
            DoAwake();

            for (int i = 0; i < Behaviors.Length; i++) Behaviors[i].DoEnter(null, false);
            CurrentChild = Children[0];
            TransitionState(Children[0]);

            waitforMachineInit?.Invoke();
        }

        /// <summary>  
        /// Resets the state machine to its basic setup.  
        /// </summary>  
        private void Reset() => SetupBasics();

        /// <summary>  
        /// Sets up the state machine and its child states.  
        /// </summary>  
        /// <param name="machine">The owning <see cref="StateMachine"/>.</param>  
        /// <param name="parent">The parent <see cref="State"/>.</param>  
        /// <param name="layer">The layer index of this <see cref="State"/>.</param>  
        /// <param name="makeDirty">Whether to mark the state machine as dirty in the editor.</param>  
        public override void Setup(StateMachine machine, State parent, int layer, bool makeDirty)
        {
            if (StateHolder == null || Machine == null) SetupBasics();
            if (StateHolder.childCount == 0)
                throw new System.Exception("Stateless State Machines are not supported. If you need to use StateBehaviors on something with only one state, create a dummy state.");

            this.PreSetup();

            {
                ChildCount = StateHolder.childCount;
                Children = new();
                for (int i = 0; i < ChildCount; i++)
                {
                    GameObject childG = StateHolder.GetChild(i).gameObject;
                    if (!childG.TryGetComponent(out State childS)) break;
                    Children.Add(childS);
                    childS.Setup(machine, this, layer + 1, makeDirty);
                }
            }

            Behaviors = GetComponents<StateBehavior>();
            for (int i = 0; i < Behaviors.Length; i++) Behaviors[i].Setup(this);
            SignalManager = GetComponent<Signals.SignalManager>();

            StatesSetup = true;

            if(makeDirty) ApplySetupChanges();
        }

        /// <summary>  
        /// Sets up the basic components of the state machine.  
        /// </summary>  
        private void SetupBasics()
        {
            if (Machine == null) Machine = this;
            Transform tryRoot = transform.Find("States");
            if (tryRoot)
            {
                StateHolder = tryRoot;
#if UNITY_EDITOR
                EditorUtility.SetDirty(this);
#endif
            }
            else
            {
                var newRoot = new GameObject("States");
                newRoot.transform.SetParent(transform, false);
                StateHolder = newRoot.transform;
            }
        }

        /// <summary>  
        /// Pre-setup logic for the state machine. Override to add custom behavior.  
        /// </summary>  
        protected virtual void PreSetup() { }

        /// <summary>  
        /// Logic executed during the Awake phase. Override to add custom behavior.  
        /// </summary>  
        protected virtual void OnAwake() { }

        #endregion

        /// <summary>  
        /// Transitions the state machine to the specified <see cref="State"/>.  
        /// </summary>  
        /// <param name="nextState">The next <see cref="State"/> to transition to.</param>  
        internal virtual void TransitionState(State nextState)
        {
            if (!Application.isPlaying ||
                nextState == null ||
                nextState == CurrentState
               ) return;

            transitionCursorDest = nextState;

            try
            {
                if(CurrentState != null && CurrentState.Parent == nextState.Parent)
                {
                    CurrentState.DoExit(nextState);
                    EnterState(nextState);
                }
                else
                {
                    if (CurrentState != null)
                    {
                        transitionCursorStart = CurrentState;
                        transitionBalance = nextState.Layer - CurrentState.Layer;

                        while (transitionBalance != 0 || transitionCursorStart.Parent != transitionCursorDest.Parent)
                        {
                            transitionBalanceTemp = transitionBalance;
                            if (transitionBalanceTemp < 1 && transitionCursorStart.Layer != 0)
                            {
                                transitionCursorStart.DoExit(nextState);
                                transitionCursorStart = transitionCursorStart.Parent;
                                transitionBalance++;
                            }
                            if (transitionBalanceTemp > -1 && transitionCursorDest.Layer != 0)
                            {
                                ExitStates.Push(transitionCursorDest);
                                transitionCursorDest = transitionCursorDest.Parent;
                                transitionBalance--;
                            }
                        }
                    }

                    if (transitionCursorStart != null) transitionCursorStart.DoExit(nextState);
                    //if (transitionCursorDest != null) ExitStates.Push(transitionCursorDest); 
                }

                while (ExitStates.Count > 0 || transitionCursorDest.HasChildren)
                {
                    EnterState(transitionCursorDest);

                    transitionCursorDest = ExitStates.Count > 0
                        ? ExitStates.Pop()
                        : transitionCursorDest.Children[0];
                }
                if (transitionCursorDest != null) EnterState(transitionCursorDest);
                if (nextState != transitionCursorDest) nextState = transitionCursorDest;

                CurrentState = nextState;
            }
            finally
            {
                transitionCursorDest = null;
                transitionCursorStart = null;
                transitionBalance = 0;
                transitionBalanceTemp = 0;
                ExitStates.Clear();
                AfterStateTransition?.Invoke();
                //Cleanup
            }
             
            void EnterState(State target)
            {
                if(target.Parent != null && target != null) target.Parent.CurrentChild = target;
                target.DoEnter(CurrentState);
            }
        }

        /// <summary>  
        /// Stack used for managing state transitions.  
        /// </summary>  
        private static Stack<State> ExitStates = new();
        private static State transitionCursorStart;
        private static State transitionCursorDest;
        private static int transitionBalance = 0;
        private static int transitionBalanceTemp = 0;
        public System.Action AfterStateTransition;

        /// <summary>  
        /// Builds the state machine by setting up its states.  
        /// </summary>  
        public void Build() => Setup(this, this, -1, true);

        /// <summary>  
        /// Marks the state machine as dirty, requiring a rebuild.  
        /// </summary>  
        public void MarkDirty()
        {
            StatesSetup = false;
#if UNITY_EDITOR
            EditorUtility.SetDirty(this);
#endif
        }

        /// <summary>
        /// Sends a <see cref="Signal"/> through the <see cref="StateMachineH.SignalManager"/> if it exists.
        /// </summary>
        /// <param name="signal">The input signal. Accepts just a string.</param>
        /// <returns></returns>
        public bool SendSignal(Signals.Signal signal) => SignalManager != null && SignalManager.FireSignal(signal);
    }
}