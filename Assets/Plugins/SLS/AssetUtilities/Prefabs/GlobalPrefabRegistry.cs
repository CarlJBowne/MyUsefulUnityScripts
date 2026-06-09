using System;
using System.Collections.Generic;
using SLS.ListUtilities;
using SLS.Singletons;
using UnityEngine;

public class GlobalPrefabRegistry : SLS.Singletons.GlobalAsset<GlobalPrefabRegistry>
{
    [SerializeField] private List<GameObject> typedPrefabs;
    [SerializeField] private DictionaryS<string, GameObject> namedPrefabs;


    public override void OnInit()
    {
        for (int i = 0; i < typedPrefabs.Count && typedPrefabs[i] != null; i++) 
            IGlobalPrefab.RegisterPrefab(typedPrefabs[i]);
        for (int i = 0; i < namedPrefabs.Count; i++) 
            IGlobalPrefab.RegisterPrefab(namedPrefabs.ValueFromIndex(i), namedPrefabs.KeyFromIndex(i));
    }
}
