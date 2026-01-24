using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class NewSingletonTest : MonoBehaviour
{
    private static NewSingletonTest _instance;

    public static NewSingletonTest Get() => Singletons.Get(ref _instance);
    public static bool TryGet(out NewSingletonTest instance) => Singletons.TryGet(Get, out instance);


    private void Awake()
    {
        var result = this.SingletonRegister(ref _instance);
        if(result != Singletons.SingletonOperationMessage.Success)
        {
            Destroy(this);
        }
    }
    private void OnDestroy()
    {
        if (_instance == this) this.SingletonUnregister(ref _instance);
    }
}
