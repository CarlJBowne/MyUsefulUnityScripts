using System;
using System.IO;
using Newtonsoft.Json.Linq;

namespace Utilities.JSON
{
    /// <summary>
    /// An Input Output stream for Saving/Loading Save Data to/from disk. Also used to display save files in UI.
    /// </summary>
    public abstract class JsonStream
    {
        public virtual void Init()
        {
            saveRootPath = UnityEngine.Application.persistentDataPath;
            RootFile = new(saveRootPath, $"Save");
            SecondaryFiles = new JsonFile[0];
        }
        protected JsonFile[] SecondaryFiles;
        protected JsonFile RootFile;

        public string saveRootPath;
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

        public JsonFile.LoadResult LoadFromFile(ref string resultToken, string defaultToken)
        {
            PreCheck();
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

            JToken token = JToken.Parse(defaultToken);
            JsonFile.LoadResult result = ReadData();
            if (result != JsonFile.LoadResult.Success) return result;
            resultToken = token.ToString();

            return result;
        }

        public virtual JsonFile.LoadResult FileVersionBehavior()
        {
            //if ((string)RootFile.Data["FileVersion"] != targetFileVersion)
            //{
            //    UnityEngine.Debug.LogWarning($"Save file version mismatch. Expected {targetFileVersion}, found {(string)RootFile.Data/["FileVersion"]}. /Attempting to load anyway.");
            //}
            return JsonFile.LoadResult.Success;
        }

        protected abstract JsonFile.LoadResult ReadData();


        public JsonFile.FileState SaveToFile()
        {
            PreCheck();

            JsonFile.FileState writeResult = WriteData();
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

        protected abstract JsonFile.FileState WriteData();

        public JsonFile.FileState DeleteFile()
        {
            PreCheck();
            RootFile.DeleteFile();
            for (int i = 0; i < SecondaryFiles.Length; i++) SecondaryFiles[i].DeleteFile();
            return JsonFile.FileState.Null;
        }

        public float GetCompletionPercentage()
        {
            PreCheck();
            int totalCollectibles = 1; // Replace with actual total collectible count later
            if (totalCollectibles == 0) return 100f;
            int collected = 0;

            return (collected / (float)totalCollectibles) * 100f;
        }

        public virtual JsonFile.FileState PreCheck() => JsonFile.FileState.Valid;

        public static JToken PruneDefaults(JToken current, JToken defaults)
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

        public static JToken ApplyDeltaToBase(JToken baseToken, JToken delta)
        {
            if (delta == null) return baseToken.DeepClone();
            if (baseToken == null) return delta.DeepClone();

            if (delta.Type != JTokenType.Object || baseToken.Type != JTokenType.Object)
                return delta.DeepClone();

            var baseObj = (JObject)baseToken.DeepClone();
            var deltaObj = (JObject)delta;
            foreach (var prop in deltaObj.Properties())
            {
                baseObj[prop.Name] = ApplyDeltaToBase(baseObj[prop.Name], prop.Value);
            }
            return baseObj;
        }


    }
}