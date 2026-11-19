using System;
using System.Collections.Generic;
using System.IO;
using DG.Tweening.Plugins.Core.PathCore;
using Newtonsoft.Json.Linq;

namespace SLS.SaveFileCore
{
    /// <summary>
    /// An individual iteration of a File I/O Stream. <br/>
    /// This is used to identify the version of a SaveFile, and to load/save data from/to it.
    /// </summary>
    public abstract class FileVersion
    {
        /// <summary>Creates a file-version instance of the specified type.</summary>
        /// <param name="T">Concrete <see cref="FileVersion"/> type to instantiate.</param>
        /// <param name="args">Arguments passed to the type's constructor.</param>
        /// <returns>The created file version, or <see langword="null"/> if the type is not a <see cref="FileVersion"/>.</returns>
        public static FileVersion Create(Type T, params object[] args) => Activator.CreateInstance(T, args) as FileVersion;

        /// <summary>The identifier written to and read from this save-file version.</summary>
        public abstract string version { get; }
        /// <summary>Additional version identifiers accepted by this implementation.</summary>
        protected virtual List<string> altVersions { get; }
        /// <summary>The root directory for this version's save file.</summary>
        public string saveRootPath { get; protected set; }
        /// <summary>The parameters used to configure this version.</summary>
        public object[] parameters { get; protected set; }
        /// <summary>Gets or sets the JSON file managed by this version.</summary>
        public JsonFile RootFile { get; protected set; }

        /// <summary>Initializes a file version with the parameters used to configure it.</summary>
        /// <param name="parameters">Arguments used by the concrete version to establish its file location or state.</param>
        public FileVersion(params object[] parameters) => this.parameters = parameters;

        /// <summary>Gets whether the root file's directory exists.</summary>
        public virtual bool PathExists => RootFile.PathExists;
        /// <summary>Gets whether the root file exists.</summary>
        public virtual bool FileExists => RootFile.FileExists;
        /// <summary>Gets whether the root directory and file both exist.</summary>
        public virtual bool Exists => RootFile.Exists;
        /// <summary>Gets whether the root file contains data.</summary>
        public virtual bool HasData => RootFile.HasData;
        /// <summary>Gets whether the root file is valid for this implementation.</summary>
        public virtual bool Valid => RootFile.Valid;

        /// <summary>Reads the root file's version identifier and compares it with this implementation's identifiers.</summary>
        /// <param name="version">Receives the stored identifier, or an error description if it cannot be read.</param>
        /// <returns>The result of loading and matching the version identifier.</returns>
        public virtual FileOpMessage MatchVersion(out string version)
        {
            FileOpMessage fileLoadResult = RootFile.LoadFromFile(out JObject data);
            if (fileLoadResult != FileOpMessage.Success)
            {
                version = $"FILE LOAD ERROR : {fileLoadResult}";
                return fileLoadResult;
            }

            if (data.TryGetValue("Version", out JToken found) ||
                data.TryGetValue("FileVersion", out found) ||
                data.TryGetValue("V", out found) ||
                data.TryGetValue("version", out found) ||
                data.TryGetValue("fileversion", out found) ||
                data.TryGetValue("v", out found) ||
                data.TryGetValue("fileVersion", out found) ||
                data.TryGetValue("schema", out found) ||
                data.TryGetValue("schemaVersion", out found) ||
                data.TryGetValue("Schema", out found) ||
                data.TryGetValue("SchemaVersion", out found) ||
                data.TryGetValue("iteration", out found) ||
                data.TryGetValue("Iteration", out found) ||
                data.TryGetValue("i", out found) ||
                data.TryGetValue("I", out found))
            {
                version = found.ToString();
                return version == this.version ? FileOpMessage.Success
                    : altVersions.Contains(version) ? FileOpMessage.Success 
                    : FileOpMessage.FileVersionMismatch;
            }
            else
            {
                version = "Identification ERROR : No Version Key Found";
                return FileOpMessage.FileMalformed;

            }
        }

        /// <summary>Loads and applies data from the root file.</summary>
        /// <returns>The result of the load operation.</returns>
        public abstract FileOpMessage LoadFromFile();
        /// <summary>Saves the current data to the root file.</summary>
        /// <returns>The result of the save operation.</returns>
        public abstract FileOpMessage SaveToFile();

        /// <summary>Restores state after a failed load; the default implementation does nothing.</summary>
        public virtual void LoadErrorRevert() { }
        /// <summary>Restores the root file from its backup after a failed save.</summary>
        public virtual void SaveErrorRevert() => RootFile.ApplyBackup();

        /// <summary>Deletes the root file.</summary>
        public virtual void DeleteFile() => RootFile.DeleteFile();

        /// <summary>Exports data used to display this save in a user interface.</summary>
        /// <param name="result">Receives the display data produced by this version.</param>
        public abstract void ExportDisplayData(out object result);

        /// <summary>Indicates that a save file's version does not match the expected version.</summary>
        public class MismatchException : Exception
        {
            /// <summary>Initializes the exception with a message describing the mismatch.</summary>
            /// <param name="message">Explanation of the version mismatch.</param>
            public MismatchException(string message) : base(message) { }
        }
    }
}