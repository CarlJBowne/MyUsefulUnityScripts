using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

// Coroutine+
// A customized, advanced form of Coroutine that keeps track of things about how it is running and has various other features.

/// <summary>
/// A customized, advanced Coroutine solution with various features for easier to write and more effective Coroutines. Use a constructor to create.
/// </summary>
public class Coroutine : IEnumerator
{

    #region Fields

    /// <summary> Forces the next line of the Coroutine to run. Necessary to accomplish anything if the Coroutine was not given an owner. </summary>
    public bool MoveNext()
    {
        if (enumerator.MoveNext()) begunRun();
        else finishedRun();
        return !enumerator.MoveNext();
    }
    /// <summary> I have no idea what this does. </summary>
    public object Current { get; private set; }
    /// <summary> This does not work. Just create a new one if you need to Reset. </summary>
    public void Reset() { }

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
    public MonoBehaviour owner { get; private set; }

    /// <summary>The IEnumerator that dictates the code ran by this Coroutine.</summary>
    public IEnumerator enumerator { get; private set; }
    private IEnumerator wrappedEnumerator;

    /// <summary> Shows if the Coroutine was Stopped using StopAuto(). </summary>
    public bool wasAutoStopped => waiting && owner != null;
    /// <summary> Returns true if the Coroutine has an owner. </summary>
    public bool hasOwner => owner != null;

    #endregion Fields




    #region Constrctors

    /// <summary>
    /// A customized, advanced Coroutine solution with various features for easier to write and more effective Coroutines. (Constructor)
    /// </summary>
    /// <param name="enumerator">The IEnumerator that dictates the code ran by this Coroutine.</param>
    /// <param name="owner">The MonoBehavior that owns and runs the coroutine. Necessary for it to be automatic. Input Null to require activation via MoveNext().</param>
    public Coroutine(IEnumerator enumerator, MonoBehaviour owner)
    {
        this.owner = owner;
        this.enumerator = enumerator;
        wrappedEnumerator = Wrap(enumerator);
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
        this.owner = owner;
        this.enumerator = enumerator;
        wrappedEnumerator = Wrap(this.enumerator);
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
        if (running || complete || (this.owner == null && owner == null)) return;

        if (owner != null) this.owner = owner;
        if (this.owner != null)
        {
            Current = this.owner.StartCoroutine(wrappedEnumerator = Wrap(this.enumerator));
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
        if (owner != null) owner.StopCoroutine(enumerator);
        running = false;
        waiting = true;
        if (decouple) owner = null;
    }



    private IEnumerator Wrap(IEnumerator enumerator)
    {
        begunRun();
        yield return enumerator;
        finishedRun();
    }


    private bool begunRan;
    private bool finishedRan;
    private void begunRun()
    {
        if (!begunRan)
        {
            OnBegin?.Invoke();
        }
    }
    private void finishedRun()
    {
        if (!finishedRan)
        {
            waiting = false;
            running = false;
            complete = true;
            OnFinish?.Invoke();
        }
    }


    public static implicit operator bool(Coroutine a) => a != null && a.running;

    public override string ToString() => base.ToString();
    public override bool Equals(object obj) => base.Equals(obj);
    public override int GetHashCode() => base.GetHashCode();

    /// <summary>
    /// Whether the IEnumerator provided is equal to the one this Coroutine is currently using.
    /// </summary>
    /// <param name="compare">The IEnumerator to compare.</param>
    /// <returns>True if equal.</returns>
    public bool Uses(IEnumerator compare) => compare == wrappedEnumerator;


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
        slot = new(Enum, omniCoroutineRunner);
        return slot;
    }
    public static void Stop(ref Coroutine slot) => slot?.StopAuto();

    internal static GameObject omniCoroutineRunner {  get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void OnLoad()
    {
        omniCoroutineRunner = new GameObject("Omni-Coroutine-Runner");
        GameObject.DontDestroyOnLoad(omniCoroutineRunner);
        omniCoroutineRunner.hideFlags = HideFlags.HideAndDontSave;
    }
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

public static class XtensionsCoroutine
{
    /// <summary>
    /// Begins a Coroutine using this Enumerator and returns it as a Coroutine+. (Automatically attaches to Omni-Coroutine-Runner)
    /// </summary>
    public static Coroutine Begin(this IEnumerator Enum) => new(Enum, Coroutine.omniCoroutineRunner);

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

}

//Bonus!
//"WaitFor" Premade Coroutines.
//By StarLightShadows.

public static class WaitFor
{

    #region PreExisting


    /// <summary>
    /// Suspends the coroutine execution for the given amount of seconds using scaled time.
    /// </summary>
    /// <param name="time">Delay execution by the amount of time in seconds.</param>
    public static IEnumerator Seconds(float time) { yield return new WaitForSeconds(time); }
    /// <summary>
    /// Creates a yield instruction to wait for a given number of seconds using unscaled time.
    /// </summary>
    /// <param name="time">Delay execution by the amount of time in seconds.</param>
    public static IEnumerator SecondsRealtime(float time) { yield return new WaitForSecondsRealtime(time); }
    /// <summary>
    /// Waits until the end of the frame after Unity has rendererd every Camera and GUI, just before displaying the frame on screen.
    /// </summary>
    public static IEnumerator EndOfFrame() { yield return new WaitForEndOfFrame(); }
    /// <summary>
    /// Waits until next fixed frame rate update function. See Also: MonoBehaviour.FixedUpdate.
    /// </summary>
    public static IEnumerator FixedUpdate() { yield return new WaitForFixedUpdate(); }
    /// <summary>
    /// Suspends the coroutine execution until the supplied delegate evaluates to true. See Also: WaitFor.Frames
    /// </summary>
    public static IEnumerator Until(System.Func<bool> predicate) { yield return new WaitUntil(predicate); }
    /// <summary>
    /// Suspends the coroutine execution until the supplied delegate evaluates to false.
    /// </summary>
    public static IEnumerator While(System.Func<bool> predicate) { yield return new WaitWhile(predicate); }
    #endregion


    /// <summary>
    /// Suspends the coroutine execution for a set amount of frames.
    /// </summary>
    /// <param name="frameCount">Delay execution by the amount of frames.</param>
    public static IEnumerator Frames(int frameCount)
    {
        while (frameCount > 0)
        {
            frameCount--;
            yield return null;
        }
    }

}
