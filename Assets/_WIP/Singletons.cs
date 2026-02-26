using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Alternative implementation to ISingleton that only provides static helper methods for managing Singleton instances.
/// </summary>
public static class Singletons
{
    public enum SingletonOperationMessage
    {
        Success,
        AlreadyRegistered,
        NullInstance,
        NotRegisteredInstance,
    }


    public static SingletonOperationMessage Register<T>(ref T slot, T newInstance) where T : class
    {
        if(slot != null && slot != newInstance)
        {
            Debug.LogWarning($"Singleton of type {typeof(T)} is already registered. Ignoring new instance.");
            return SingletonOperationMessage.AlreadyRegistered;
        }
        if(newInstance == null)
        {
            Debug.LogWarning($"Cannot register null instance for singleton of type {typeof(T)}.");
            return SingletonOperationMessage.NullInstance;
        }

        slot = newInstance;
        dictionary[typeof(T)] = newInstance;
        return SingletonOperationMessage.Success;
    }

    public static SingletonOperationMessage SingletonRegister<T>(this T newInstance, ref T slot) 
        where T : UnityEngine.Object 
        => Register(ref slot, newInstance);

    public static SingletonOperationMessage Unregister<T>(ref T slot, T instance) where T : class
    {
        if(slot == null)
        {
            Debug.LogWarning($"No singleton of type {typeof(T)} is registered to unregister.");
            return SingletonOperationMessage.NullInstance;
        }
        if(slot != instance)
        {
            Debug.LogWarning($"The provided instance does not match the registered singleton of type {typeof(T)}.");
            return SingletonOperationMessage.NotRegisteredInstance;
        }
        slot = null;
        dictionary.Remove(typeof(T));
        return SingletonOperationMessage.Success;
    }

    public static SingletonOperationMessage SingletonUnregister<T>(this T instance, ref T slot) 
        where T : UnityEngine.Object
        => Unregister(ref slot, instance);

    public delegate object GetObjectDelegate();

    public static T Get<T>(ref T slot, params GetObjectDelegate[] createAttempts) where T : class
    {
        if (slot == null)
        {
            for (int i = 0; i < createAttempts.Length; i++)
            {
                slot = createAttempts[i]() as T;
                if (slot != null) break;
            }
        }
        return slot;
    }

    public static bool TryGet<T>(Func<T> getInstance, out T instance) where T : class
    {
        instance = getInstance();
        return instance != null;
    }


    private static Dictionary<Type, object> dictionary = new();

}
