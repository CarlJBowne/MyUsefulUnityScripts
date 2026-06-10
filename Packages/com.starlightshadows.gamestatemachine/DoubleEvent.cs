using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UltEvents;
using UnityEngine.Events;

namespace SLS.GameStateMachine
{
    [System.Serializable]
    public class DualEvent
    {
#if ULT_EVENTS
        [UnityEngine.SerializeField] private UltEvent Real;

        public static DualEvent operator +(DualEvent r, Action l)
        {
            r.Real.DynamicCalls += l;
            return r;
        }
        public static DualEvent operator -(DualEvent r, Action l)
        {
            r.Real.DynamicCalls -= l;
            return r;
        }

        public void Invoke() => Real?.Invoke();

#else

        [UnityEngine.SerializeField] private UnityEvent Un;
        [UnityEngine.SerializeField] private Action Act;

        public static DualEvent operator +(DualEvent r, Action l)
        {
            r.Act += l;
            return r;
        }
        public static DualEvent operator -(DualEvent r, Action l)
        {
            r.Act -= l;
            return r;
        }
        public static DualEvent operator +(DualEvent r, UnityAction l)
        {
            r.Un.AddListener(l);
            return r;
        }
        public static DualEvent operator -(DualEvent r, UnityAction l)
        {
            r.Un.RemoveListener(l);
            return r;
        }

        public void Invoke()
        {
            Un?.Invoke();
            Act?.Invoke();
        } 
#endif
    }
}
