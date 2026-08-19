using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Utilities.JSON
{
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
        public JObject Data;

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
                                    ? FileState.FileEmpty
                                    : string.IsNullOrEmpty(path) || string.IsNullOrEmpty(filename)
                                        ? FileState.PathNotSetup
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
        /// <returns>A <see cref="FileState"/> indicating the result of the load operation.</returns>
        public FileState LoadFromFile()
        {
            if (State == FileState.PathNotSetup || !Directory.Exists(path)) return FileState.FileNotFound;
            if (!File.Exists(FullPath)) return FileState.FileNotFound;

            using StreamReader load = File.OpenText(FullPath);
            string fileContent = load.ReadToEnd();

            if (string.IsNullOrWhiteSpace(fileContent)) return FileState.FileEmpty;

            try { Data = JObject.Parse(fileContent); }
            catch (JsonReaderException) { return FileState.Error; }

            return FileState.Valid;
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
        public FileState SaveToFile(JObject NewData)
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
            if (State == FileState.PathNotSetup || !Directory.Exists(path)) return;
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
            FileEmpty,
            /// <summary>
            /// The file was not found at the specified path.
            /// </summary>
            FileNotFound,
            /// <summary>
            /// The file path or filename is invalid.
            /// </summary>
            PathNotSetup,
            /// <summary>
            /// Some other error occurred.
            /// </summary>
            Error
        }

        public bool FileExists => Directory.Exists(path) && File.Exists(FullPath);

        public JToken this[string i] => Data?[i];
    }
}