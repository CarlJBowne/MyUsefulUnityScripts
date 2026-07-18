using System;
using System.Reflection;
using SLS.ListUtilities;
using SLS.Singletons;
using UnityEngine;

namespace SLS.GameStateMachine
{
    [DefaultExecutionOrder(-155)]
    public abstract class GameStateSingle<T> : _GameStateSingleBase where T : GameState
    {
        /// Backing field for the late object singleton instance.
        /// </summary>
        static Singleton<T> S = new();

        /// <summary>
        /// Gets the registered singleton instance, attempting any configured creation paths if necessary.
        /// </summary>
        public static T Get => S.Get;

        /// <summary>
        /// Whether an instance of this Singleton Type is Active.
        /// </summary>
        public static bool Present => S.Active;

        /// <summary>
        /// Attempts to get the currently registered singleton instance.
        /// </summary>
        /// <param name="instance">Out parameter that receives the instance if present.</param>
        /// <returns>True if an instance is present; otherwise false.</returns>
        public static bool TryGet(out T instance) => S.TryGet(out instance);

        public override void OnEnable()
        {
            base.OnEnable();
            Singleton.OperationMessage res = S.Register(this as T);
            if (res != Singleton.OperationMessage.Success) return;
        }
    }
    public abstract class _GameStateSingleBase : GameState
    {
#if UNITY_EDITOR
        // Non-generic variant to create/load assets by runtime Type
        public static _GameStateSingleBase GetOrCreate(Type t, string path = "Data/GameStates/")
        {
            if (t == null) return null;

            string searchFilter = $"t:{t.Name}";
            string[] guids = UnityEditor.AssetDatabase.FindAssets(searchFilter);

            if (guids != null && guids.Length > 0)
            {
                if (guids.Length > 1)
                    for (int i = guids.Length - 1; i > 0; i--)
                    {
                        UnityEngine.Object obj = UnityEditor.AssetDatabase.LoadMainAssetAtPath(UnityEditor.AssetDatabase.GUIDToAssetPath(guids[i]));
                        if (obj != null) Destroy(obj);
                    }

                UnityEngine.Object loaded = UnityEditor.AssetDatabase.LoadMainAssetAtPath(UnityEditor.AssetDatabase.GUIDToAssetPath(guids[0]));
                if (loaded is _GameStateSingleBase asset) return asset;
            }

            // Create new ScriptableObject instance of the requested Type
            ScriptableObject created = CreateInstance(t);
            if (created == null) return null;

            System.IO.Directory.CreateDirectory(System.IO.Path.Combine(Application.dataPath, path));
            UnityEditor.AssetDatabase.CreateAsset(created, $"Assets/{path}{t.Name}.asset");
            UnityEditor.AssetDatabase.SaveAssets();

            return created as _GameStateSingleBase;
        }

        public static bool TryGetAlreadyActive(Type t, out _GameStateSingleBase result)
        {
            FieldInfo singletonField = typeof(GameStateSingle<>).MakeGenericType(t)
                .GetField("S", BindingFlags.Static | BindingFlags.NonPublic);
            PropertyInfo slotField = typeof(Singleton<>).MakeGenericType(t)
                .GetProperty("slot", BindingFlags.Instance | BindingFlags.Public);

            object single = singletonField.GetValue(null);
            if (single is null)
            {
                result = null;
                return false;
            }
            object slot = slotField.GetValue(single);
            if (slot is null || slot.GetType() != t)
            {
                result = null;
                return false;
            }
            result = slot as _GameStateSingleBase;
            return true;
        }

#endif

    }
}