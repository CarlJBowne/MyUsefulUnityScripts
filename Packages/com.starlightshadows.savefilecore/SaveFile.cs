using System;
using System.Collections.Generic;
using System.Text;

namespace SLS.SaveFileCore
{
    /// <summary>
    /// This represents an individual SaveFile ID of indeterminant Stream Version. It will attempt to load the most recent version of the SaveFile, and if it fails, it will attempt to load the next most recent version, and so on until it either finds a valid SaveFile or determines that no SaveFile exists for this ID.
    /// </summary>
    public class SaveFile
    {
        public static FileVersion.Options _versionOptions;
        public virtual FileVersion.Options VersionOptions => _versionOptions;
        public FileVersion FileVersion;
        private readonly int targetFileID;

        public SaveFile(int fileID)
        {
            targetFileID = fileID;
            VersionOptions.Initialize(ref FileVersion, fileID);
        }

        public void LoadFromFile()
        {
            if (!FileVersion.Valid) return;
            PreLoad();
            FileVersion.LoadFromFile();
            PostLoad();
        }
        public void SaveToFile()
        {
            PreSave();
            if (FileVersion.GetType() != VersionOptions.DesiredFileVersion)
            {
                FileVersion.DeleteFile();
                FileVersion = FileVersion.Create(VersionOptions.DesiredFileVersion, targetFileID);
            }
            FileVersion.SaveToFile();
            PostSave();
        }

        public void ExportDisplayData<T>(out T result)
        {
            if (FileVersion == null) throw new Exception($"SaveFile {targetFileID} has no FileVersion assigned. Cannot export Menu Display Data.");
            FileVersion.ExportDisplayData(out object resultObj);
            if (resultObj is not T) throw new Exception($"SaveFile {targetFileID} has FileVersion {FileVersion.GetType().Name} which returned an object of type {resultObj.GetType().Name}, which cannot be cast to the requested type {typeof(T).Name}");
            result = (T)resultObj;
        }

        public static Action PreLoad;
        public static Action PreSave;
        public static Action PostLoad;
        public static Action PostSave;
    }
}
