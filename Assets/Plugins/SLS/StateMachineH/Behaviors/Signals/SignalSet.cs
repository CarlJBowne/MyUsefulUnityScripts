using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using SLS.StateMachineH.Utils;
using SLS.ListUtilities;



#if ULT_EVENTS
using EVENT = UltEvents.UltEvent;
#else
using EVENT = UnityEngine.Events.UnityEvent;
#endif

namespace SLS.StateMachineH.Signals
{
    /// <summary>  
    /// Represents a dictionary of signals, where each signal is associated with a unique string name and int key 
    /// </summary>  
    [Serializable]
    public class SignalSet : HashedListS<EVENT> { }

    /// <summary>  
    /// Represents a signal with properties for queue time, lock behavior, and duplicate allowance.  
    /// </summary>  
    public class Signal
    {
        /// <summary>  
        /// Initializes a new instance of the <see cref="Signal"/> class.  
        /// </summary>  
        /// <param name="name">The name of the signal.</param>  
        /// <param name="queueTime">The time the signal should remain in the queue. Default is 0.5 seconds.</param>  
        /// <param name="ignoreLock">Indicates whether the signal should ignore lock conditions.</param>  
        /// <param name="allowDuplicates">Indicates whether duplicate signals are allowed.</param>  
        public Signal(string name, float queueTime = DEFAULT_QUEUE_TIME, bool ignoreLock = false, bool allowDuplicates = false)
        {
            this.name = name;
            this.queueTime = queueTime;
            this.ignoreLock = ignoreLock;
            this.allowDuplicates = allowDuplicates;
        }

        /// <summary>  
        /// The identifier of the <see cref="Signal"/>, compared to a <see cref="SignalManager"/> or <see cref="SignalNode"/>'s dictionaries.
        /// </summary>  
        public string name;

        /// <summary>  
        /// The time the <see cref="Signal"/> should remain in the queue. 0 means it will not be queued.
        /// </summary>  
        public float queueTime = .5f;

        /// <summary>  
        /// A value indicating whether the <see cref="Signal"/> should ignore lock conditions.  
        /// </summary>  
        public bool ignoreLock = false;

        /// <summary>  
        /// A value indicating whether duplicate <see cref="Signal"/>s are allowed among the queue.  
        /// </summary>  
        public bool allowDuplicates = false;

        /// <summary>  
        /// Implicitly converts a <see cref="Signal"/> instance to its name as a string.  
        /// </summary>  
        /// <param name="signal">The signal to convert.</param>  
        /// <returns>The name of the signal.</returns>  
        public static implicit operator string(Signal signal) => signal.name;

        /// <summary>  
        /// Implicitly converts a string to a <see cref="Signal"/> instance.  
        /// </summary>  
        /// <param name="name">The name of the signal.</param>  
        /// <returns>A new <see cref="Signal"/> instance with the specified name.</returns>  
        public static implicit operator Signal(string name) => new(name);

        public Signal NoQueue() => new(name, 0, ignoreLock, allowDuplicates);

        public static bool operator ==(Signal signalL, Signal signalR) => signalL.name == signalR.name;
        public static bool operator !=(Signal signalL, Signal signalR) => signalL.name != signalR.name;

        /// <summary>  
        /// Determines whether the specified object is equal to the current <see cref="Signal"/>.  
        /// </summary>  
        /// <param name="obj">The object to compare with the current signal.</param>  
        /// <returns>True if the specified object is equal to the current signal; otherwise, false.</returns>  
        public override bool Equals(object obj) => obj is Signal signal && this.name == signal.name;

        /// <summary>  
        /// Serves as the default hash function.  
        /// </summary>  
        /// <returns>A hash code for the current <see cref="Signal"/>.</returns>  
        public override int GetHashCode() => string.IsNullOrEmpty(name) ? Animator.StringToHash(name) : 0;

        public const float DEFAULT_QUEUE_TIME = 0.5f;

    }
}
