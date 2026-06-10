/// <summary>
/// A better Cloneable interface that enforces support for deep cloning data into an existing object.
/// </summary>
/// <typeparam name="T"></typeparam>
public interface ICloneable<T> where T : class
{
    /// <summary>
    /// Deep Clones this object, creating a new instance or populating the provided instance field.
    /// Note: Does not properly populate existing null fields. Use <see cref="Clone(out)"/> instead.
    /// </summary>
    public T Clone(T target = null);
}

public static class XtensionsICloneable
{
    /// <summary>
    /// Deep Clones this object into the null field provided.
    /// </summary>
    public static T Clone<T>(this T source, out T result) where T : class, ICloneable<T>
    {
        result = source.Clone();
        return result;
    }

    /// <summary>
    /// Populates this object with a Deep Clone of all of the source's data.
    /// NOTE: Does NOT work with null fields.
    /// </summary>
    public static T CloneFrom<T>(this T target, T source) where T : class, ICloneable<T>
    {
        source.Clone(target);
        return target;
    }
}