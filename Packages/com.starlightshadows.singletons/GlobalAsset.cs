using System;
using System.Reflection;
using UnityEngine;

namespace SLS.Singletons
{
    /// <summary>
    /// A base class for globally accessible "Singleton" type Scriptable Object Assets.
    /// Inherit from `GlobalAsset&lt;YourType&gt;` to gain automatic registration and availability.
    /// </summary>
    /// <remarks>
    /// <see cref="GlobalAsset{T}"/>s are automatically registered with the <see cref="GlobalRegistry"/>, which is itself automatically registered with Preloaded Assets. 
    /// </remarks>
    /// <typeparam name="T">The concrete type inheriting this base class (the singleton type).</typeparam>
    [DefaultExecutionOrder(-155)]
    public abstract class GlobalAsset<T> : _GlobalAssetBase where T : class
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
        public static bool Active => S.Active;

        /// <summary>
        /// Attempts to get the currently registered singleton instance.
        /// </summary>
        /// <param name="instance">Out parameter that receives the instance if present.</param>
        /// <returns>True if an instance is present; otherwise false.</returns>
        public static bool TryGet(out T instance) => S.TryGet(out instance);


        /// <summary>
        /// Unity OnEnable callback override - registers this ScriptableObject as the singleton instance.
        /// </summary>
        public sealed override void OnEnable()
        {
            Singleton.OperationMessage res = S.Register(this as T);
            if (res != Singleton.OperationMessage.Success) return;
            OnInit();
        }

        /// <summary>
        /// Unity OnDisable callback - unregisters this ScriptableObject if it is registered.
        /// </summary>
        private void OnDisable()
        {
            Singleton.OperationMessage res = S.Deregister(this as T);
            if (res != Singleton.OperationMessage.Success) return;
            OnDeInit();
        }

        public virtual void OnInit() { }
        public virtual void OnDeInit() { }
    }


    /// <summary>
    /// Lightweight ScriptableObject base to allow `GlobalAsset` to inherit.
    /// </summary>
    public abstract class _GlobalAssetBase : ScriptableObject
    {
        /// <summary>
        /// Called by Unity when this ScriptableObject becomes enabled. Can be called to force initialization. <br/>
        /// Override OnInit() instead of this method in derived classes as needed.
        /// </summary>
        public virtual void OnEnable() { }

#if UNITY_EDITOR
        // Non-generic variant to create/load assets by runtime Type
        public static _GlobalAssetBase GetOrCreate(Type t, string path = "Data/")
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
                if (loaded is _GlobalAssetBase asset) return asset;
            }

            // Create new ScriptableObject instance of the requested Type
            ScriptableObject created = CreateInstance(t);
            if (created == null) return null;

            System.IO.Directory.CreateDirectory(System.IO.Path.Combine(Application.dataPath, path));
            UnityEditor.AssetDatabase.CreateAsset(created, $"Assets/{path}{t.Name}.asset");
            UnityEditor.AssetDatabase.SaveAssets();

            return created as _GlobalAssetBase;
        }

        public static bool TryGetAlreadyActive(Type t, out _GlobalAssetBase result)
        {
            FieldInfo singletonField = typeof(GlobalAsset<>).MakeGenericType(t)
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
            result = slot as _GlobalAssetBase;
            return true;
        }
#endif
    }
}