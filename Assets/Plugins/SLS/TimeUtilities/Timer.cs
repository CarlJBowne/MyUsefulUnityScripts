using System;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Simple, reusable timer that can be driven manually via <see cref="Tick"/> or attached to the
/// background runner `TimeUtilityBackground.Self` for automatic ticking.
/// </summary>
public class Timer
{
    // Public configurable fields (kept for compatibility with existing codebase)
    public float length;
    public bool loop;
    public Action targetAction;

    // State
    public float time { get; private set; }
    public enum State
    {
        Inactive,
        Updating,
        BackgroundPaused,
        BackgroundDriven,
    }
    public State state { get; private set; } = State.Inactive;


    /// <summary>
    /// Create a timer instance.
    /// </summary>
    /// <param name="length">Duration in seconds.</param>
    /// <param name="loop">Whether the timer should loop after firing.</param>
    /// <param name="result">Action invoked when the timer completes a cycle.</param>
    /// <param name="autoStart">Whether to start immediately.</param>
    /// <param name="attachToBackground">Whether to attach automatically to the background runner.</param>
    public Timer(float length, bool loop = false, Action result = null, State immediateState = State.Inactive)
    {
        this.length = Mathf.Max(0f, length);
        this.loop = loop;
        this.targetAction = result;
        time = 0f;

        targetAction = result;
        SetState(immediateState, true);
    }


    public Timer SetState(State newState, bool restart = false, Action replaceAction = null)
    {
        if (state is State.Updating or State.BackgroundDriven && !restart) return this;

        if (newState is State.BackgroundDriven or State.BackgroundPaused && targetAction == null && replaceAction == null)
            return this;
        if (replaceAction != null) Target(replaceAction);

        if (IsStateBackground(newState) && !IsBackground) 
            TimeBackgroundUtility.AttachTimer(this);
        else if (!IsStateBackground(newState) && IsBackground)
            TimeBackgroundUtility.DetachTimer(this);

        state = newState;
        time = 0;

        return this;
    }
    public Timer StartUpdate(bool restart = false, Action replaceAction = null) => 
        SetState(State.Updating, restart, replaceAction);
    public Timer StartBackground(bool restart = false, Action replaceAction = null) => 
        SetState(State.BackgroundDriven, restart, replaceAction);
    public Timer PrepBackground(bool restart = false, Action replaceAction = null) => 
        SetState(State.BackgroundPaused, restart, replaceAction);
    public Timer Stop() => SetState(State.Inactive);
    public Timer Pause(bool unPause = false)
    {
        if (!unPause)
        {
            if (state is State.Updating) state = State.Inactive;
            if (state is State.BackgroundDriven) state = State.BackgroundPaused;
        }
        else
        {
            if (state is State.Inactive) state = State.Updating;
            if (state is State.BackgroundPaused) state = State.BackgroundDriven;
        }

        return this;
    }



    public void Target(Action newTarget) => targetAction = newTarget;
    public void Reset() => time = 0f;

    // ---------- Tick logic ----------

    /// <summary>
    /// Advance the timer by the current frame's delta time. Returns true the frame the timer reaches its length.
    /// This is the method used by the background runner as well.
    /// </summary>
    /// <returns>True the frame the timer completes a cycle.</returns>
    public bool Tick()
    {
        if (!IsActive) return false;

        // Zero-length timers fire immediately.
        if (length <= 0f)
        {
            targetAction?.Invoke();
            if (loop)
            {
                // keep active and leave time at 0
                time = 0f;
            }
            else
            {
                Stop();
                time = 0f;
            }
            return true;
        }

        time += Time.deltaTime;
        if (time < length) return false;

        // fire
        targetAction?.Invoke();

        if (loop)
        {
            // Preserve overflow time for more accurate intervals.
            time %= length;
            // remain active
        }
        else Stop();

        return true;
    }

    // ---------- Utility properties ----------

    /// <summary>
    /// Progress from 0..1 (clamped). 0 = just started, 1 = reached or exceeded length.
    /// </summary>
    public float Progress => length <= 0f ? 1f : Mathf.Clamp01(time / length);

    /// <summary>
    /// Remaining time until next firing (>= 0).
    /// </summary>
    public float Remaining => Mathf.Max(0f, length - time);

    public bool IsActive => state is State.Updating or State.BackgroundDriven;
    public bool IsBackground => state is State.BackgroundDriven or State.BackgroundPaused;
    public bool IsStateActive(State state) => state is State.Updating or State.BackgroundDriven;
    public bool IsStateBackground(State state) => state is State.BackgroundDriven or State.BackgroundPaused;

    public override string ToString() => 
        $"Timer(len={length:0.00}, time={time:0.00}, active={IsActive}, loop={loop})";

    public static void Begin(ref Timer timer, float length, bool loop, Action targetAction = null, bool background = false)
    {
        if (timer == null) timer = new(length, loop, targetAction,
            background ? global::Timer.State.BackgroundDriven : global::Timer.State.Updating);
        else timer.SetState(background ? global::Timer.State.BackgroundDriven : global::Timer.State.Updating, true);
    }
}

public static class Xtensions_Timers
{
    public static Timer Timer(this float length, Action result = null, Timer.State immediateState = global::Timer.State.Inactive) => new(length, false, result, immediateState);
    public static Timer Loop(this float length, Action result = null, Timer.State immediateState = global::Timer.State.Inactive) => new(length, true, result, immediateState);
    public static Timer Timer(this Action result, float length, Timer.State immediateState = global::Timer.State.Inactive) => new(length, false, result, immediateState);
    public static Timer Loop(this Action result, float length, Timer.State immediateState = global::Timer.State.Inactive) => new(length, true, result, immediateState);
}