using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace SLS.SaveFileCore
{
    /// <summary>Extension methods for working with JSON tokens and file-operation results.</summary>
    public static class XtensionsJson
    {
        /// <summary>Removes a property from a JSON object when its value is JSON null.</summary>
        /// <param name="THIS">Object to update.</param>
        /// <param name="name">Name of the property to inspect and remove.</param>
        public static void RemoveIfNull(this JObject THIS, string name)
        {
            if (THIS.ContainsKey(name) && THIS[name].Type is JTokenType.Null)
                THIS.Remove(name);
        }

        /// <summary>Invokes an action on a JSON token and returns that token for fluent composition.</summary>
        /// <typeparam name="T">JSON token type.</typeparam>
        /// <param name="THIS">Token to populate.</param>
        /// <param name="func">Action to apply to the token.</param>
        /// <returns>The same token after the action completes.</returns>
        public static T Populate<T>(this T THIS, Action<T> func) where T : JToken
        {
            func(THIS);
            return THIS;
        }

        /// <summary>Creates a JSON delta containing values in <paramref name="current"/> that differ from <paramref name="defaults"/>.</summary>
        /// <param name="current">Current JSON value.</param>
        /// <param name="defaults">Default JSON value to compare against.</param>
        /// <returns>A token containing changed values, or <see langword="null"/> when there are no differences.</returns>
        public static JToken PruneDefaults(this JToken current, JToken defaults)
        {
            if (JToken.DeepEquals(current, defaults))
                return null; // nothing different at this node

            if (current == null) return null;
            if (defaults == null) return current.DeepClone();

            if (current.Type != defaults.Type)
                return current.DeepClone();

            switch (current.Type)
            {
                case JTokenType.Object:
                {
                    var curObj = (JObject)current;
                    var defObj = defaults as JObject ?? new JObject();
                    var outObj = new JObject();
                    foreach (var prop in curObj.Properties())
                    {
                        var defProp = defObj.Property(prop.Name);
                        var prunedChild = PruneDefaults(prop.Value, defProp?.Value);
                        if (prunedChild != null)
                            outObj.Add(prop.Name, prunedChild);
                    }
                    return outObj.HasValues ? outObj : null;
                }
                case JTokenType.Array:
                {
                    // Simple heuristic: if arrays are equal -> prune; if not equal -> keep full current array.
                    var defArr = defaults as JArray;
                    var curArr = current as JArray;
                    if (JToken.DeepEquals(curArr, defArr)) return null;
                    // Optionally implement element-wise pruning here; for now return full current
                    return curArr.DeepClone();
                }
                default:
                    // primitive types -> since not DeepEquals, return current value (replace)
                    return current.DeepClone();
            }
        }

        /// <summary>Applies a JSON delta to a base token, recursively merging object properties.</summary>
        /// <param name="baseToken">Base value to copy and update.</param>
        /// <param name="delta">Changed values to apply; non-object values replace the base value.</param>
        /// <returns>A new token containing the merged value.</returns>
        public static JToken ApplyDelta(this JToken baseToken, JToken delta)
        {
            if (delta == null) return baseToken.DeepClone();
            if (baseToken == null) return delta.DeepClone();

            if (delta.Type != JTokenType.Object || baseToken.Type != JTokenType.Object)
                return delta.DeepClone();

            var baseObj = (JObject)baseToken.DeepClone();
            var deltaObj = (JObject)delta;
            foreach (var prop in deltaObj.Properties())
            {
                baseObj[prop.Name] = ApplyDelta(baseObj[prop.Name], prop.Value);
            }
            return baseObj;
        }

        /// <summary>Determines whether a file operation returned a result other than <see cref="FileOpMessage.Success"/>.</summary>
        /// <param name="source">Operation result to check.</param>
        /// <returns><see langword="true"/> when the operation failed.</returns>
        public static bool IfFail(this FileOpMessage source) => source != FileOpMessage.Success;

        /// <summary>Determines whether a file operation failed and copies its result to an output parameter.</summary>
        /// <param name="source">Operation result to check.</param>
        /// <param name="result">Receives <paramref name="source"/> unchanged.</param>
        /// <returns><see langword="true"/> when the operation failed.</returns>
        public static bool IfFail(this FileOpMessage source, out FileOpMessage result)
        {
            result = source;
            return source != FileOpMessage.Success;
        }
    }


    #region SerializableStructs

    /// <summary>Provides Unity vector serialization helpers and serializable vector value types.</summary>
    public static class SerializableStructs
    {
        /// <summary>Converts a Unity vector to its serializable wrapper when its type is supported.</summary>
        /// <param name="input">Object to convert.</param>
        /// <returns>A serializable vector wrapper for Unity vectors, or the original object for other types.</returns>
        public static object Serializable(object input)
        {
            object result = input;

            if (input.GetType() == typeof(UnityEngine.Vector2)) result = (Vector2)(UnityEngine.Vector2)input;
            else if (input.GetType() == typeof(UnityEngine.Vector3)) result = (Vector3)(UnityEngine.Vector3)input;
            else if (input.GetType() == typeof(UnityEngine.Vector4)) result = (Vector4)(UnityEngine.Vector4)input;

            return result;
        }

        /// <summary>Serializable value type containing the components of a two-dimensional vector.</summary>
        [System.Serializable]
        public struct Vector2
        {
            /// <summary>The horizontal component.</summary>
            public float x;
            /// <summary>The vertical component.</summary>
            public float y;

            /// <summary>Initializes the vector components.</summary>
            /// <param name="x">Horizontal component.</param>
            /// <param name="y">Vertical component.</param>
            public Vector2(float x, float y)
            {
                this.x = x;
                this.y = y;
            }

            /// <summary>Converts this wrapper to a Unity <see cref="UnityEngine.Vector2"/>.</summary>
            /// <param name="v">Wrapper value to convert.</param>
            /// <returns>The equivalent Unity vector.</returns>
            public static implicit operator UnityEngine.Vector2(Vector2 v) => new(v.x, v.y);
            /// <summary>Converts a Unity <see cref="UnityEngine.Vector2"/> to this wrapper.</summary>
            /// <param name="v">Unity vector to convert.</param>
            /// <returns>A wrapper containing the vector's components.</returns>
            public static explicit operator Vector2(UnityEngine.Vector2 v) => new(v.x, v.y);
        }

        /// <summary>Converts a Unity <see cref="UnityEngine.Vector2"/> to its serializable wrapper.</summary>
        /// <param name="v">Unity vector to convert.</param>
        /// <returns>A wrapper containing the vector's components.</returns>
        public static Vector2 Serializable(this UnityEngine.Vector2 v) => new(v.x, v.y);
        /// <summary>Serializable value type containing the components of a three-dimensional vector.</summary>
        [System.Serializable]
        public struct Vector3
        {
            /// <summary>The x component.</summary>
            public float x;
            /// <summary>The y component.</summary>
            public float y;
            /// <summary>The z component.</summary>
            public float z;

            /// <summary>Initializes the vector components.</summary>
            /// <param name="x">X component.</param>
            /// <param name="y">Y component.</param>
            /// <param name="z">Z component.</param>
            public Vector3(float x, float y, float z)
            {
                this.x = x;
                this.y = y;
                this.z = z;
            }

            /// <summary>Converts this wrapper to a Unity <see cref="UnityEngine.Vector3"/>.</summary>
            /// <param name="v">Wrapper value to convert.</param>
            /// <returns>The equivalent Unity vector.</returns>
            public static implicit operator UnityEngine.Vector3(Vector3 v) => new(v.x, v.y, v.z);
            /// <summary>Converts a Unity <see cref="UnityEngine.Vector3"/> to this wrapper.</summary>
            /// <param name="v">Unity vector to convert.</param>
            /// <returns>A wrapper containing the vector's components.</returns>
            public static explicit operator Vector3(UnityEngine.Vector3 v) => new(v.x, v.y, v.z);
        }

        /// <summary>Converts a Unity <see cref="UnityEngine.Vector3"/> to its serializable wrapper.</summary>
        /// <param name="v">Unity vector to convert.</param>
        /// <returns>A wrapper containing the vector's components.</returns>
        public static Vector3 Serializable(this UnityEngine.Vector3 v) => new(v.x, v.y, v.z);
        /// <summary>Serializable value type containing the components of a four-dimensional vector.</summary>
        [System.Serializable]
        public struct Vector4
        {
            /// <summary>The x component.</summary>
            public float x;
            /// <summary>The y component.</summary>
            public float y;
            /// <summary>The z component.</summary>
            public float z;
            /// <summary>The w component.</summary>
            public float w;

            /// <summary>Initializes the vector components.</summary>
            /// <param name="x">X component.</param>
            /// <param name="y">Y component.</param>
            /// <param name="z">Z component.</param>
            /// <param name="w">W component.</param>
            public Vector4(float x, float y, float z, float w)
            {
                this.x = x;
                this.y = y;
                this.z = z;
                this.w = w;
            }

            /// <summary>Converts this wrapper to a Unity <see cref="UnityEngine.Vector4"/>.</summary>
            /// <param name="v">Wrapper value to convert.</param>
            /// <returns>The equivalent Unity vector.</returns>
            public static implicit operator UnityEngine.Vector4(Vector4 v) => new(v.x, v.y, v.z, v.w);
            /// <summary>Converts a Unity <see cref="UnityEngine.Vector4"/> to this wrapper.</summary>
            /// <param name="v">Unity vector to convert.</param>
            /// <returns>A wrapper containing the vector's components.</returns>
            public static explicit operator Vector4(UnityEngine.Vector4 v) => new(v.x, v.y, v.z, v.w);
        }

        /// <summary>Converts a Unity <see cref="UnityEngine.Vector4"/> to its serializable wrapper.</summary>
        /// <param name="v">Unity vector to convert.</param>
        /// <returns>A wrapper containing the vector's components.</returns>
        public static Vector4 Serializable(this UnityEngine.Vector4 v) => new(v.x, v.y, v.z, v.w);


        /// <summary>Serializes a Unity <see cref="UnityEngine.Vector3"/> as an object with x, y, and z properties.</summary>
        /// <param name="v">Vector to serialize.</param>
        /// <returns>JSON object containing the vector components.</returns>
        public static JObject Serialize(this UnityEngine.Vector3 v) => new()
        {
            ["x"] = v.x,
            ["y"] = v.y,
            ["z"] = v.z,
        };

        /// <summary>Deserializes x, y, and z properties into a Unity vector.</summary>
        /// <param name="v">Vector value to update; set to zero when <paramref name="input"/> is null.</param>
        /// <param name="input">Object containing numeric x, y, and z properties, or null.</param>
        /// <returns>The updated vector.</returns>
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

        /// <summary>Serializes a Unity <see cref="UnityEngine.Vector2"/> as an object with x and y properties.</summary>
        /// <param name="v">Vector to serialize.</param>
        /// <returns>JSON object containing the vector components.</returns>
        public static JObject Serialize(this UnityEngine.Vector2 v) => new()
        {
            ["x"] = v.x,
            ["y"] = v.y,
        };

        /// <summary>Deserializes x and y properties into a Unity vector.</summary>
        /// <param name="v">Vector value to update; set to zero when <paramref name="input"/> is null.</param>
        /// <param name="input">Object containing numeric x and y properties, or null.</param>
        /// <returns>The updated vector.</returns>
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

        /// <summary>Serializes a Unity <see cref="UnityEngine.Vector4"/> as an object with x, y, z, and w properties.</summary>
        /// <param name="v">Vector to serialize.</param>
        /// <returns>JSON object containing the vector components.</returns>
        public static JObject Serialize(this UnityEngine.Vector4 v) => new()
        {
            ["x"] = v.x,
            ["y"] = v.y,
            ["z"] = v.z,
            ["w"] = v.w,
        };

        /// <summary>Deserializes x, y, z, and w properties into a Unity vector.</summary>
        /// <param name="v">Vector value to update; set to zero when <paramref name="input"/> is null.</param>
        /// <param name="input">Object containing numeric x, y, z, and w properties, or null.</param>
        /// <returns>The updated vector.</returns>
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
}