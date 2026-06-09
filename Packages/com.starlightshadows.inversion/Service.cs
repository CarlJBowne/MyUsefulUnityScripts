using System;

/// <summary>
/// A near instant way to create Services! <br/>
/// Add a static one to any class and initialize it in its source via constructor and voila!
/// </summary>
public class Service<T>
{
    public Service(Func<T> input) => Getter = input;
    private readonly Func<T> Getter;
    public static implicit operator T(Service<T> This) => This.Getter != null ? This.Getter() : default;
    public T Get => Getter != null ? Getter() : default;
    public bool TryGet(out T value)
    {
        value = Getter != null ? Getter() : default;
        return Getter != null;
    }
}

/// <summary>
/// A near instant way to create Getter AND Setter Services! <br/>
/// Add a static one to any class and initialize it in its source via constructor and voila!
/// </summary>
public class GetterSetterService<T>
{
    public GetterSetterService() { }
    public GetterSetterService(Func<T> getter, Action<T> setter)
    {
        this.Getter = getter;
        this.Setter = setter;
    }
    public Func<T> Getter;
    public Action<T> Setter;
    public static implicit operator T(GetterSetterService<T> This) => This.Getter != null ? This.Getter() : default;

    public T Get => Getter != null ? Getter() : default;

    public bool TryGet(out T value)
    {
        value = Getter != null ? Getter() : default;
        return Getter != null;
    }

    public void Set(T value) => Setter?.Invoke(value);

    public T Value
    {
        get => Getter != null ? Getter() : default;
        set => Setter?.Invoke(value);
    }

}

/// <summary>
/// Alternate form of Services. Generally less useful.
/// </summary>
/// <typeparam name="T"></typeparam>
public interface IService<T> where T : class, IService<T>
{
    protected static T SInstance;

    /// <summary>
    /// Messages returned by IService operations to describe result state.
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

    public static OperationMessage Register(T item)
    {
        if (SInstance != null) return OperationMessage.AlreadyRegistered;
        if (item == null) return OperationMessage.NullInstance;
        SInstance = item;
        return OperationMessage.Success;
    }
    public static OperationMessage Deregister(T item)
    {
        if (SInstance == null) return OperationMessage.NullInstance;
        if (SInstance != item) return OperationMessage.NotRegisteredInstance;
        SInstance = null;
        return OperationMessage.Success;
    }
}