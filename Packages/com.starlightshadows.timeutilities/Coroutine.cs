using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

// Coroutine+
// A customized, advanced form of Coroutine inspired by aarthificial that keeps track of things about how it is running and has various other features.

/// <summary>
/// A customized, advanced Coroutine solution with various features for easier to write and more effective Coroutines. Use a constructor to create.
/// </summary>
public class Coroutine : IEnumerator
{

    #region Fields

    /// <summary> Forces the next line of the Coroutine to run. Necessary to accomplish anything if the Coroutine was not given an owner. </summary>
    public bool MoveNext()
    {
        if (Enumerator == null) return false;

        // Ensure begin-hook runs before the inner IEnumerator executes.
        if (!begunRan) begunRun();

        bool moved;
        try
        {
            moved = Enumerator.MoveNext();
            // Forward underlying Current so nested yields behave correctly.
            Current = Enumerator.Current;
        }
        catch
        {
            // If the inner enumerator throws, consider it finished for our bookkeeping.
            finishedRun();
            throw;
        }

        if (!moved) finishedRun();

        return moved;
    }
    /// <summary> The current state of this Coroutine.</summary>
    public object Current { get; private set; }
    public void Reset()
    {
        begunRan = false;
        finishedRan = false;
        running = false;
        complete = false;
        waiting = false;
        (Enumerator as System.Collections.IEnumerator)?.Reset();
    }

    /// <summary> Shows if the Coroutine is currently running automatically. </summary>
    public bool running { get; private set; }
    /// <summary> Shows if the Coroutine has completed its tasks. </summary>
    public bool complete { get; private set; }
    /// <summary> Shows if the Coroutine is waiting to be activated via MoveNext(). </summary>
    public bool waiting { get; private set; }


    /// <summary> An Event that is called at the beginning of the Coroutine's tasks. </summary>
    public event System.Action OnBegin;
    /// <summary> An Event that is called at the end of the Coroutine's tasks. </summary>
    public event System.Action OnFinish;

    /// <summary> The MonoBehavior that owns the Coroutine. Necessary for automatic running. (Get Only) </summary>
    public MonoBehaviour Owner { get; private set; }

    /// <summary>The IEnumerator that dictates the code ran by this Coroutine.</summary>
    public IEnumerator Enumerator { get; private set; }

    public UnityEngine.Coroutine UnityCoroutine { get; private set; }

    /// <summary> Shows if the Coroutine was Stopped using StopAuto(). </summary>
    public bool wasAutoStopped => waiting && Owner != null;
    /// <summary> Returns true if the Coroutine has an owner. </summary>
    public bool hasOwner => Owner != null;

    #endregion Fields




    #region Constrctors

    /// <summary>
    /// A customized, advanced Coroutine solution with various features for easier to write and more effective Coroutines. (Constructor)
    /// </summary>
    /// <param name="enumerator">The IEnumerator that dictates the code ran by this Coroutine.</param>
    /// <param name="owner">The MonoBehavior that owns and runs the coroutine. Necessary for it to be automatic. Input Null to require activation via MoveNext().</param>
    public Coroutine(IEnumerator enumerator, MonoBehaviour owner)
    {
        this.Owner = owner;
        this.Enumerator = enumerator;

        if (owner != null) BeginAuto(owner);
        else waiting = true;
    }
    /// <summary>
    /// A customized, advanced Coroutine solution with various features for easier to write and more effective Coroutines. (Constructor)
    /// </summary>
    /// <param name="enumerator">The IEnumerator that dictates the code ran by this Coroutine.</param>
    /// <param name="automatic">Whether or not this coroutine runs automatically. Setting to true does not do anything unless owner is made non-null.</param>
    /// <param name="owner">The MonoBehavior that owns and runs the coroutine. Necessary for automatic running. Input Null to require activation via MoveNext().</param>
    public Coroutine(IEnumerator enumerator, bool automatic, MonoBehaviour owner = null)
    {
        this.Owner = owner;
        this.Enumerator = enumerator;

        if (automatic && owner != null) BeginAuto(owner);
        else waiting = true;
    }

    #endregion



    /// <summary>
    /// Begins a Coroutine's automatic running. (Does not work without an owner or if already running.)
    /// </summary>
    ///<param name="owner">The MonoBehavior that owns and runs the coroutine. Use to replace the owner or give an owner to a Coroutine previously not given one.</param>
    public void BeginAuto(MonoBehaviour owner = null)
    {
        if (running || complete || (this.Owner == null && owner == null)) return;

        if (owner != null) this.Owner = owner;
        if (this.Owner != null)
        {
            // start this instance directly so Unity will call our MoveNext/Current
            UnityCoroutine = this.Owner.StartCoroutine(this);
        }
        running = true;
        waiting = false;
    }

    /// <summary>
    /// Stops the Coroutine's automatic running.
    /// </summary>
    /// <param name="decouple">Decouples the Coroutine from its parent, making it impossible to begin automatic running without setting a new owner.</param>
    public void StopAuto(bool decouple = false)
    {
        if (Owner != null) Owner.StopCoroutine(this);
        UnityCoroutine = null;
        running = false;
        waiting = true;
        if (decouple) Owner = null;
    }

    private bool begunRan;
    private bool finishedRan;
    private void begunRun()
    {
        if (!begunRan)
        {
            begunRan = true;
            OnBegin?.Invoke();
        }
    }
    private void finishedRun()
    {
        if (!finishedRan)
        {
            finishedRan = true;
            waiting = false;
            running = false;
            complete = true;
            OnFinish?.Invoke();
        }
    }

    public WaitUntil Wait => new(() => complete);

    public static implicit operator bool(Coroutine a) => a != null && a.running;

    public override string ToString() => base.ToString();
    public override bool Equals(object obj) => base.Equals(obj);
    public override int GetHashCode() => base.GetHashCode();

    /// <summary>
    /// Whether the IEnumerator provided is equal to the one this Coroutine is currently using.
    /// </summary>
    /// <param name="compare">The IEnumerator to compare.</param>
    /// <returns>True if equal.</returns>
    public bool Uses(IEnumerator compare) => compare == Enumerator;


    public static Coroutine Begin(ref Coroutine slot, IEnumerator Enum, MonoBehaviour owner, bool replace = true)
    {
        if (!replace && slot && slot.running) return null;
        slot?.StopAuto(true);
        slot = new(Enum, owner);
        return slot;
    }
    public static Coroutine Begin(ref Coroutine slot, IEnumerator Enum, bool replace = true)
    {
        if (!replace && slot && slot.running) return null;
        slot?.StopAuto(true);
        slot = new(Enum, Coroutine.Runner);
        return slot;
    }
    public static void Stop(ref Coroutine slot) => slot?.StopAuto();

    public static UpdateProxy Runner => UpdateProxy.Self;

}

public static class SceneOperationRoutine
{
    public static IEnumerator Load(string sceneName, UnityEngine.SceneManagement.LoadSceneMode mode = LoadSceneMode.Additive)
    {
        if (SceneManager.GetSceneByName(sceneName).IsValid()) yield break;
        var operation = UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(sceneName, mode);
        while (operation != null && !operation.isDone) yield return null;

    }

    public static IEnumerator Unload(string sceneName)
    {
        if (!SceneManager.GetSceneByName(sceneName).IsValid()) yield break;
        var operation = UnityEngine.SceneManagement.SceneManager.UnloadSceneAsync(sceneName);
        if (operation != null && !operation.isDone) yield return null;
    }
}

public static class Xtensions_Coroutine
{
    /// <summary>
    /// Begins a Coroutine using this Enumerator and returns it as a Coroutine+. (Automatically attaches to Omni-Coroutine-Runner)
    /// </summary>
    public static Coroutine Begin(this IEnumerator Enum) => new(Enum, Coroutine.Runner);

    /// <summary>
    /// Begins a Coroutine using this Enumerator and returns it as a Coroutine+.
    /// </summary>
    /// <param name="owner">The MonoBehavior that owns and runs the coroutine. Necessary for it to be automatic. Input Null to require activation via MoveNext().</param>
    public static Coroutine Begin(this IEnumerator Enum, MonoBehaviour owner) => new(Enum, owner);

    /// <summary>
    /// Begins a Coroutine using this Enumerator and returns it as a Coroutine+.
    /// </summary>
    /// <param name="automatic">Whether or not this coroutine runs automatically. Setting to true does not do anything unless owner is made non-null.</param>
    /// <param name="owner">The MonoBehavior that owns and runs the coroutine. Necessary for automatic running. Input Null to require activation via MoveNext().</param>
    public static Coroutine Begin(this IEnumerator Enum, bool automatic, MonoBehaviour owner = null) => new(Enum, automatic, owner);

    /// <summary>
    /// Forces this Coroutine to run all of its logic instantly. Cannot guarentee the stability of this choice.
    /// </summary>
    public static void Instant(this IEnumerator Enum)
    {
        if (Enum == null) return;
        bool moved;
        do
        {
            moved = Enum.MoveNext();
            if (Enum.Current is IEnumerator ienum) 
                ienum.Instant();
        } while (moved != false);
    }
    /// <summary>
    /// Forces this Coroutine to run all of its logic instantly. Cannot guarentee the stability of this choice.
    /// </summary>
    public static void InstantSafe(this IEnumerator Enum)
    {
        int backupCounter = 0;
        if (Enum == null) return;
        bool moved;
        do
        {
            moved = Enum.MoveNext();
            if (++backupCounter > 300)
            {
                Debug.LogWarning("Instant Enumeration hit 300 iterations.");
                return;
            }
            if (Enum.Current is IEnumerator ienum) 
                ienum.InstantSafe();
        } while (moved != false);
    }

}
