using UnityEngine;
using System.Collections.Generic;
using System;

[DefaultExecutionOrder(1)]
public class UpdateProxy : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void INIT() => Self = StaticComponents.Add<UpdateProxy>();
    public static UpdateProxy Self { get; private set; }

    internal static readonly List<string> updateChannelKeys = new();
    internal static readonly List<Channel> updateChannels = new();

    public static event Action OnUpdate;
    public static event Action OnFixedUpdate;
    public static event Action OnLateUpdate;

    private static List<Timer> attachedTimers = new();

    public class Channel
    {
        public float maxUpdatesPerFrame = 4;
        public List<Action> updateQueue = new();

        public int updatesDone = 0;
        public void Update()
        {
            while (updateQueue.Count > 0 && updatesDone < maxUpdatesPerFrame)
            {
                updateQueue[0]?.Invoke();
                updateQueue.RemoveAt(0);
                updatesDone++;
            }
            updatesDone = 0;
        }
    }

    private void Update()
    {
        OnUpdate?.Invoke();

        for (int i = 0; i < attachedTimers.Count; i++) attachedTimers[i].Tick();
        for (int i = 0; i < updateChannels.Count; i++) updateChannels[i].Update();
    }
    private void FixedUpdate() => OnFixedUpdate?.Invoke();
    private void LateUpdate() => OnLateUpdate?.Invoke();

    public static void RegisterChannel(string channelName, float maxUpdatesPerFrame = 4)
    {
        if (!updateChannelKeys.Contains(channelName))
            updateChannels[updateChannelKeys.IndexOf(channelName)] 
                = new Channel { maxUpdatesPerFrame = maxUpdatesPerFrame };
    }

    public static void QueueUpdate(Action updateAction, string channelName, bool isFixedUpdate = false)
    {
        if (!updateChannelKeys.Contains(channelName)) RegisterChannel(channelName, 4);
        int id = updateChannelKeys.IndexOf(channelName);
        if (updateChannels[id].updatesDone < updateChannels[id].maxUpdatesPerFrame)
        {
            updateAction?.Invoke();
            updateChannels[id].updatesDone++;
        }
        else updateChannels[id].updateQueue.Add(updateAction);
    }

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
}
