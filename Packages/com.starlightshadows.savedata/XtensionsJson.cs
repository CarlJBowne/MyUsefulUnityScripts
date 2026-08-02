using System;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace SLS.SaveData
{
    public static class XtensionsJson
    {
        public static void RemoveIfNull(this JObject THIS, string name)
        {
            if (THIS.ContainsKey(name) && THIS[name].Type is JTokenType.Null) 
                THIS.Remove(name);
        }
    }


    #region SerializableStructs

    public static class SerializableStructs
    {

        public static object Serializable(object input)
        {
            object result = input;

            if (input.GetType() == typeof(UnityEngine.Vector2)) result = (Vector2)(UnityEngine.Vector2)input;
            else if (input.GetType() == typeof(UnityEngine.Vector3)) result = (Vector3)(UnityEngine.Vector3)input;
            else if (input.GetType() == typeof(UnityEngine.Vector4)) result = (Vector4)(UnityEngine.Vector4)input;

            return result;
        }

        [System.Serializable]
        public struct Vector2
        {
            public float x;
            public float y;
            public Vector2(float x, float y)
            {
                this.x = x;
                this.y = y;
            }
            public static implicit operator UnityEngine.Vector2(Vector2 v) => new(v.x, v.y);
            public static explicit operator Vector2(UnityEngine.Vector2 v) => new(v.x, v.y);
        }
        public static Vector2 Serializable(this UnityEngine.Vector2 v) => new(v.x, v.y);
        [System.Serializable]
        public struct Vector3
        {
            public float x;
            public float y;
            public float z;
            public Vector3(float x, float y, float z)
            {
                this.x = x;
                this.y = y;
                this.z = z;
            }
            public static implicit operator UnityEngine.Vector3(Vector3 v) => new(v.x, v.y, v.z);
            public static explicit operator Vector3(UnityEngine.Vector3 v) => new(v.x, v.y, v.z);
        }
        public static Vector3 Serializable(this UnityEngine.Vector3 v) => new(v.x, v.y, v.z);
        [System.Serializable]
        public struct Vector4
        {
            public float x;
            public float y;
            public float z;
            public float w;
            public Vector4(float x, float y, float z, float w)
            {
                this.x = x;
                this.y = y;
                this.z = z;
                this.w = w;
            }
            public static implicit operator UnityEngine.Vector4(Vector4 v) => new(v.x, v.y, v.z, v.w);
            public static explicit operator Vector4(UnityEngine.Vector4 v) => new(v.x, v.y, v.z, v.w);
        }
        public static Vector4 Serializable(this UnityEngine.Vector4 v) => new(v.x, v.y, v.z, v.w);


        public static JObject Serialize(this UnityEngine.Vector3 v) => new()
        {
            ["x"] = v.x,
            ["y"] = v.y,
            ["z"] = v.z,
        };
        public static UnityEngine.Vector3 Deserialize(this UnityEngine.Vector3 v, JObject input)
        {
            if (input == null)
            {
                v = UnityEngine.Vector3.zero;
                return v;
            }
            v.x = (float)input["x"];
            v.y = (float)input["y"];
            v.z = (float)input["z"];
            return v;
        }
        public static JObject Serialize(this UnityEngine.Vector2 v) => new()
        {
            ["x"] = v.x,
            ["y"] = v.y,
        };
        public static UnityEngine.Vector2 Deserialize(this UnityEngine.Vector2 v, JObject input)
        {
            if (input == null)
            {
                v = UnityEngine.Vector2.zero;
                return v;
            }
            v.x = (float)input["x"];
            v.y = (float)input["y"];
            return v;
        }
        public static JObject Serialize(this UnityEngine.Vector4 v) => new()
        {
            ["x"] = v.x,
            ["y"] = v.y,
            ["z"] = v.z,
            ["w"] = v.w,
        };
        public static UnityEngine.Vector4 Deserialize(this UnityEngine.Vector4 v, JObject input)
        {
            if (input == null)
            {
                v = UnityEngine.Vector4.zero;
                return v;
            }
            v.x = (float)input["x"];
            v.y = (float)input["y"];
            v.z = (float)input["z"];
            v.w = (float)input["w"];
            return v;
        }





    }

    #endregion

    //Generic FilePath class, intersting, but probably not useful.
    public struct FilePath
    {
        public string path;
        public string filename;
        public string extension;
        public FilePath(string path, string filename, string extension)
        {
            this.path = path;
            this.filename = filename;
            this.extension = extension;
        }
        public readonly string Fullpath => Path.Combine(path, $"{filename}.{extension}");
        public static implicit operator string(FilePath obj) => Path.Combine(obj.path, $"{obj.filename}.{obj.extension}");
    }
}