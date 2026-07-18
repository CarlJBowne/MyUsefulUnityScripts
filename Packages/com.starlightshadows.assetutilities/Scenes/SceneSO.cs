using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

[CreateAssetMenu(fileName = "SceneAsset", menuName = "Scriptable Objects/SceneAsset")]
public class SceneSO : ScriptableObject
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

    [field: NonSerialized] public SceneState CurrentState { get; protected set; } = SceneState.Unloaded;
    [field: NonSerialized] public SceneState DesiredState { get; protected set; } = SceneState.Unloaded;

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

    // Routine variants: return IEnumerator to be used as coroutines.
    public virtual IEnumerator LoadRoutine()
    {
        if (CurrentState is SceneState.Loaded or SceneState.Loading) yield break;
        DesiredState = SceneState.Loaded;
        if (CurrentState is SceneState.Unloaded)
        {
            var op = SceneManager.LoadSceneAsync(Scene, Additive ? LoadSceneMode.Additive : LoadSceneMode.Single);
            op.completed += FinishLoad;
            CurrentState = SceneState.Loading;
            while (!op.isDone) yield return null;
        }
    }

    public virtual IEnumerator UnloadRoutine()
    {
        if (!Additive) yield break;
        if (CurrentState is SceneState.Unloaded or SceneState.Unloading) yield break;
        DesiredState = SceneState.Unloaded;
        if (CurrentState is SceneState.Loaded)
        {
            var op = SceneManager.UnloadSceneAsync(Scene, UnloadSceneOptions.UnloadAllEmbeddedSceneObjects);
            op.completed += FinishUnload;
            CurrentState = SceneState.Unloading;
            while (!op.isDone) yield return null;
        }
    }

    // Immediate variants: block until finished (halts game logic while waiting).
    public void LoadImmediate()
    {
        if (CurrentState is SceneState.Loaded or SceneState.Loading) return;
        DesiredState = SceneState.Loaded;
        if (CurrentState is SceneState.Unloaded)
        {
            // Synchronous load (will block until complete)
            SceneManager.LoadScene(Scene, Additive ? LoadSceneMode.Additive : LoadSceneMode.Single);

            FinishLoad(null);
        }
    }

    public void UnloadImmediate()
    {
        if (!Additive) return;
        if (CurrentState is SceneState.Unloaded or SceneState.Unloading) return;
        DesiredState = SceneState.Unloaded;
        if (CurrentState is SceneState.Loaded)
        {
            var op = SceneManager.UnloadSceneAsync(Scene, UnloadSceneOptions.UnloadAllEmbeddedSceneObjects);
            CurrentState = SceneState.Unloading;
            // Busy-wait until unload completes (halts game logic)
            while (!op.isDone) { }
            FinishUnload(null);
        }
    }

    private void FinishLoad(AsyncOperation op)
    {
        LoadedStruct = SceneManager.GetSceneByName(Scene.sceneName);
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
        LoadedStruct = default;
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

    public static SceneSO CreateRuntime(SceneReference input)
    {
        SceneSO result = ScriptableObject.CreateInstance<SceneSO>();
        result.Scene = input;
        return result;
    }

    public bool Loaded => CurrentState == SceneState.Loaded;
    public UnityEngine.SceneManagement.Scene LoadedStruct { get; private set; }
}
