using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Files = System.IO.File;

namespace SLS.SaveFileCore
{
    /// <summary>
    /// A Json File representation. Stores a JToken.
    /// </summary>
    public class JsonFile : TextFile
    {
        public JsonFile(string path, string filename) : base(path, filename) { }
        public override string extension
        {
            get => ".json";
            set { }
        }

        /// <summary>
        /// Loads Json Data from the File specified by this JsonFile's path and filename.
        /// </summary>
        /// <returns>A <see cref="FileState"/> indicating the result of the load operation.</returns>
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

        /// <summary>  
        /// Saves the specified <see cref="NewData"/> content to the file specified by this JsonFile's path and filename.  
        /// </summary>  
        /// <param name="input">Quick override to input new/changed data before save.</param>  
        /// <returns>A <see cref="FileState"/> indicating the result of the operation.</returns>  
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

        private string backupData;

        public virtual void MakeBackup() => LoadFromFile(out backupData);
        public virtual void ApplyBackup() => SaveToFile(backupData);
    }
}