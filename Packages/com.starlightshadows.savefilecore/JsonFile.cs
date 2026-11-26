using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Files = System.IO.File;

namespace SLS.SaveFileCore
{
    /// <summary>
    /// Represents a JSON text file with load and save operations.
    /// </summary>
    public class JsonFile : TextFile
    {
        /// <summary>Initializes a JSON file reference with the <c>.json</c> extension.</summary>
        /// <param name="path">Directory containing the file.</param>
        /// <param name="filename">File's base name, without its extension.</param>
        public JsonFile(string path, string filename) : base(path, filename) { }

        /// <summary>Gets the fixed <c>.json</c> extension.</summary>
        public override string extension
        {
            get => ".json";
            set { }
        }

        /// <summary>Loads and parses the JSON object stored in this file.</summary>
        /// <param name="result">Receives the parsed object, or <see langword="null"/> when the file cannot be loaded.</param>
        /// <returns>The result of the load operation.</returns>
        public virtual FileOpMessage LoadFromFile(out JObject result)
        {
            result = null;

            if (!PathExists) return FileOpMessage.DirectoryNotFound;
            if (!FileExists) return FileOpMessage.FileNotFound;
            if (!HasData) return FileOpMessage.FileEmpty;

            string load = Files.OpenText(FullPath).ReadToEnd();

            result = JObject.Parse(load);
            
            return FileOpMessage.Success;
        }

        /// <summary>Saves a JSON object to this file, optionally creating its directory.</summary>
        /// <param name="input">JSON object to write; an empty object is not saved.</param>
        /// <param name="createIfNonexistant">Whether to create the directory when it does not exist.</param>
        /// <returns>The result of the save operation.</returns>
        public virtual FileOpMessage SaveToFile(JObject input, bool createIfNonexistant = true)
        {
            if (input.Count == 0) return FileOpMessage.FileEmpty;
            try
            {
                if (createIfNonexistant)
                {
                    if (!PathExists) Directory.CreateDirectory(path);
                }
                else
                {
                    if (!FileExists) return FileOpMessage.FileNotFound;
                    if (!PathExists) return FileOpMessage.DirectoryNotFound;
                }
                using StreamWriter file = Files.CreateText(FullPath);
                file.WriteLine(input.ToString());
                return FileOpMessage.Success;
            }
            catch (DirectoryNotFoundException) { return FileOpMessage.DirectoryNotFound; }
            catch (FileNotFoundException) { return FileOpMessage.FileNotFound; }
            catch (Exception) { return FileOpMessage.UnknownError; }
        }

        /// <summary>Stores the most recently loaded JSON text for possible restoration.</summary>
        private string backupData;

        /// <summary>Loads the current file content into the in-memory backup.</summary>
        public virtual void MakeBackup() => LoadFromFile(out backupData);
        /// <summary>Writes the in-memory backup back to the file.</summary>
        public virtual void ApplyBackup() => SaveToFile(backupData);
    }
}