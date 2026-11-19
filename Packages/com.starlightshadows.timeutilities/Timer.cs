using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using System.Collections;


#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Simple, reusable timer. Default implementation that uses the central <see cref="UpdateProxy"/> as a central timer manager.
/// <br/> Has subclasses <see cref="Timer.Manual"/> and <see cref="Timer.MonoDriven"/> for alternate implementations.
/// </summary>
[System.Serializable]
public class Timer
{
    /// <summary>
    /// Duration of the timer in seconds. This is the only field intended to be visible in the Editor.
    /// Must be non-negative.
    /// </summary>
    public float length; //The only field that should actually be visible in Editor;

    /// <summary>
    /// Whether the timer should automatically restart after firing.
    /// </summary>
    public bool loop = true;

    /// <summary>
    /// Whether the timer uses unscaled time (ignores timeScale).
    /// </summary>
    public bool unscaled = true;

    // NonSerialized
    /// <summary>
    /// The action invoked when the timer completes a cycle. Not serialized.
    /// </summary>
    [NonSerialized] public Action action;

    /// <summary>
    /// The current elapsed time of the timer in seconds.
    /// </summary>
    public float time { get; private set; } = 0f;

    /// <summary>
    /// Whether the timer is currently active (ticking).
    /// </summary>
    public virtual bool active { get; private set; } = false;

    /// <summary>
    /// Whether the timer is currently paused.
    /// </summary>
    public bool paused { get; private set; } = false;


    /// <summary>
    /// Create a timer instance.
    /// </summary>
    /// <param name="length">Duration in seconds. Values less than zero are clamped to zero.</param>
    /// <param name="loop">Whether the timer should loop after firing.</param>
    public Timer(float length, bool loop = true)
    {
        this.length = Mathf.Max(0f, length);
        this.loop = loop;
    }

    /// <summary>
    /// Start the timer and optionally set the action to invoke when the timer completes.
    /// </summary>
    /// <param name="targetAction">Action to invoke on completion. If null, existing action is preserved.</param>
    public virtual void Start(Action targetAction)
    {
        time = 0;
        active = true;
        paused = false;
        if (targetAction != null) action = targetAction;
        UpdateProxy.AttachTimer(this);
    }

    /// <summary>
    /// Stop the timer. The timer will no longer tick until started again.
    /// </summary>
    public virtual void Stop()
    {
        active = false;
        UpdateProxy.DetachTimer(this);
    }

    /// <summary>
    /// Pause or unpause the timer.
    /// </summary>
    /// <param name="unPause">If true, unpause; otherwise toggle paused state.</param>
    public virtual void Pause(bool unPause = false)
    {
        paused = !unPause;
    }

    /// <summary>
    /// Reset the elapsed time to zero. Does not change active/paused state.
    /// </summary>
    public virtual void Reset() => time = 0f;


    /// <summary>
    /// Advance the timer by the current frame's delta time. Returns true the frame the timer reaches its length.
    /// This is the method used by the background runner as well.
    /// </summary>
    /// <returns>True on the frame the timer completes a cycle and invokes its action; otherwise false.</returns>
    public bool Tick()
    {
        if (!active || paused) return false;

        if (length > 0) time += unscaled ? Time.unscaledDeltaTime : Time.deltaTime;
        if (time < length) return false;

        // fire
        action?.Invoke();

        if (loop) time %= length;
        else
        {
            time = 0;
            Stop();
        }

        return true;
    }


    /// <summary>
    /// Progress of the timer as a value from 0 to 1 (clamped). 0 = just started, 1 = reached or exceeded length.
    /// </summary>
    public float Progress => length <= 0f ? 1f : Mathf.Clamp01(time / length);

    /// <summary>
    /// Remaining time in seconds until the next firing (>= 0).
    /// </summary>
    public float Remaining => Mathf.Max(0f, length - time);

    public bool Running => active && !paused;


    /// <summary>
    /// Returns a short string representation of the timer.
    /// </summary>
    public override string ToString() =>
        $"Timer(len={length:0.00}, t={time:0.00}, active={active}, loop={loop})";


    /// <summary>
    /// A Manual variation of the <see cref="Timer"/> that must be called manually in a MonoBehaviour's Update() method. This is useful for timers that need to be tied to a specific object or called exactly within an update cycle.
    /// </summary>
    /// <remarks>
    /// An action callback can technically be attached, but its easier to just put the Tick function into an if statement.
    /// </remarks>
    public class Manual : Timer
    {
        /// <summary>
        /// This value is irrelevent to this Timer variation as it has to be driven manually. Thus, always true.
        /// </summary>
        public override bool active => true;

        /// <summary>
        /// This constructor creates a Manual timer instance.
        /// </summary>
        public Manual(float length, bool loop = true) : base(length, loop) { }

        /// <summary>
        /// Irrelevant. Manual timers are always considered active.
        /// </summary>
        public override void Start(Action targetAction) => action = targetAction;
        /// <summary>
        /// Irrelevant. Manual timers are always considered active.
        /// </summary>
        public override void Stop() { }
    }

    /// <summary>
    /// A Timer subtype designed to be driven by a MonoBehaviour via a coroutine.
    /// </summary>
    /// <remarks> 
    /// Note, this technically only disables when the entire GameObject the MonoBehavior is attached to is disabled, not when the MonoBehavior itself is disabled. Making its usecase more limited than makes intuitive sense. >:T
    /// </remarks>
    public class MonoDriven : Timer
    {
        /// <summary>
        /// Create a MonoDriven timer.
        /// </summary>
        /// <param name="length">Duration in seconds.</param>
        /// <param name="loop">Whether to loop after firing.</param>
        public MonoDriven(float length, bool loop = true) : base(length, loop) { }

        /// <summary>
        /// Start the timer and launch a coroutine on the provided MonoBehaviour to drive it.
        /// </summary>
        /// <param name="targetAction">Action to invoke on completion.</param>
        /// <param name="self">MonoBehaviour used to start the coroutine.</param>
        public void Start(Action targetAction, MonoBehaviour self)
        {
            base.Start(targetAction);
            Coroutine.Begin(ref coroutine, TickCoroutine(), self, false);
        }

        /// <summary>
        /// Stop the timer and stop the coroutine driving it (if any).
        /// </summary>
        public override void Stop()
        {
            base.Stop();
            coroutine?.StopAuto();
        }

        Coroutine coroutine;

        /// <summary>
        /// Coroutine that advances the timer each frame until it completes or is stopped.
        /// </summary>
        /// <returns>IEnumerator for the Unity coroutine system.</returns>
        IEnumerator TickCoroutine()
        {
            time = 0;
            do
            {
                while (time < length)
                {
                    if (paused || !active)
                    {
                        yield return null;
                        continue;
                    }
                    Tick();
                    yield return null;
                }
            } while (loop);
        }

        /// <summary>
        /// THIS VERSION WILL NOT ALLOW THIS SUBTYPE TO FUNCTION, USE <see cref="Start(Action, MonoBehaviour)"/>.
        /// </summary>
        /// <param name="targetAction">Not used.</param>
        /// <exception cref="InvalidOperationException">Always thrown to prevent misuse of this overload.</exception>
        public override void Start(Action targetAction) => throw new InvalidOperationException();
    }




    [System.Serializable]
    public struct Loop
    {
        [SerializeField] public float rate;
        [SerializeField] public float current;
        [HideInInspector] public bool disabled;

        public Loop(float rate, bool disable = false)
        {
            this.rate = rate;
            current = 0f;
            disabled = disable;
        }

        public void Tick(Action callback)
        {
            if (disabled || rate < 0) return;
            if (rate == 0) callback?.Invoke();
            current += Time.deltaTime;
            if (current > rate)
            {
                current %= rate;
                callback?.Invoke();
            }
        }
    }

    [System.Serializable]
    public struct OneTime
    {
        [SerializeField] public float length;
        [SerializeField] public float current;
        [HideInInspector] public bool running;

        public OneTime(float length, bool activate = false)
        {
            this.length = length;
            current = 0f;
            running = false;
            if (activate) Begin();
        }

        public void Begin()
        {
            current = 0f;
            running = true;
        }

        public void Tick(Action callback)
        {
            if (!running) return;
            current += Time.deltaTime;
            if (current > length)
            {

                running = false;
                callback?.Invoke();
            }
        }
    }


}
