using UnityEngine;
using System.Collections.Generic;

public class GameplayPauseManager : SingletonMonoBasic<GameplayPauseManager>
{
    public static bool paused { get => Get()._paused; private set => Get()._paused = value; }
    private bool _paused;
    private List<Pauseable> pauseables = new();

    private void _SetPause(bool value)
    {
        if (paused == value) return;
        paused = value;

        for (int i = 0; i < pauseables.Count; i++) pauseables[i].SetPause(paused);

    }

    public static void SetPause(bool value) => Get()._SetPause(value);
    public static void Pause() => Get()._SetPause(true);
    public static void UnPause() => Get()._SetPause(false);
    public static void TogglePause() => Get()._SetPause(!paused);


    public static void RegisterPausable(Pauseable pauseable)
    {
        Get().pauseables.Add(pauseable);
        pauseable.registered = true;
    }
    public static void UnRegisterPausable(Pauseable pauseable)
    {
        Get().pauseables.Remove(pauseable);
        pauseable.registered = false;
    }

    private void OnDisable() => UnRegisterAll();
    private void OnDestroy() => UnRegisterAll();

    private void UnRegisterAll()
    {
        Debug.Log("Unregistering Pausables");
        for (int i = 0; i < pauseables.Count; i++) UnRegisterPausable(pauseables[i]);
    }

}
