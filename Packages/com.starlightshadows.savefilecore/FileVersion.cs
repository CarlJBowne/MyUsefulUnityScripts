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
        public static FileVersion Create(Type T, int fileID) => Activator.CreateInstance(T, fileID) as FileVersion;

        public abstract string version { get; }
        public string saveRootPath { get; protected set; }
        public int fileID { get; protected set; }
        public JsonFile RootFile { get; protected set; }

        public FileVersion(int fileID)
        {
            this.fileID = fileID;
            Initialize();
        }
        public abstract void Initialize();

        public virtual bool PathExists => RootFile.PathExists;
        public virtual bool FileExists => RootFile.FileExists;
        public virtual bool Exists => RootFile.Exists;
        public virtual bool HasData => RootFile.HasData;
        public virtual bool Valid => RootFile.Valid;
        public virtual FileOpMessage MatchVersion(out string version)
        {
            FileOpMessage fileLoadResult = RootFile.LoadFromFile(out JObject data);
            if (fileLoadResult != FileOpMessage.Success)
            {
                version = $"FILE LOAD ERROR : {fileLoadResult}";
                return fileLoadResult;
            }
            if (!data.ContainsKey("Version"))
            {
                version = "Identification ERROR : No Version Key Found";
                return FileOpMessage.FileMalformed;
            }
            version = data["Version"].ToString();
            return version == this.version ? FileOpMessage.Success : FileOpMessage.FileVersionMismatch;
        }

        public abstract FileOpMessage LoadFromFile();
        public abstract FileOpMessage SaveToFile();

        public virtual void LoadErrorRevert() { }
        public virtual void SaveErrorRevert() => RootFile.ApplyBackup();

        public virtual void DeleteFile() => RootFile.DeleteFile();

        public abstract void ExportDisplayData(out object result);


        public class Options
        {
            public Type DesiredFileVersion;
            public Dictionary<string, Type> AllFileVersions;

            public void Initialize(ref FileVersion FileVersion, int fileID)
            {
                FileVersion = Create(DesiredFileVersion, fileID);
                if (FileVersion.MatchVersion(out string version).IfFail(out FileOpMessage failReason))
                {
                    if (failReason == FileOpMessage.FileVersionMismatch)
                    {
                        FileVersion = Create(AllFileVersions[version], fileID);
                        if (FileVersion.MatchVersion(out _).IfFail(out failReason))
                        {
                            FileVersion = Create(DesiredFileVersion, fileID);
                        }
                    }
                    else
                    {
                        FileVersion = null;
                        foreach (var pair in AllFileVersions)
                        {
                            if (pair.Value == DesiredFileVersion) continue;
                            FileVersion = Create(pair.Value, fileID);
                            if (!FileVersion.MatchVersion(out string loaderVersion).IfFail(out failReason)) return;
                            else if (failReason == FileOpMessage.FileVersionMismatch)
                            {
                                FileVersion = Create(AllFileVersions[version], fileID);
                                if (!FileVersion.MatchVersion(out _).IfFail(out failReason)) return;
                                else FileVersion = null;
                            }
                        }
                        FileVersion ??= Create(DesiredFileVersion, fileID);
                    }
                }

            }
        }
        public class MismatchException : Exception
        {
            public MismatchException(string message) : base(message) { }
        }
    }
}