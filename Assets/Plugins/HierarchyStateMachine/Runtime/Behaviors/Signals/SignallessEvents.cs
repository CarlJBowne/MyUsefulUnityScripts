using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;


#if ULT_EVENTS
using EVENT = UltEvents.UltEvent;
#else
using EVENT = UnityEngine.Events.UnityEvent;
#endif

namespace SLS.StateMachineH.Utils
{
    [Tooltip("A list of events that can be fired by index, completely disconnected from both State architecture and the horrors of Serialized Polymorphs. Use if you want a way to serialize Events by a set ID without giving this State a SignalNode. If you already have a Signal Node, just use that. You can fire by number.")]
    public class SignallessEvents : MonoBehaviour
    {
        public List<EVENT> events = new();
        public void FireEvent(int i)
        {
            if (i < 0 || i >= events.Count) return;
            events[i].Invoke();
        } 
    }
}
