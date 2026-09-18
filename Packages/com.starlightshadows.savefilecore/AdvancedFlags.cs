using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SLS.GeneralUtilities.EventTickets;
using SLS.ListUtilities;




#if UNITY_EDITOR
using UnityEditor;
#endif

namespace SLS.SaveFileCore
{
    [Serializable]
    public abstract class Flag : Polymorph
    {
        public abstract object valueObject { get; set; }
        public abstract Type ValueType { get; }

        public abstract void LoadFromJson(JToken input);
        public abstract JToken SaveToJson();
        public abstract Flag Clone(Flag source);

        protected abstract void CallValueChangedCallback();

        public static implicit operator JToken(Flag source) => source.SaveToJson();
        /// <summary>
        /// psudeo assignment operator.
        /// </summary>
        public static Flag operator &(Flag flag, object value)
        {
            flag.valueObject = value;
            return flag;
        }


        //[Polymorph.ValidTypes(typeof(int), typeof(float), typeof(bool), typeof(UnityEngine.Vector3))]
        public abstract class Generic<T> : Flag
        {
            public T Value
            {
                get => value;
                set
                {
                    if (this.value.Equals(value)) return;
                    this.value = value;
                    CallValueChangedCallback();
                }
            }
            public override object valueObject
            {
                get => value;
                set
                {
                    if (value.GetType() != typeof(T)) return;
                    this.value = (T)value;
                    CallValueChangedCallback();
                }
            }
            [SerializeField] protected T value;

            public override Type ValueType => typeof(T);


            public event Action<T> OnValueChanged;

            public EventTicket RegisterCallback(Action<T> callback) =>
                EventTicket.Action(OnValueChanged, callback);

            protected override void CallValueChangedCallback() => OnValueChanged?.Invoke(Value);

            public override Flag Clone(Flag source)
            {
                if (source == null) return this;
                if (source.valueObject.GetType() != typeof(T)) return this;
                valueObject = source.valueObject;
                return this;
            }
            public Generic<T> Clone(Generic<T> source)
            {
                if (source == null) return this;
                Value = source.Value;
                return this;
            }

            public static implicit operator T(Generic<T> input) => input.Value;
            public static Generic<T> operator &(Generic<T> flag, T value)
            {
                flag.Value = value;
                return flag;
            }
        }

        [Serializable]
        public class Bool : Generic<bool>
        {
            public override void LoadFromJson(JToken input)
            {
                if (input == null || input.Type != JTokenType.Boolean) return;
                Value = (bool)input;
            }
            public override JToken SaveToJson() => (JToken)Value;
        }
        [Serializable]
        public class Int : Generic<int>
        {
            public override void LoadFromJson(JToken input)
            {
                if (input == null || input.Type != JTokenType.Integer) return;
                Value = (int)input;
            }
            public override JToken SaveToJson() => (JToken)Value;
        }
        [Serializable]
        public class Float : Generic<float>
        {
            public override void LoadFromJson(JToken input)
            {
                if (input == null || input.Type != JTokenType.Float) return;
                Value = (float)input;
            }
            public override JToken SaveToJson() => (JToken)Value;
        }
        [Serializable]
        public class Vector3 : Generic<UnityEngine.Vector3>
        {
            public override void LoadFromJson(JToken input)
            {
                if (input == null || input is not JArray array || array.Count != 3) return;
                Value = new((float)array[0], (float)array[1], (float)array[2]);
            }
            public override JToken SaveToJson() => new JArray { value.x, value.y, value.z };
        }
        [Serializable]
        public class String : Generic<string>
        {
            public override void LoadFromJson(JToken input)
            {
                if (input == null || input.Type != JTokenType.String) return;
                Value = (string)input;
            }
            public override JToken SaveToJson() => (JToken)Value;
        }
        [Serializable]
        public class Char : Generic<char>
        {
            public override void LoadFromJson(JToken input)
            {
                if (input == null || input.Type != JTokenType.Integer) return;
                Value = (char)(int)input;
            }
            public override JToken SaveToJson() => (JToken)(int)Value;
        }

        [Serializable]
        public class Collection : Dictionary<Flag>
        {
            // Get or create a Flag<T> under the given name (uses string-name storage)
            public Generic<T> GetOrCreate<T>(string name)
            {
                if (TryGet(name, out Flag existing, true) && existing is Generic<T> f) return f;

                Flag newFlag = null;
                if (typeof(T) == typeof(bool)) newFlag = new Bool();
                if (typeof(T) == typeof(int)) newFlag = new Int();
                if (typeof(T) == typeof(float)) newFlag = new Float();
                if (typeof(T) == typeof(UnityEngine.Vector3)) newFlag = new Vector3();
                if (typeof(T) == typeof(string)) newFlag = new String();
                if (typeof(T) == typeof(char)) newFlag = new Char();
                this[name] = newFlag;
                return newFlag as Generic<T>;

            }

            public void Set<T>(string name, T value)
            {
                if (!ContainsName(name)) return;
                if (this[name].ValueType != typeof(T)) return;
                if (this[name] is not Generic<T> gen) return;
                gen.Value = value;
            }

            public bool TryGet<T>(string name, out T value)
            {
                value = default;
                if (!ContainsName(name)) return false;
                if (this[name].ValueType != typeof(T)) return false;
                if (this[name] is not Generic<T> gen) return false;

                value = gen.Value;
                return true;
            }

            public EventTicket Subscribe<T>(string name, Action<T> callback) =>
                TryGet(name, out Generic<T> res) ? res.RegisterCallback(callback) : null;

            public string[] AllNamesOfType<T>() => AllNamesOfType(typeof(T));
            public string[] AllNamesOfType(Type type)
            {
                List<string> result = new();
                for (int i = 0; i < Count; i++)
                    if (Values[i].ValueType == type)
                        result.Add(Names[i]);
                return result.ToArray();
            }

            public void Clone(Collection source, DictionaryCloneOp op = DictionaryCloneOp.Transfer)
            {
                if (source == null) return;
                if (Count == 0) op = DictionaryCloneOp.TransferAndAdd;
                if (op is DictionaryCloneOp.ReplaceEntirely) Clear();
                for (int i = 0; i < source.Count; i++)
                {
                    if (!serializedKeys.Contains(source.serializedKeys[i]) && op is not DictionaryCloneOp.Transfer)
                        this.Add(source.Names[i], Activator.CreateInstance(source.Values[i].GetType()) as Flag);
                    this[source.Keys[i]].Clone(source.Values[i]);
                }
            }

            public void LoadFromJson(JObject list)
            {
                if (!list.HasValues) return;
                foreach (JProperty prop in list.Properties())
                    if (ContainsName(prop.Name))
                        this[prop.Name].LoadFromJson(prop.Value);
            }
            public JObject SaveToJson()
            {
                JObject result = new();
                for (int i = 0; i < Count; i++)
                    result.Add(new JProperty(Names[i], Values[i].SaveToJson()));
                return result;
            }
        }

        public class OneTypeCollection<T> : Dictionary<Generic<T>>
        {
            // Get or create a Flag<T> under the given name (uses string-name storage)
            public Generic<T> GetOrCreate(string name)
            {
                if (TryGet(name, out Generic<T> existing, true) && existing is Generic<T> f) return f;

                Flag newFlag = null;
                if (typeof(T) == typeof(bool)) newFlag = new Bool();
                if (typeof(T) == typeof(int)) newFlag = new Int();
                if (typeof(T) == typeof(float)) newFlag = new Float();
                if (typeof(T) == typeof(UnityEngine.Vector3)) newFlag = new Vector3();
                if (typeof(T) == typeof(string)) newFlag = new String();
                if (typeof(T) == typeof(char)) newFlag = new Char();
                this[name] = newFlag as Generic<T>;
                return newFlag as Generic<T>;

            }

            public void Set(string name, T value)
            {
                if (!ContainsName(name)) return;
                if (this[name].ValueType != typeof(T)) return;
                if (this[name] is not Generic<T> gen) return;
                gen.Value = value;
            }

            public bool TryGet(string name, out T value)
            {
                value = default;
                if (!ContainsName(name)) return false;
                if (this[name].ValueType != typeof(T)) return false;
                if (this[name] is not Generic<T> gen) return false;

                value = gen.Value;
                return true;
            }

            public EventTicket Subscribe(string name, Action<T> callback) =>
                TryGet(name, out Generic<T> res) ? res.RegisterCallback(callback) : null;

            public void Clone(OneTypeCollection<T> source, DictionaryCloneOp op = DictionaryCloneOp.Transfer)
            {
                if (source == null) return;
                if (Count == 0) op = DictionaryCloneOp.TransferAndAdd;
                if (op is DictionaryCloneOp.ReplaceEntirely) Clear();
                for (int i = 0; i < source; i++)
                {
                    if (!serializedKeys.Contains(source.serializedKeys[i]) && op is not DictionaryCloneOp.Transfer)
                        this.Add(source.Names[i], Activator.CreateInstance(source.Values[i].GetType()) as Generic<T>);
                    this[source.Keys[i]].Clone(source.Values[i]);
                }
            }

            public void LoadFromJson(JObject list)
            {
                if (!list.HasValues) return;
                foreach (JProperty prop in list.Properties())
                    if (ContainsName(prop.Name))
                        this[prop.Name].LoadFromJson(prop.Value);
            }
            public JObject SaveToJson()
            {
                JObject result = new();
                for (int i = 0; i < Count; i++)
                    result.Add(new JProperty(Names[i], Values[i].SaveToJson()));
                return result;
            }
        }

        public class BoolOnlyCollection : OneTypeCollection<bool>
        {
            public float CompletionOf(float percentage)
            {
                int completeted = 0;
                for (int i = 0; i < Count; i++)
                    if (Values[i].Value)
                        completeted++;
                return completeted / Count * percentage;
            }
            public void Clone(BoolOnlyCollection source, DictionaryCloneOp op = DictionaryCloneOp.Transfer)
            {
                if (source == null) return;
                if (Count == 0) op = DictionaryCloneOp.TransferAndAdd;
                if (op is DictionaryCloneOp.ReplaceEntirely) Clear();
                for (int i = 0; i < source; i++)
                {
                    if (!serializedKeys.Contains(source.serializedKeys[i]) && op is not DictionaryCloneOp.Transfer)
                        this.Add(source.Names[i], Activator.CreateInstance(source.Values[i].GetType()) as Generic<bool>);
                    this[source.Keys[i]].Clone(source.Values[i]);
                }
            }
            public new JArray SaveToJson()
            {
                JArray res = new();
                for (int i = 0; i < Count; i++)
                    if (Values[i].Value)
                        res.Add(Names[i]);
                return res;
            }
            public void LoadFromJson(JArray list)
            {
                if (!list.HasValues) return;
                for (int i = 0; i < list.Count; i++)
                    if (ContainsName(list[i].ToString()))
                        this[list[i].ToString()].Value = true;
            }
        }

        /*
        [Serializable]
        public class Boolean : FlagBase
        {
            public bool value;

            public override object valueObject
            {
                get => value;
                set { if (value is bool B) this.value = B; }
            }

            public override Type type => Type.Bool;

            public override FlagBase Clone(FlagBase target = null)
            {
                if (target is not Boolean t) t = new Boolean();
                t.value = value;
                return t;
            }
            public override JToken SaveToJson() => new JValue(value);
            public override void LoadFromJson(JToken input)
            {
                if (input == null || input.Type != JTokenType.Boolean)
                    return;

                value = input.Value<bool>();
            }
        }
        [Serializable]
        public class Integer : FlagBase
        {
            public int value;

            public override object valueObject
            {
                get => value;
                set { if (value is int B) this.value = B; }
            }

            public override Type type => Type.Int;
            public override FlagBase Clone(FlagBase target = null)
            {
                if (target is not Integer t) t = new Integer();
                t.value = value;
                return t;
            }
            public override JToken SaveToJson() => new JValue(value);
            public override void LoadFromJson(JToken input)
            {
                if (input == null || input.Type != JTokenType.Integer)
                    return;

                value = input.Value<int>();
            }
        }
        [Serializable]
        public class Float : FlagBase
        {
            public float value;

            public override object valueObject
            {
                get => value;
                set { if (value is float B) this.value = B; }
            }

            public override Type type => Type.Float;

            public override FlagBase Clone(FlagBase target = null)
            {
                if (target is not Float t) t = new Float();
                t.value = value;
                return t;
            }
            public override JToken SaveToJson() => new JValue(value);
            public override void LoadFromJson(JToken input)
            {
                if (input == null || (input.Type != JTokenType.Float && input.Type != JTokenType.Integer))
                    return;

                value = input.Value<float>();
            }
        }
        [Serializable]
        public class Vector3 : FlagBase
        {
            public UnityEngine.Vector3 value;

            public override object valueObject
            {
                get => value;
                set { if (value is UnityEngine.Vector3 B) this.value = B; }
            }

            public override Type type => Type.Vector3;
            public override FlagBase Clone(FlagBase target = null)
            {
                if (target is not Vector3 t) t = new Vector3();
                t.value = value;
                return t;
            }

            public override JToken SaveToJson()
            {
                return new JArray(value.x, value.y, value.z);
            }
            public override void LoadFromJson(JToken input)
            {
                JToken t = input;
                value.x = t[0].ToObject<float>();
                value.y = t[1].ToObject<float>();
                value.z = t[2].ToObject<float>();
            }
        }
        [Serializable]
        public class String : FlagBase
        {
            public string value;

            public override object valueObject
            {
                get => value;
                set { if (value is string B) this.value = B; }
            }

            public override Type type => Type.String;
            public override FlagBase Clone(FlagBase target = null)
            {
                if (target is not String t) t = new String();
                t.value = value;
                return t;
            }
            public override JToken SaveToJson() => new JValue(value);
            public override void LoadFromJson(JToken input)
            {
                if (input == null || (input.Type != JTokenType.String && input.Type != JTokenType.Null))
                    return;

                value = input.Type == JTokenType.Null ? null : input.Value<string>();
            }
        }*/
    }

}

/*
public class Vector3Object
{
    public float x, y, z;
    public Vector3Object(float x, float y, float z)
    {
        this.x = x;
        this.y = y;
        this.z = z;
    }
    public Vector3Object(Vector3 s)
    {
        this.x = s.x;
        this.y = s.y;
        this.z = s.z;
    }
    public static implicit operator Vector3Object(Vector3 v) => new(v);
    public static implicit operator Vector3(Vector3Object o) => new(o.x, o.y, o.z);
}
*/