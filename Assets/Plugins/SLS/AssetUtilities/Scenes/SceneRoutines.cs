using System.Collections;
using UnityEngine;

public class SceneRoutine : Coroutine
{
    public SceneRoutine(IEnumerator enumerator, MonoBehaviour owner) : base(enumerator, owner)
    {
    }

    public SceneRoutine(IEnumerator enumerator, bool automatic, MonoBehaviour owner = null) : base(enumerator, automatic, owner)
    {

    }

    
}
