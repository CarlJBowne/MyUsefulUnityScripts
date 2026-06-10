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

        public bool FileExists => Directory.Exists(path) && File.Exists(FullPath);
    }
}