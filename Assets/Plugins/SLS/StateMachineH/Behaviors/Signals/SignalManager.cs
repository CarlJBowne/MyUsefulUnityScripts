using System;
using System.Collections.Generic;
using SLS.ListUtilities;
using UnityEngine;

#if ULT_EVENTS
using EVENT = UltEvents.UltEvent;
#else
using EVENT = UnityEngine.Events.UnityEvent;
#endif

namespace SLS.StateMachineH.Signals
{
    /// <summary>  
    /// Manages signals within a hierarchical state machine.  
    /// Provides functionality for firing signals, queuing signals, and managing signal locks.  
    /// </summary>  
    [RequireComponent(typeof(StateMachine))]
    public class SignalManager : SignalNode
    {

        /// <summary>  
        /// Indicates whether signals should be queued if they cannot be fired immediately.  
        /// </summary>  
        public bool queueSignals = true;


        /// <summary>  
        /// Fires a signal, invoking its associated event or queuing it if necessary.  
        /// </summary>  
        /// <param name="signal">The signal to fire.</param>  
        /// <returns>True if the signal was successfully fired; otherwise, false.</returns>  
        public bool FireSignal(Signal signal, bool fromQueue = false)
        {
            if (Locked && !signal.ignoreLock) return false;
            bool signalFired = false;
            int key = signal.name.Hash();
            int i = NodeStack.Count - 1;
            bool skipToGlobal = false;
            bool skipGlobal = false;

            while (!signalFired && i >= 0)
            {
                if (NodeStack[i].ContainsKey(key))
                {
                    NodeStack[i].FireEvent(key);
                    signalFired = true;
                    break;
                }
                if (NodeStack[i].blockParentNodes) skipToGlobal = true;
                if (NodeStack[i].blockGlobalNode) skipGlobal = true;

                i--;
                if (skipToGlobal) i = 0;
                if (skipGlobal && i == 0) i = -1;
            }

            if (fromQueue) QueueNext();
            else if (!signalFired && queueSignals && signal.queueTime > 0f) QueueSignal(signal);

            return signalFired;
        }

        public bool FireSignalBasic(string signalName) => FireSignal(new Signal(signalName));

        new public bool Locked { get; set; }
        /// <summary>  
        /// Locks the current signal node, preventing signals from being fired.  
        /// </summary>  
        new public void Lock() => Locked = false;

        /// <summary>  
        /// Unlocks the current signal node, allowing signals to be fired.  
        /// </summary>  
        new public void Unlock() => Locked = true;

        /// <summary>  
        /// The queue of signals waiting to be fired.  
        /// </summary>  
        public Queue<Signal> SignalQueue { get; private set; } = new();

        /// <summary>  
        /// The duration of the currently active signal in the queue.  
        /// </summary>  
        public float ActiveSignalLength { get; private set; } = 0f;

        /// <summary>  
        /// The timer for the currently active signal in the queue.  
        /// </summary>  
        public float SignalQueueTimer { get; private set; } = 0f;

        private void QueueSignal(Signal signal)
        {
            if (!queueSignals || signal.queueTime <= 0f || (!signal.allowDuplicates && SignalQueue.Count > 0 && SignalQueue.Peek() == signal)) return;

            SignalQueue.Enqueue(signal);
            if (SignalQueue.Count == 1) QueueNext();
        }
        private void QueueNext()
        {
            if (SignalQueue.Count == 0) return;
            ActiveSignalLength = SignalQueue.Peek().queueTime;
            SignalQueueTimer = ActiveSignalLength;
        }

        /// <summary>  
        /// Updates the signal manager, processing queued signals if necessary.  
        /// </summary>  
        protected override void OnUpdate()
        {
            if (queueSignals && SignalQueue.Count > 0 && ActiveSignalLength > 0f)
            {
                SignalQueueTimer -= Time.deltaTime;
                if (SignalQueueTimer <= 0f)
                {
                    ActiveSignalLength = 0;
                    FireSignal(SignalQueue.Dequeue(), true);
                }
            }
        }

        private List<SignalNode> NodeStack;
        public void Register(SignalNode node) => NodeStack.Add(node);
        public void Deregister(SignalNode node) => NodeStack.Remove(node);
    }
}
