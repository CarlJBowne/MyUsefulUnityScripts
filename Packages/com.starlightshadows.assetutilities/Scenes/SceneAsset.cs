using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SLS.AssetUtilties
{
    [CreateAssetMenu(fileName = "SceneAsset", menuName = "Scriptable Objects/SceneAsset")]
    public class SceneAsset : ScriptableObject
    {
        [field: SerializeField] public SceneReference Scene { get; protected set; }
        [field: SerializeField] public bool Additive { get; protected set; } = true;

        public enum SceneState
        {
            Unloaded,
            Unloading,
            Loading,
            Loaded
        }

        public SceneState CurrentState { get; protected set; }
        public SceneState DesiredState { get; protected set; }

        public void Load()
        {
            if (CurrentState is SceneState.Loaded or SceneState.Loading) return;
            DesiredState = SceneState.Loaded;
            if (CurrentState is SceneState.Unloaded)
            {
                var op = SceneManager.LoadSceneAsync(Scene, Additive ? LoadSceneMode.Additive : LoadSceneMode.Single);
                op.completed += FinishLoad;
                CurrentState = SceneState.Loading;
            }
        }
        public void Unload()
        {
            if (!Additive) return;
            if (CurrentState is SceneState.Unloaded or SceneState.Unloading) return;
            DesiredState = SceneState.Unloaded;
            if (CurrentState is SceneState.Loaded)
            {
                var op = SceneManager.UnloadSceneAsync(Scene, UnloadSceneOptions.UnloadAllEmbeddedSceneObjects);
                op.completed += FinishUnload;
                CurrentState = SceneState.Unloading;
            }
        }

        private void FinishLoad(AsyncOperation op)
        {
            OnFinishLoad();
            OnLoad?.Invoke();
            if (DesiredState is SceneState.Loaded) 
            {
                DesiredState = SceneState.Loaded;
                CurrentState = SceneState.Loaded;
            }
            else Unload();
        }
        private void FinishUnload(AsyncOperation op)
        {
            OnFinishUnload();
            OnUnLoad?.Invoke();
            if (DesiredState is SceneState.Unloaded)
            {
                DesiredState = SceneState.Unloaded;
                CurrentState = SceneState.Unloaded;
            }
            else Load();
        }

        protected virtual void OnFinishLoad() { }
        protected virtual void OnFinishUnload() { }

        public Action OnLoad;
        public Action OnUnLoad;

        public static SceneAsset CreateRuntime(SceneReference input)
        {
            SceneAsset result = ScriptableObject.CreateInstance<SceneAsset>();
            result.Scene = input;
            return result;
        }
    }
}
