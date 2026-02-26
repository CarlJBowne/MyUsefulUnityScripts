using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace SaveSystem
{
    public partial class SaveData
    {
        public const string targetFileVersion = "1.0.0";
        private int FUNValue;


        public static SaveData Clone(SaveData source, SaveData target)
        {
            target ??= new SaveData();

            return target;
        }

        public SaveData()
        {

        }

        public partial class IOStream
        {
            private bool ReadToData(JToken jObject, SaveData targetData)
            {
                try
                {
                }
                catch (Exception) { return false; }

                return true;
            }

            private bool WriteFromData(SaveData sourceData, out JToken result)
            {
                result = null;

                try
                {
                    result = new JObject
                    {
                        ["FileVersion"] = targetFileVersion
                    };
                }
                catch (Exception) { return false; }

                return true;
            }

        }
    }






    /// <summary>
    /// A class tracking saved values across the game.
    /// </summary>
    public partial class SaveData
    {
        /// <summary> The currently active Save Data during Gameplay. </summary>
        public static SaveData Current;
        /// <summary>
        /// The Save Data used to reload data after the player experiences a death.    
        /// </summary>
        /// <remarks> See <see cref="RevertToDeathData"/></remarks>
        public static SaveData DeathReloadData;
        /// <summary>  Default Save Data template created from the <see cref="SavedValueRegistry"/>. </summary>
        public static SaveData Default;


        /// <summary>
        /// The active IO Stream for saving data during gameplay.
        /// </summary>
        public static IOStream IO;

        /// <summary>
        /// An Input Output stream for Saving/Loading Save Data to/from disk. Also used to display save files in UI.
        /// </summary>
        public partial class IOStream
        {
            public IOStream(int fileID)
            {
                this.fileID = fileID;
                file = new JsonFile(Path.Combine(UnityEngine.Application.persistentDataPath, "Saves"), $"File{fileID}");
            }


            public JsonFile file;

            public int fileID = -1;
            public bool doesFileExist => Directory.Exists(file.path) && File.Exists(file.FullPath);


            public void ClearFileTarget()
            {
                fileID = -1;
                file = null;
            }

            public JsonFile.LoadResult LoadFromFile(SaveData targetData)
            {
                if (fileID == -1) throw new Exception("No file target set. Use SetFileTarget before loading or saving.");
                if (!doesFileExist) return JsonFile.LoadResult.FileNotFound;

                JsonFile.LoadResult result;
                result = file.LoadFromFile();
                if (result != JsonFile.LoadResult.Success) return result;

                if ((string)file.Data["FileVersion"] != targetFileVersion)
                {
                    UnityEngine.Debug.LogWarning($"Save file version mismatch. Expected {targetFileVersion}, found {(string)file.Data["FileVersion"]}. Attempting to load anyway.");
                }

                SaveData process = new();

                if (ReadToData(file.Data, process)) Clone(process, targetData);
                else return JsonFile.LoadResult.FileCorrupted;

                return JsonFile.LoadResult.Success;
            }

            public JsonFile.FileState SaveToFile(SaveData sourceData)
            {
                if (fileID == -1) throw new Exception("No file target set. Use SetFileTarget before loading or saving.");

                if (WriteFromData(sourceData, out JToken result)) file.Data = result;
                else return JsonFile.FileState.Error;

                JsonFile.FileState state = file.SaveToFile();
                if (state != JsonFile.FileState.Valid) return state;
                return JsonFile.FileState.Valid;
            }

            public JsonFile.FileState DeleteFile()
            {
                if (fileID == -1) throw new Exception("No file target set. Use SetFileTarget before loading or saving.");
                file.DeleteFile();
                File.Delete(file.FullPath);
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


        /// <summary>
        /// Reverts the current save data to its state at the time of the last Death Checkpoint. <br/>
        /// See <see cref="DeathReloadData"/>
        /// </summary>
        public static void RevertToDeathData()
        {
            Clone(DeathReloadData, Current);
        }
        /// <summary>
        /// Reverts the current save data to the data last saved to disk.
        /// </summary>
        /// <remarks>See <see cref="IO"/>.</remarks>
        public static void RevertToSaveFile()
        {
            IO.LoadFromFile(Current);
            Clone(Current, DeathReloadData);
        }
        /// <summary>
        /// Saves the current Data to disk.
        /// </summary>
        /// <param name="destination">The current location of the player, as will be applied to all active SaveData objects.</param>
        public static void SaveFileToDisk()
        {
            Clone(Current, DeathReloadData);
            IO.SaveToFile(Current);
        }



    }
}
