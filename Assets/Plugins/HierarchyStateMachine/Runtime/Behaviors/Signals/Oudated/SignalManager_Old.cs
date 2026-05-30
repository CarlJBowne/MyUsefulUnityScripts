using System;
using System.Collections.Generic;
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
    public class SignalManager_Old : SignalNode_Old
    {

        /// <summary>  
        /// Indicates whether signals should be queued if they cannot be fired immediately.  
        /// </summary>  
        public bool queueSignals = true;

        /// <summary>  
        /// Attempts to get a <see cref="SignalNode"/> from the active state.  
        /// </summary>  
        /// <returns>The current signal node.</returns>  
        public SignalNode_Old GetCurrentNode() => Machine.CurrentState.GetComponent<SignalNode_Old>();

        /// <summary>  
        /// Attempts to retrieve the current signal node from the active state.  
        /// </summary>  
        /// <param name="signalNode">The retrieved signal node, if found.</param>  
        /// <returns>True if the signal node was found; otherwise, false.</returns>  
        public bool TryCurrentNode(out SignalNode_Old signalNode) => Machine.CurrentState.TryGetComponent(out signalNode);

        /// <summary>  
        /// Fires a signal, invoking its associated event or queuing it if necessary.  
        /// </summary>  
        /// <param name="signal">The signal to fire.</param>  
        /// <returns>True if the signal was successfully fired; otherwise, false.</returns>  
        public bool FireSignal(Signal signal, bool fromQueue = false)
        {
            bool signalFired = false;
            if (TryCurrentNode(out SignalNode_Old signalNode) && signalNode.FireSignal(signal.name)) signalFired = true;
            else if (signals.ContainsKey(signal.name))
            {
                signals[signal]?.Invoke();
                signalFired = true;
            }

            if (fromQueue) QueueNext();
            else if (!signalFired && queueSignals && signal.queueTime > 0f) QueueSignal(signal);

            return signalFired;
        }

        public bool FireSignalBasic(string signalName) => FireSignal(new Signal(signalName));

        /// <summary>  
        /// Locks the current signal node, preventing signals from being fired.  
        /// </summary>  
        new public void Lock()
        {
            if (TryCurrentNode(out SignalNode_Old signalNode)) signalNode.Lock();
        }

        /// <summary>  
        /// Unlocks the current signal node, allowing signals to be fired.  
        /// </summary>  
        new public void Unlock()
        {
            if (TryCurrentNode(out SignalNode_Old signalNode))
            {
                signalNode.Unlock();
                if (queueSignals && SignalQueue.Count > 0) FireSignal(SignalQueue.Dequeue());
            }
        }

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

        new public bool Locked => TryCurrentNode(out SignalNode_Old signalNode) && signalNode.Locked;
    }
}
