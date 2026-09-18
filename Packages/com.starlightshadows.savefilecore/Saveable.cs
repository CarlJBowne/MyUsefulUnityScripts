using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace SLS.SaveFileCore
{
    [System.Serializable]
    public abstract class Saveable<T> : ICloneable<T> where T : Saveable<T>
    {
        public abstract T Clone(T source);
        public static void Clone(T from, T to) => to.Clone(from);
    }

}
