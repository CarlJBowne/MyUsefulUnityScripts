using System;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using JToken = Newtonsoft.Json.Linq.JToken;

namespace SaveSystem
{
    public static class XtensionsJson
    {
        /*
        /// <summary>
        /// Loads a JToken from a file with the specified path and filename.
        /// </summary>
        /// <param name="path">The path of the file.</param>
        /// <param name="filename">The filename.</param>
        /// <returns>The loaded JToken.</returns>
        public static JToken LoadJsonFromFile(string path, string filename)
        {
            if (!Directory.Exists(path)) return null;
            if (!File.Exists(new FilePath(path, filename, "json"))) return null;
            using StreamReader load = File.OpenText($"{path}/{filename}.json");
            return JObject.Parse(load.ReadToEnd());
        }
        /// <summary>
        /// Saves this JToken to a file with the specified path and filename.
        /// </summary>
        /// <param name="path">The path of the file.</param>
        /// <param name="filename">The filename.</param>
        public static void SaveToFile(this JToken THIS, string path, string filename)
        {
            if (!Directory.Exists(path)) Directory.CreateDirectory(path);
            using StreamWriter file = File.CreateText(new FilePath(path, filename, "json"));
            file.WriteLine(THIS.ToString());
        }
        */

        /// <summary>
        /// Serializes the input object into a JToken. 
        /// <br />For when you're unsure if the target object is an ICustomSerialized or not.
        /// <br />*Must be used with one a newly constructed JToken via new().
        /// </summary>
        /// <param name="OBJ">The Source Object to be Serialized.</param>
        /// <returns></returns>
        public static JToken Serialize(this JToken THIS, object OBJ)
        {
            THIS = typeof(ICustomSerialized).IsAssignableFrom(OBJ.GetType())
                ? (OBJ as ICustomSerialized).Serialize()
                : JObject.FromObject(OBJ);
            return THIS;
        }

        /// <summary>
        /// Deserializes this Token into the desired Type.
        /// <br />For when you're unsure if the target object is an ICustomSerialized or not.
        /// </summary>
        /// <typeparam name="T">The Type to Deserialize into.</typeparam>
        /// <returns>The Deserialized Value.</returns>
        public static T Deserialize<T>(this JToken THIS)
        {
            if (typeof(ICustomSerialized).IsAssignableFrom(typeof(T)))
            {
                var Result = Activator.CreateInstance<T>() as ICustomSerialized;
                Result.Deserialize(THIS);
                return (T)Result;
            }
            else return THIS.ToObject<T>();
        }
        /// <summary>
        /// Attempts to Deserialize this Token into the desired Type.
        /// <br />For when you're unsure if the target object is an ICustomSerialized or not.
        /// </summary>
        /// <typeparam name="T">The Type to Deserialize into.</typeparam>
        /// <param name="result"></param>
        /// <returns>Whether the Deserialization was succesful.</returns>
        public static bool TryDeserialize<T>(this JToken THIS, out T result)
        {
            if (typeof(ICustomSerialized).IsAssignableFrom(typeof(T)))
            {
                var IResult = Activator.CreateInstance<T>() as ICustomSerialized;
                IResult.Deserialize(THIS);
                result = (T)IResult;
            }
            else result = THIS.Value<T>();
            return result != null;
        }

        /// <summary>
        /// Populates an existing object using this Token.
        /// <br />For when you're unsure if the target object is an ICustomSerialized or not.
        /// </summary>
        /// <param name="target">The Target object.</param>
        public static void DeserializeInto(this JToken THIS, object target)
        {
            var Custom = target as ICustomSerialized;
            if (Custom != null) Custom.Deserialize(THIS);
            else
                using (JsonReader sr = THIS.CreateReader())
                    JsonSerializer.CreateDefault().Populate(sr, target);
        }

        /// <summary>
        /// Converts and Returns the Value as the desired Type. ToObject is more efficient, really just kept for those who are lazy.
        /// </summary>
        /// <typeparam name="T">The Type to Convert to.</typeparam>
        /// <returns>A Converted Value.</returns>
        public static T As<T>(this JToken THIS) => THIS.ToObject<T>();

    }



    /// <summary>
    /// A Json File representation. Stores a JToken. Includes simple functionality for Saving and Loading from file.
    /// </summary>
    public class JsonFile
    {
        /// <summary>
        /// The directory path of the JSON file.
        /// </summary>
        public readonly string path;

        /// <summary>
        /// The name of the JSON file (without extension).
        /// </summary>
        public readonly string filename;

        /// <summary>
        /// The JToken representation of the JSON file's content.
        /// <br />Set
        /// </summary>
        public JToken Data;

        /// <summary>
        /// Gets the full path of the JSON file, including the filename and extension.
        /// </summary>
        public string FullPath => Path.Combine(path, $"{filename}.json");

        /// <summary>
        /// Implicitly accesses a JsonFile's JToken Data.
        /// </summary>
        /// <param name="THIS">The JsonFile instance.</param>
        public static implicit operator JToken(JsonFile THIS) => THIS.Data;

        /// <summary>
        /// Checks the state of the JSON file based on its content and path validity.
        /// </summary>
        public FileState State => Data == null
                                    ? FileState.Null
                                    : string.IsNullOrEmpty(path) || string.IsNullOrEmpty(filename)
                                        ? FileState.NoPath
                                        : FileState.Valid;

        /// <summary>
        /// Initializes a new instance of the <see cref="JsonFile"/> class with the specified path and filename.
        /// </summary>
        /// <param name="path">The directory path of the JSON file.</param>
        /// <param name="filename">The name of the JSON file (without extension).</param>
        public JsonFile(string path, string filename)
        {
            this.path = path;
            this.filename = filename;
            Data = null;
        }


        /// <summary>
        /// Loads Json Data from the File specified by this JsonFile's path and filename.
        /// </summary>
        /// <returns>A <see cref="LoadResult"/> indicating the result of the load operation.</returns>
        public LoadResult LoadFromFile()
        {
            if (State == FileState.NoPath || !Directory.Exists(path)) return LoadResult.DirectoryNotFound;
            if (!File.Exists(FullPath)) return LoadResult.FileNotFound;

            using StreamReader load = File.OpenText(FullPath);
            string fileContent = load.ReadToEnd();

            if (string.IsNullOrWhiteSpace(fileContent)) return LoadResult.FileEmpty;

            try { Data = JObject.Parse(fileContent); }
            catch (JsonReaderException) { return LoadResult.FileCorrupted; }

            return LoadResult.Success;
        }

        /// <summary>
        /// Saves the current <see cref="Data"/> content to the file specified by this JsonFile's path and filename.
        /// </summary>
        /// <returns>A <see cref="FileState"/> indicating the result of the operation.</returns>
        public FileState SaveToFile()
        {
            FileState state = State;
            if (State != FileState.Valid) return state;
            if (!Directory.Exists(path)) Directory.CreateDirectory(path);
            using StreamWriter file = File.CreateText(FullPath);
            file.WriteLine(Data);
            return state;
        }

        /// <summary>  
        /// Saves the specified <see cref="NewData"/> content to the file specified by this JsonFile's path and filename.  
        /// </summary>  
        /// <param name="NewData">Quick override to input new/changed data before save.</param>  
        /// <returns>A <see cref="FileState"/> indicating the result of the operation.</returns>  
        public FileState SaveToFile(JToken NewData)
        {
            Data = NewData;
            FileState state = State;
            if (State != FileState.Valid) return state;
            if (!Directory.Exists(path)) Directory.CreateDirectory(path);
            using StreamWriter file = File.CreateText(FullPath);
            file.WriteLine(Data);
            return state;
        }


        /// <summary>
        /// Deletes the file specified by this JsonFile's path and filename.
        /// </summary>
        public void DeleteFile()
        {
            if (State == FileState.NoPath || !Directory.Exists(path)) return;
            if (!File.Exists(FullPath)) return;
            File.Delete(FullPath);
            Data = null;
        }


        /// <summary>
        /// Represents the state of the JsonFile.
        /// </summary>
        public enum FileState
        {
            /// <summary>
            /// The file is valid and ready for operations.
            /// </summary>
            Valid,

            /// <summary>
            /// The file content is null.
            /// </summary>
            Null,

            /// <summary>
            /// The file path or filename is invalid.
            /// </summary>
            NoPath,

            /// <summary>
            /// Some other error occurred.
            /// </summary>
            Error
        }

        /// <summary>
        /// Represents the result of a load operation from a file.
        /// </summary>
        public enum LoadResult
        {
            /// <summary>
            /// The file was successfully loaded.
            /// </summary>
            Success,

            /// <summary>
            /// The file was not found at the specified path.
            /// </summary>
            FileNotFound,

            /// <summary>
            /// The directory containing the file was not found.
            /// </summary>
            DirectoryNotFound,

            /// <summary>
            /// The file is empty.
            /// </summary>
            FileEmpty,

            /// <summary>
            /// The file content is corrupted and could not be parsed.
            /// </summary>
            FileCorrupted
        }
    }

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


    #region SerializableStructs

    public static class SerializableStructs
    {
        /*
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
        */

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


    public interface ICustomSerialized
    {

        /// <summary>
        /// Serializes the object into a JToken.
        /// <br />HEAVILY encouraged to create an implicit JToken operator redirecting to this for easier/faster conversion.
        /// </summary>
        /// <param name="name">Optional name for if serialized as a full Json Property</param>
        /// <returns>The Json representation.</returns>
        public JToken Serialize(string name = null);
        /// <summary>
        /// Deserializes a JToken and populates this object with its data.
        /// </summary>
        /// <param name="Data">The Json representation to be Deserialized.</param>
        public void Deserialize(JToken Data);

    }
}