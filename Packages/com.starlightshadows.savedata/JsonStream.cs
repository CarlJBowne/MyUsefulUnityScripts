using System;
using System.IO;
using Newtonsoft.Json.Linq;

namespace Utilities.JSON
{
    /// <summary>
    /// An Input Output stream for Saving/Loading Save Data to/from disk. Also used to display save files in UI.
    /// </summary>
    public abstract class JsonStream<T> where T : class, new()
    {
        public JsonStream(int fileID)
        {
            this.fileID = fileID;
            saveRootPath = Path.Combine(UnityEngine.Application.persistentDataPath, "Saves");
            InitFiles();
        }
        public virtual void InitFiles()
        {
            RootFile = new(saveRootPath, $"Save{fileID}");
            SecondaryFiles = new JsonFile[0];
        }
        protected JsonFile[] SecondaryFiles;
        protected JsonFile RootFile;

        public readonly string saveRootPath;
        public int fileID = -1;
        public bool filesDoExist
        {
            get
            {
                for (int i = 0; i < SecondaryFiles.Length; i++)
                    if (!SecondaryFiles[i].FileExists)
                        return false;
                return true;
            }
        }

        public JsonFile.LoadResult LoadFromFile(T ResultingData)
        {
            if (fileID == -1) throw new Exception("No file target set. Use SetFileTarget before loading or saving.");
            if (!RootFile.FileExists) return JsonFile.LoadResult.FileNotFound;
            for (int i = 0; i < SecondaryFiles.Length; i++)
                if (!SecondaryFiles[i].FileExists)
                    return JsonFile.LoadResult.FileNotFound;

            JsonFile.LoadResult rootFileLoadResult = RootFile.LoadFromFile();
            if (rootFileLoadResult != JsonFile.LoadResult.Success) return rootFileLoadResult;

            var fileVersionBehavior = FileVersionBehavior();
            if (fileVersionBehavior != JsonFile.LoadResult.Success) return fileVersionBehavior;

            for (int i = 0; i < SecondaryFiles.Length; i++)
            {
                JsonFile.LoadResult iFileResult = SecondaryFiles[i].LoadFromFile();
                if (iFileResult != JsonFile.LoadResult.Success) return iFileResult;
            }

            ResultingData = new();

            return ReadToData(RootFile.Data as JObject, ResultingData);
        }

        public virtual JsonFile.LoadResult FileVersionBehavior()
        {
            //if ((string)RootFile.Data["FileVersion"] != targetFileVersion)
            //{
            //    UnityEngine.Debug.LogWarning($"Save file version mismatch. Expected {targetFileVersion}, found {(string)RootFile.Data/["FileVersion"]}. /Attempting to load anyway.");
            //}
            return JsonFile.LoadResult.Success;
        }

        protected abstract JsonFile.LoadResult ReadToData(JObject RootFileData, T ResultingData);


        public JsonFile.FileState SaveToFile(T sourceData)
        {
            if (fileID == -1) throw new Exception("No file target set. Use SetFileTarget before loading or saving.");

            JsonFile.FileState writeResult = WriteFromData(sourceData);
            if (writeResult != JsonFile.FileState.Valid) return writeResult;

            JsonFile.FileState resultState;

            resultState = RootFile.SaveToFile();
            if (resultState != JsonFile.FileState.Valid) return resultState;

            for (int i = 0; i < SecondaryFiles.Length; i++)
            {
                resultState = SecondaryFiles[i].SaveToFile();
                if (resultState != JsonFile.FileState.Valid) return resultState;
            }
            return resultState;
        }

        protected abstract JsonFile.FileState WriteFromData(T sourceData);

        public JsonFile.FileState DeleteFile()
        {
            if (fileID == -1) throw new Exception("No file target set. Use SetFileTarget before loading or saving.");
            RootFile.DeleteFile();
            for (int i = 0; i < SecondaryFiles.Length; i++) SecondaryFiles[i].DeleteFile();
            return JsonFile.FileState.Null;
        }

        public float GetCompletionPercentage()
        {
            if (fileID == -1) throw new Exception("No file target set. Use SetFileTarget before loading or saving.");
            int totalCollectibles = 1; // Replace with actual total collectible count later
            if (totalCollectibles == 0) return 100f;
            int collected = 0;

            return (collected / (float)totalCollectibles) * 100f;
        }

    }
}