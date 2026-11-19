using System;
using System.Reflection;
using UnityEditor.Presets;
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
    public abstract class GlobalAsset<T> : _GlobalAssetBase where T : GlobalAsset<T>
    {
        /// Backing field for the late object singleton instance.
        /// </summary>
        static readonly Singleton<T> S = new();

        /// <summary>
        /// Gets the registered singleton instance, attempting any configured creation paths if necessary.
        /// </summary>
        public static T Get => S.Get;
        /// <summary>
        /// Attempts to get the currently registered singleton instance.
        /// </summary>
        /// <param name="instance">Out parameter that receives the instance if present.</param>
        /// <returns>True if an instance is present; otherwise false.</returns>
        public static bool TryGet(out T instance) => S.TryGet(out instance);

        protected bool isInitialized { get; private set; }
        protected bool isEditorInitialized { get; private set; }

        /// <summary>
        /// Whether this <see cref="GlobalAsset{T}"/> is present and locatable in the singleton registry.
        /// </summary>
        public static bool Present => S.Active;
        /// <summary>
        /// Whether this <see cref="GlobalAsset{T}"/> is present and has been initialized for runtime use.
        /// </summary>
        public static bool Active => Present && S.Get.isInitialized;
        /// <summary>
        /// Whether this <see cref="GlobalAsset{T}"/> is present and has been initialized for editor use.
        /// </summary>
        public static bool EditorActive => Present && S.Get.isEditorInitialized;



        /// <summary>
        /// Unity OnEnable callback override - registers this ScriptableObject as the singleton instance.
        /// </summary>
        internal sealed override void OnEnable()
        {
            if (Present && Active && EditorActive) return;

            if (!Present)
            {
                Singleton.OperationMessage res = S.Register(this as T);
                if (res != Singleton.OperationMessage.Success) return;
            }

            this.GetExecutionDetails(out bool gameIsEditor, out bool gameIsPlaying);

            if (!EditorActive && gameIsEditor)
            {
#if UNITY_EDITOR
                if (!gameIsPlaying) Debug.Log($"INITIALIZING : {this.name}... (For Editor use only.)");
#endif
                OnInitEditor();
                isEditorInitialized = true;
            }

            OnInit();

            if (!Active && gameIsPlaying)
            {
#if UNITY_EDITOR
                Debug.Log($"INITIALIZING : {this.name}...");
#endif
                OnInitPlaying();
                isInitialized = true;
            }
        }

        /// <summary>
        /// Unity OnDisable callback - unregisters this ScriptableObject if it is registered.
        /// </summary>
        private void OnDisable()
        {
            if (!Present && !Active && !EditorActive) return;

            Singleton.OperationMessage res = S.Deregister(this as T);
            if (res != Singleton.OperationMessage.Success) return;

            OnDeload();
            isInitialized = false;
            isEditorInitialized = false;
        }

        public virtual void OnInit() { }
        public virtual void OnDeload() { }
        public virtual void OnInitEditor() { }
        public virtual void OnInitPlaying() { }
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
        internal virtual void OnEnable() { }

#if UNITY_EDITOR

        public static void Enable_For_Editor(_GlobalAssetBase asset) => asset.OnEnable();

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
            //For some reason an unknown bug has appeared here where the GA_Type line throws an error as if T is an invalid generic argument when it's not.
            try
            {
                Type GA_Type = typeof(GlobalAsset<>).MakeGenericType(t);
                FieldInfo singletonField = GA_Type.GetField("S", BindingFlags.Static | BindingFlags.NonPublic);
                Type S_Type = typeof(Singleton<>).MakeGenericType(t);
                PropertyInfo slotField = S_Type.GetProperty("slot", BindingFlags.Instance | BindingFlags.Public);

                object single = singletonField.GetValue(null) ?? throw new();
                object slot = slotField.GetValue(single);
                if (slot is null || slot.GetType() != t) throw new();
                result = slot as _GlobalAssetBase;
                return true;
            }
            catch
            {
                result = null;
                return false;
            }
        }
#endif
    }
}