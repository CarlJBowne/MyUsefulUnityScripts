using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Utilities.JSON;

namespace SaveSystem
{
    public partial class SaveData
    {
        public const string targetFileVersion = "1.0.0";


        public static SaveData Clone(SaveData source, SaveData target)
        {
            target ??= new SaveData();


            return target;
        }

        public SaveData()
        {

        }

        public class Json : JsonSaveFile<SaveData>
        {
            public Json(int fileID) : base(fileID) { }

            protected override JsonFile.LoadResult ReadToData(JObject RootFileData, SaveData ResultingData) => throw new NotImplementedException();
            protected override JsonFile.FileState WriteFromData(SaveData sourceData) => throw new NotImplementedException();
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
        public static Json IO;

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
