using System;

/// <summary>
/// A better Cloneable interface that enforces support for deep cloning data into an existing object.
/// </summary>
/// <typeparam name="T"></typeparam>
public interface ICloneable<T> where T : class, ICloneable<T>
{
    public T Clone(T source = null);
}

public static class Xtensions_ICloneable
{
    public static T CloneInto<T>(this T source, T target) where T : class, ICloneable<T>
    {
        if (source == null) return null;
        target.Clone(source);
        return target;
    }
    public static T Clone<T>(this T source, T target = default)
    {
        target ??= Activator.CreateInstance<T>();
        target.Clone(source);
        return target;
    }
}