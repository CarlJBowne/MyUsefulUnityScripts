using UnityEngine;
using System.Collections.Generic;

public class TimeBackgroundUtility : MonoBehaviour
{
    private static TimeBackgroundUtility _self;
    public static TimeBackgroundUtility Self
    {
        get
        {
            if (_self == null)
            {
                GameObject GO = new("--Coroutine-Runner-Utility--");
                _self = GO.AddComponent<TimeBackgroundUtility>();
                GO.hideFlags = HideFlags.HideAndDontSave;
                DontDestroyOnLoad(GO);
            }
            return _self;
        }
    }

    private static List<Timer> attachedTimers;

    internal static void AttachTimer(Timer timer)
    {
        if (attachedTimers.Contains(timer)) return;
        if (timer.targetAction == null) return; //Wont do anything if it's not registered.
        attachedTimers.Add(timer);
    }
    internal static void DetachTimer(Timer timer)
    {
        if (!attachedTimers.Contains(timer)) return;
        attachedTimers.Remove(timer);
    }

    private void Update()
    {
        for (int i = 0; i < attachedTimers.Count; i++)
            attachedTimers[i].Tick();
    }
}
