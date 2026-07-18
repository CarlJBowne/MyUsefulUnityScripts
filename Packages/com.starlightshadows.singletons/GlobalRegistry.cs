using System.Collections.Generic;
using UnityEngine;
using System;
using System.Reflection;
using System.Linq;

namespace SLS.Singletons
{
    /// <summary>
    /// A global registry that manages <see cref="GlobalAsset{t}"/>s and manually added globally accessible prefabs.
    /// </summary>
    /// <remarks>
    /// An instance of this, and all <see cref="GlobalAsset{t}"/>s are automatically created and registered if they don't already exist. <see cref="GlobalAsset{t}"/>s are registered to this, which is in turn registered to PlayerSettings preloaded assets, ensuring they are always loaded and accessible at runtime and in the editor.
    /// </remarks>
    [DefaultExecutionOrder(-165)]
    public class GlobalRegistry : GlobalAsset<GlobalRegistry>
    {
        public List<_GlobalAssetBase> assets = new();

        public override void OnInit()
        { 
            for (int i = 0; i < assets.Count && assets[i] != null; i++) assets[i].OnEnable();
        }

#if UNITY_EDITOR

        public class PostProcessor : UnityEditor.AssetPostprocessor
        { 
            private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
            { 
                if (!TryGet(out GlobalRegistry registry))
                    registry = GetOrCreate(typeof(GlobalRegistry)) as GlobalRegistry;

                registry.OnEnable();

                Type GlobalAssetType = typeof(GlobalAsset<>);
                Type[] globalAssetTypes = GlobalAssetType.GetAllInheritors();

                foreach (Type type in globalAssetTypes)
                {
                    if (type == typeof(GlobalRegistry)) continue;


                    // If no existing in-memory instance is found, ensure an asset exists on disk
                    if(_GlobalAssetBase.TryGetAlreadyActive(type, out _GlobalAssetBase currentInstance))
                    {
                        if (!registry.assets.Contains(currentInstance)) registry.assets.Add(currentInstance);
                        currentInstance.OnEnable();
                    }
                    else
                    {
                        _GlobalAssetBase created = GetOrCreate(type);
                        if (!registry.assets.Contains(created)) registry.assets.Add(created);
                        created.OnEnable();
                    }
                }

                // Ensure the GlobalAssetRegistry asset is added to PlayerSettings preloaded assets
                try
                {
                    UnityEngine.Object[] preloaded = UnityEditor.PlayerSettings.GetPreloadedAssets() ?? Array.Empty<UnityEngine.Object>();
                    if (!preloaded.Contains(registry))
                    {
                        var list = preloaded.ToList();
                        list.Add(registry);
                        UnityEditor.PlayerSettings.SetPreloadedAssets(list.ToArray());
                    }
                }
                catch (Exception) { }// If PlayerSettings APIs change or fail in some contexts, fail silently to avoid breaking import pipeline

                //last minute run through of registry's assets to get rid of Null values.
                for (int i = registry.assets.Count - 1; i >= 0; i--)
                    if (registry.assets[i] == null) registry.assets.RemoveAt(i);

                UnityEditor.AssetDatabase.SaveAssets();
                UnityEditor.AssetDatabase.Refresh();
            }
        }
#endif
    }

}