using AYellowpaper.SerializedCollections;
using SaveSystem;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SaveSystem
{
    /// <summary>
    /// A Globally acessible registry for all saved values in the game. <br/>
    /// An Asset where defaults are defined and cloned from. DO NOT DELETE.
    /// </summary>
    public class SavedValueRegistry : Singleton.Asset<SavedValueRegistry>
    {
        public override void OnEnable()
        {
            base.OnEnable();
            EstablishDefaultSaveData();
        }

        private void EstablishDefaultSaveData()
        {
            SaveData Def = new();
            SaveData.Default = Def;



        }
    }
}