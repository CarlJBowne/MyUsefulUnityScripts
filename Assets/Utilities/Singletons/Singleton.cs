using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A lightweight, centralized helper-based implementation of the Singleton design pattern.
/// Provides static helper methods for implementing and managing singleton patterns, 
/// such as Register, Unregister, Get, and TryGet.
/// </summary>
/// <remarks>
/// Also provides a plug-and-play MonoBehavior base by inhereting from <see cref="MonoBehaviour{T}"/> and an equivalent to static C# objects with <see cref="LateObject{T}"/>. For Singleton Asset functionality, see <see cref="GlobalAsset{T}"/>
/// </remarks>
public static class Singleton
{
    /// <summary>
    /// Messages returned by singleton operations to describe result state.
    /// </summary>
    public enum OperationMessage
    {
        /// <summary>
        /// Operation completed successfully.
        /// </summary>
        Success,
        /// <summary>
        /// An instance is already registered and a different instance was attempted to be registered.
        /// </summary>
        AlreadyRegistered,
        /// <summary>
        /// A null instance was provided where a non-null instance was expected.
        /// </summary>
        NullInstance,
        /// <summary>
        /// The provided instance does not match the currently registered instance.
        /// </summary>
        NotRegisteredInstance,
    }

    /// <summary>
    /// Attempts to register a singleton instance into the provided slot.
    /// If a different instance is already registered the registration is rejected.
    /// </summary>
    /// <typeparam name="T">Type of the singleton instance.</typeparam>
    /// <param name="slot">Reference to the slot holding the registered instance.</param>
    /// <param name="newInstance">The instance to register.</param>
    /// <returns>An <see cref="OperationMessage"/> indicating the result of the registration attempt.</returns>
    public static OperationMessage Register<T>(ref T slot, T newInstance) where T : class
    {
        if (slot != null && slot != newInstance)
        {
#if UNITY_EDITOR
            Debug.LogWarning($"Singleton of type {typeof(T)} is already registered. Ignoring new instance.");
#endif
            return OperationMessage.AlreadyRegistered;
        }
        if (newInstance == null)
        {
#if UNITY_EDITOR
            Debug.LogWarning($"Cannot register null instance for singleton of type {typeof(T)}.");
#endif
            return OperationMessage.NullInstance;
        }

        slot = newInstance;
        return OperationMessage.Success;
    }

    /// <summary>
    /// Attempts to unregister a singleton instance from the provided slot.
    /// </summary>
    /// <typeparam name="T">Type of the singleton instance.</typeparam>
    /// <param name="slot">Reference to the slot holding the registered instance.</param>
    /// <param name="instance">The instance expected to be currently registered.</param>
    /// <returns>An <see cref="OperationMessage"/> indicating the result of the unregistration attempt.</returns>
    public static OperationMessage Unregister<T>(ref T slot, T instance) where T : class
    {
        if (slot == null)
        {
#if UNITY_EDITOR
            Debug.LogWarning($"No singleton of type {typeof(T)} is registered to unregister.");
#endif
            return OperationMessage.NullInstance;
        }
        if (slot != instance)
        {
#if UNITY_EDITOR
            Debug.LogWarning($"The provided instance does not match the registered singleton of type {typeof(T)}.");
#endif
            return OperationMessage.NotRegisteredInstance;
        }
        slot = null;
        return OperationMessage.Success;
    }

    /// <summary>
    /// Returns the singleton instance stored in <paramref name="slot"/>, optionally attempting to create it
    /// using the provided <paramref name="createAttempts"/> functions if the slot is currently null.
    /// </summary>
    /// <typeparam name="T">Type of the singleton instance.</typeparam>
    /// <param name="slot">Reference to the slot that holds (or will hold) the instance.</param>
    /// <param name="createAttempts">Zero or more functions that will be invoked in order to attempt creating the instance when the slot is null.</param>
    /// <returns>The resolved singleton instance, or null if none could be resolved.</returns>
    public static T Get<T>(ref T slot, params Func<T>[] createAttempts) where T : class
    {
        if (slot == null)
        {
            for (int i = 0; i < createAttempts.Length; i++)
            {
                slot = createAttempts[i]();
                if (slot != null) break;
            }
        }
        return slot;
    }

    /// <summary>
    /// Attempts to retrieve an instance by invoking the provided getter function.
    /// </summary>
    /// <typeparam name="T">Type of the instance to retrieve.</typeparam>
    /// <param name="getInstance">Function that returns the instance to retrieve.</param>
    /// <param name="instance">Out parameter that receives the retrieved instance.</param>
    /// <returns>True if an instance was retrieved (non-null); otherwise false.</returns>
    public static bool TryGet<T>(Func<T> getInstance, out T instance)
    {
        instance = getInstance();
        return instance != null;
    }

    /// <summary>
    /// Attempts to retrieve an instance from a provided value (getter plug).
    /// </summary>
    /// <typeparam name="T">Type of the instance to retrieve.</typeparam>
    /// <param name="getterPlug">The value to return as the instance.</param>
    /// <param name="instance">Out parameter that receives the provided value.</param>
    /// <returns>True if the provided value is non-null; otherwise false.</returns>
    public static bool TryGet<T>(T getterPlug, out T instance)
    {
        instance = getterPlug;
        return instance != null;
    }


    /// <summary>
    /// Base helper for MonoBehaviour-based singletons.
    /// Inherit from `Singleton.MonoBehaviour&lt;YourType&gt;` to gain automatic registration on Awake/OnDestroy.
    /// </summary>
    /// <typeparam name="T">The concrete type inheriting this base class (the singleton type).</typeparam>
    public abstract class MonoBehaviour<T> : UnityEngine.MonoBehaviour where T : class
    {
        /// <summary>
        /// Backing field for the singleton instance.
        /// </summary>
        private static T instance;

        /// <summary>
        /// Gets the registered singleton instance, attempting any configured creation paths if necessary.
        /// </summary>
        public static T Get => Get(ref instance);

        /// <summary>
        /// Attempts to get the currently registered singleton instance.
        /// </summary>
        /// <param name="instance">Out parameter that receives the instance if present.</param>
        /// <returns>True if an instance is present; otherwise false.</returns>
        public static bool TryGet(out T instance) => Singleton.TryGet(Get, out instance);


        /// <summary>
        /// Unity Awake callback - registers this instance as the singleton when possible. If another instance is already registered,
        /// this GameObject will be destroyed.
        /// </summary>
        private void Awake()
        {
            var result = Register(ref instance, this as T);
            if (result == OperationMessage.AlreadyRegistered)
            {
                Destroy(gameObject);
                return;
            }
            OnInit();
        }

        /// <summary>
        /// Unity OnDestroy callback - unregisters this instance if it is the registered singleton.
        /// </summary>
        private void OnDestroy()
        {
            var res = Unregister(ref instance, this as T);
            if (res == OperationMessage.NotRegisteredInstance) return;
            OnDeInit();
        }

        /// <summary>
        /// Called after this instance has been successfully registered as the singleton. Override to perform initialization.
        /// </summary>
        protected virtual void OnInit() { }

        /// <summary>
        /// Called after this instance has been successfully unregistered as the singleton. Override to perform cleanup.
        /// </summary>
        protected virtual void OnDeInit() { }
    }

    /// <summary>
    /// Lightweight ScriptableObject base to allow `GlobalAsset` to inherit.
    /// </summary>
    public abstract class Z_Asset : ScriptableObject
    {
        /// <summary>
        /// Called by Unity when this ScriptableObject becomes enabled. Override in derived classes as needed.
        /// </summary>
        public virtual void OnEnable() { }
    }

    /// <summary>
    /// Helper for objects that are instantiated lazily when they're first needed. Effectively the same as a basic C# object but initialized when needed instead of the beginning of the project.
    /// </summary>
    /// <typeparam name="T">The concrete late object type.</typeparam>
    public abstract class LateObject<T> where T : class
    {
        /// <summary>
        /// Backing field for the late object singleton instance.
        /// </summary>
        private static T instance;

        /// <summary>
        /// Gets the late-instantiated singleton instance, creating and initializing it on first access.
        /// </summary>
        public static T Get
        {
            get
            {
                if (instance == null) return instance;
                LateObject<T> result = Activator.CreateInstance(typeof(T)) as LateObject<T>;
                result.Awake();
                return result as T;
            }
        }

        /// <summary>
        /// Attempts to get the late object singleton instance.
        /// </summary>
        /// <param name="instance">Out parameter that receives the instance if present.</param>
        /// <returns>True if an instance is present; otherwise false.</returns>
        public static bool TryGet(out T instance)
        {
            instance = Get as T;
            return instance != null;
        }

        /// <summary>
        /// Called when the late object is instantiated. Override in derived types to perform initialization.
        /// </summary>
        protected virtual void Awake()
        {

        }

        /// <summary>
        /// Called when the late object is destroyed. Override in derived types to perform cleanup.
        /// </summary>
        protected virtual void OnDestroy()
        {

        }

        /// <summary>
        /// Destroys the current late object instance, invoking its <see cref="OnDestroy"/> method if present.
        /// </summary>
        public static void Destroy()
        {
            if (instance == null) return;
            (instance as LateObject<T>).OnDestroy();
            instance = null;
        }
    }
}