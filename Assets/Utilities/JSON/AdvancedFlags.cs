using Newtonsoft.Json.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Utilities.JSON;


#if UNITY_EDITOR
using UnityEditor;
#endif

namespace SaveSystem.Flags
{
    [System.Serializable]
    public abstract class Flag
    {

        public bool IsType<T>() => type == TypeEnumFromCType<T>();
        public static Type TypeEnumFromCType<T>()
               => typeof(T) == typeof(bool) ? Type.Bool
                : typeof(T) == typeof(int) ? Type.Int
                : typeof(T) == typeof(float) ? Type.Float
                : typeof(T) == typeof(UnityEngine.Vector3) ? Type.Vector3
                : typeof(T) == typeof(string) ? Type.String
                : throw new System.Exception("No matching FlagType for type " + typeof(T).Name);

        public abstract object valueObject { get; set; }
        public abstract Type type { get; }

        // Shared TrySetValue(object value) implementation
        public virtual bool TrySetValue(object value)
        {
            if(value == null || valueObject.GetType() != value.GetType()) return false;
            valueObject = value;
            return true;
        }

        public bool TryGetValue<T>(out T value)
        {
            if (IsType<T>())
            {
                value = (T)valueObject;
                return true;
            }
            value = default;
            return false;
        }

        public bool TrySetValue<T>(T value)
        {
            if (IsType<T>())
            {
                valueObject = value;
                return true;
            }
            return false;
        }

        public abstract Flag Clone(Flag target = null);

        public abstract void LoadFromJson(JToken input);
        public abstract JToken SaveToJson();

        public static Flag CreateInstanceFromEnum(Type type)
        {
            return type switch
            {
                Type.Bool => new Boolean(),
                Type.Int => new Integer(),
                Type.Float => new Float(),
                Type.Vector3 => new Vector3(),
                Type.String => new String(),
                _ => null,
            };
        }


        public enum Type
        {
            Bool,
            Int,
            Float,
            Vector3,
            String,
        }

        public class Boolean : Flag
        {
            public bool value;

            public override object valueObject
            {
                get => value;
                set { if (value is bool B) this.value = B; }
            }

            public override Type type => Type.Bool;

            public override Flag Clone(Flag target = null)
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

        public class Integer : Flag
        {
            public int value;

            public override object valueObject
            {
                get => value;
                set { if (value is int B) this.value = B; }
            }

            public override Type type => Type.Int;
            public override Flag Clone(Flag target = null)
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

        public class Float : Flag
        {
            public float value;

            public override object valueObject
            {
                get => value;
                set { if (value is float B) this.value = B; }
            }

            public override Type type => Type.Float;

            public override Flag Clone(Flag target = null)
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

        public class Vector3 : Flag
        {
            public UnityEngine.Vector3 value;

            public override object valueObject
            {
                get => value;
                set { if (value is UnityEngine.Vector3 B) this.value = B; }
            }

            public override Type type => Type.Vector3;
            public override Flag Clone(Flag target = null)
            {
                if (target is not Vector3 t) t = new Vector3();
                t.value = value;
                return t;
            }

            public override JToken SaveToJson() => value.Serialize();
            public override void LoadFromJson(JToken input) => value.Deserialize((JObject)input);
        }

        public class String : Flag
        {
            public string value;

            public override object valueObject
            {
                get => value;
                set { if (value is string B) this.value = B; }
            }

            public override Type type => Type.String;
            public override Flag Clone(Flag target = null)
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
        }
    }
}
