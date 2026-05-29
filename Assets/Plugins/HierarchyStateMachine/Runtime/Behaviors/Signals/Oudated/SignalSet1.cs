using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using SLS.StateMachineH.Utils;

#if ULT_EVENTS
using EVENT = UltEvents.UltEvent;
#else
using EVENT = UnityEngine.Events.UnityEvent;
#endif

namespace SLS.StateMachineH.Signals
{
    /// <summary>  
    /// Represents a dictionary of signals, where each signal is associated with a unique string key.  
    /// </summary>  
    [Serializable]
    internal class SignalSet_Old : SerializedDictionary<string, EVENT> { }
}
