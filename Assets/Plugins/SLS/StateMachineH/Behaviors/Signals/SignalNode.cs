using UnityEngine;
using SLS.ListUtilities;


#if ULT_EVENTS
using EVENT = UltEvents.UltEvent;
#else
using EVENT = UnityEngine.Events.UnityEvent;
#endif


namespace SLS.StateMachineH.Signals
{
    /// <summary>  
    /// Represents a node in the <see cref="StateMachine"/> that can recieve signals and enact events.  
    /// </summary>  
    [RequireComponent(typeof(State))]
    public class SignalNode : StateBehavior
    {
        /// <summary>  
        /// The signals associated with this node.  
        /// </summary>  
        [SerializeField] internal SignalSet signals = new();

        [SerializeField] public bool blockParentNodes = false;
        [SerializeField] public bool blockGlobalNode = false;
        [SerializeField] private SignalManager manager;

        /// <summary>  
        /// Indexer to access events associated with a signal name.  
        /// </summary>  
        /// <param name="name">The name of the signal.</param>  
        /// <returns>The event associated with the signal name.</returns>  
        public EVENT this[string name] => signals[name];
        /// <summary>
        /// Attempts to retrieve an event associated with the specified signal name.
        /// </summary>
        public bool TryGet(string name, out EVENT Result, bool USENAME = false) =>
            !USENAME ? signals.TryGet(name.Hash(), out Result)
            : signals.TryGet(name, out Result);

        /// <summary>
        /// Returns whether this Signal Node contains an event with the specified name.
        /// </summary>
        public bool ContainsName(string name, bool USENAME = false) =>
            !USENAME ? signals.ContainsKey(name.Hash())
            : signals.ContainsName(name);
        /// <summary>
        /// Returns whether this Signal Node contains an event with the specified name.
        /// </summary>
        public bool ContainsKey(int key) => signals.ContainsKey(key);

        /// <summary>
        /// Removes the event with the specified name.
        /// </summary>
        /// <param name="name"></param>
        public void Remove(string name) => signals.Remove(name);
        /// <summary>
        /// Removes the event with the specified name.
        /// </summary>
        /// <param name="name"></param>
        public void Remove(int ID) => signals.Remove(ID);

        public bool FireEvent(string signalName)
        {
            if (signals.ContainsKey(signalName.Hash()))
            {
                signals[signalName]?.Invoke();
                return true;
            }
            else return false;
        }
        public bool FireEvent(int hash)
        {
            if (signals.ContainsKey(hash))
            {
                signals[hash]?.Invoke();
                return true;
            }
            else return false;
        }
        public bool FireEventIndex(int id)
        {
            if (signals.Count > id && id >= 0)
            {
                signals.ValueFromIndex(id)?.Invoke();
                return true;
            }
            else return false;
        }

        /// <summary>  
        /// The value indicating whether the node is currently locked.  
        /// </summary>  
        public bool Locked
        {
            get => manager.Locked;
            set => manager.Locked = value;
        }
        /// <summary>  
        /// Unlocks the node, allowing signals to be fired.  
        /// </summary>  
        public void Unlock() => Locked = false;
        /// <summary>  
        /// Locks the node, preventing signals from being fired.  
        /// </summary>  
        public void Lock() => Locked = true;

        protected override void OnSetup() => manager = GetComponentFromMachine<SignalManager>();

        /// <summary>  
        /// Called when entering a state. Locks the node if <see cref="lockOnEnter"/> is true.  
        /// </summary>  
        /// <param name="prev">The previous state.</param>  
        /// <param name="isFinal">Indicates whether this is the final state.</param>  
        protected override void OnEnter(State prev, bool isFinal) => manager.Register(this);
        /// <summary>
        /// Called when exiting a state. Unlocks the node to ensure no stale lockage.
        /// </summary>
        /// <param name="next"></param>
        protected override void OnExit(State next)
        {
            manager.Deregister(this);
            Locked = false;
        }

    }
}
 