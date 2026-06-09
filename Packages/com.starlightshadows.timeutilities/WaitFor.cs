using System.Collections;
using UnityEngine;
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
