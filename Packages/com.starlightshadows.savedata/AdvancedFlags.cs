using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Utilities.JSON;


#if UNITY_EDITOR
using UnityEditor;
#endif

namespace SLS.SaveData
{
    [Serializable]
    public abstract class FlagBase : Polymorph
    {

        //public bool IsType<T>() => type == TypeEnumFromCType<T>();
        //public static Type TypeEnumFromCType<T>()
        //       => typeof(T) == typeof(bool) ? Type.Bool
        //        : typeof(T) == typeof(int) ? Type.Int
        //        : typeof(T) == typeof(float) ? Type.Float
        //        : typeof(T) == typeof(UnityEngine.Vector3) ? Type.Vector3
        //        : typeof(T) == typeof(string) ? Type.String
        //        : throw new System.Exception("No matching FlagType for type " + typeof(T).Name);

        public abstract object valueObject { get; protected set; }

        public event Action<object> OnValueChanged;

        //public abstract Type type { get; }

        // Shared TrySetValue(object value) implementation
        public virtual bool TrySetValue(object value)
        {
            if (value == null || valueObject.GetType() != value.GetType()) return false;
            valueObject = value;
            return true;
        }

        public bool TryGetValue<T>(out T value)
        {
            if (valueObject.GetType() == typeof(T))
            {
                value = (T)valueObject;
                OnValueChanged?.Invoke(value);
                return true;
            }
            value = default;
            return false;
        }

        public bool TrySetValue<T>(T value)
        {
            if (valueObject.GetType() == typeof(T))
            {
                valueObject = value;
                return true;
            }
            return false;
        }

        public abstract FlagBase Clone(FlagBase source);

        public abstract void LoadFromJson(JToken input);
        public abstract JToken SaveToJson();

        public static implicit operator JToken(FlagBase source) => source.SaveToJson();

        //public static FlagBase CreateInstanceFromEnum(Type type)
        //{
        //    return type switch
        //    {
        //        Type.Bool => new Boolean(),
        //        Type.Int => new Integer(),
        //        Type.Float => new Float(),
        //        Type.Vector3 => new Vector3(),
        //        Type.String => new String(),
        //        _ => null,
        //    };
        //}


        //public enum Type
        //{
        //    Bool,
        //    Int,
        //    Float,
        //    Vector3,
        //    String,
        //}

        public class Flag<T> : FlagBase where T : struct
        {
            private static Type[] ValidTypes =
            {
                typeof(int),
                typeof(float),
                typeof(bool),
                typeof(Vector3),
            };

            public T value;
            new public event Action<T> OnValueChanged;

            public override object valueObject
            {
                get => value;
                protected set
                {
                    if (typeof(T) != value.GetType()) return;
                    this.value = (T)value;
                    OnValueChanged?.Invoke((T)value);
                }
            }
            //public override Type type { get; }

            public override FlagBase Clone(FlagBase source)
            {
                if (source == null) return this;
                if (source.valueObject.GetType() != typeof(T)) return this;
                valueObject = source.valueObject;
                return this;
            }
            public override void LoadFromJson(JToken input)
            {
                if (typeof(T) == typeof(Vector3))
                {
                    JArray arr = input as JArray;
                    valueObject = new Vector3(arr[0].ToObject<float>(), arr[1].ToObject<float>(), arr[2].ToObject<float>());
                }
                else value = input.ToObject<T>();
                OnValueChanged?.Invoke(value);
            }
            public override JToken SaveToJson()
            {
                if (typeof(T) == typeof(Vector3))
                {
                    Vector3 v = (Vector3)valueObject;
                    return new JArray { v.x, v.y, v.z };
                }
                else return JToken.FromObject(value);
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