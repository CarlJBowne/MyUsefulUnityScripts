using System;

/// <summary>
/// A better Cloneable interface that enforces support for deep cloning data into an existing object.
/// </summary>
/// <typeparam name="T"></typeparam>
public interface ICloneable<T> where T : class, ICloneable<T>
{
    /// <summary>
    /// The center of the <see cref="ICloneable{T}"/> interface. This method should copy all data from the <paramref name="source"/> object into this object, overwriting any existing data.
    /// </summary>
    /// <param name="source">The source object</param>
    /// <returns>Should return the resulting destination object</returns>
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
}