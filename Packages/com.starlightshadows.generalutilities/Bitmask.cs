using System;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;

[System.Serializable]
public class Bitmask : IEquatable<Bitmask>
{
    /// <summary>
    /// Backing integer value representing the bitmask.
    /// </summary>
    public int intValue;

    /// <summary>
    /// Indexer to get or set an individual bit.
    /// Valid indices are 0..31. Negative or out-of-range indices throw <see cref="ArgumentOutOfRangeException"/>.
    /// </summary>
    /// <param name="i">Bit index (0-based).</param>
    public bool this[int i]
    {
        get
        {
            if (i is < 0 or > 31) throw new ArgumentOutOfRangeException(nameof(i));
            return (intValue & (1 << i)) != 0;
        }
        set
        {
            if (i is < 0 or > 31) throw new ArgumentOutOfRangeException(nameof(i));
            if (value) intValue |= 1 << i;
            else intValue &= ~(1 << i);
        }
    }


    #region Creation

    /// <summary>
    /// Create a BitwiseEnum with specific integer bitmask.
    /// </summary>
    /// <param name="intValue">Integer bitmask.</param>
    public Bitmask(int intValue = 0) => this.intValue = intValue;
    /// <summary>
    /// Create a BitwiseEnum from an array of booleans. Each true value sets the corresponding bit.
    /// </summary>
    /// <param name="inputs">Boolean array where index i sets bit i if true.</param>
    public Bitmask(params bool[] inputs)
    {
        intValue = 0;
        if (inputs == null) return;
        int maxBits = sizeof(int) * 8;
        int len = Math.Min(inputs.Length, maxBits);
        for (int i = 0; i < len; i++)
            if (inputs[i]) intValue |= 1 << i;
    }
    /// <summary>
    /// Create a BitwiseEnum with all the input values switched on.
    /// </summary>
    /// <param name="values"></param>
    public Bitmask(params int[] values)
    {
        intValue = 0;
        for (int i = 0; i < values.Length; i++)
            this[(int)values[0]] = true;
    }


    /// <summary>
    /// Create a BitwiseEnum Cloned from an existing one.
    /// </summary>
    /// <param name="source">The Source.</param>
    public Bitmask(Bitmask source) => new Bitmask(source.intValue);

    /// <summary>
    /// Explicit conversion to <see cref="int"/> returning the underlying bitmask.
    /// If <paramref name="value"/> is null, 0 is returned.
    /// </summary>
    public static explicit operator int(Bitmask value) => value?.intValue ?? 0;

    /// <summary>
    /// Explicit conversion from <see cref="int"/> to <see cref="Bitmask"/>.
    /// </summary>
    public static explicit operator Bitmask(int value) => new(value);

    /// <summary>
    /// Explicit conversion from <see cref="bool[]"/> to <see cref="Bitmask"/>.
    /// </summary>
    public static explicit operator Bitmask(bool[] inputs) => new(inputs);

    public static T New<T>(int input = 0) => (T)Activator.CreateInstance(typeof(T), input);
    public Bitmask Clone() => new(this);

    #endregion

    /// <summary>
    /// True if this instance contains the specified bit index.
    /// </summary>
    public bool Contains(int Other)
    {
        if (Other < 0 || Other >= sizeof(int) * 8) throw new ArgumentOutOfRangeException(nameof(Other));
        return (intValue & (1 << Other)) != 0;
    }

    /// <summary>
    /// Returns true if any bit in <paramref name="Other"/> is also set in this instance.
    /// Treats null as empty set.
    /// </summary>
    public bool ContainsAnyFrom(Bitmask Other)
    {
        if (Other == null) return false;
        return (intValue & Other.intValue) != 0;
    }

    /// <summary>
    /// Returns true if all bits set in <paramref name="Other"/> are also set in this instance.
    /// If <paramref name="Other"/> is null or zero, returns true.
    /// </summary>
    public bool ContainsAllOf(Bitmask Other)
    {
        if (Other == null) return true;
        return (intValue & Other.intValue) == Other.intValue;
    }

    #region Operators

    /// <summary>
    /// Returns a Bitmask where any flags from L OR R are true. Equivalent to | or + operators.
    /// </summary>
    public static Bitmask operator |(Bitmask L, Bitmask R) => L.OR(R, true);
    /// <summary>
    /// Returns a Bitmask where any flags from L OR R are true. Equivalent to | or + operators.
    /// </summary>
    public static Bitmask operator +(Bitmask L, Bitmask R) => L.OR(R, true);

    /// <summary>
    /// Returns a Bitmask where any flags on L AND R are true. 
    /// </summary>
    public static Bitmask operator &(Bitmask L, Bitmask R) => L.AND(R, true);
    /// <summary>
    /// Returns a Bitmask where any flags on L AND R are true. 
    /// </summary>
    public static Bitmask operator *(Bitmask L, Bitmask R) => L.AND(R, true);

    /// <summary>
    /// Returns a Bitmask where only flags true on one of the two operands, L/R are true. Equivalent to ^ or / operators.
    /// </summary>
    public static Bitmask operator ^(Bitmask L, Bitmask R) => L.XOR(R, true);
    /// <summary>
    /// Returns a Bitmask where only flags true on one of the two operands, L/R are true. Equivalent to ^ or / operators.
    /// </summary>
    public static Bitmask operator /(Bitmask L, Bitmask R) => L.XOR(R, true);



    /// <summary>
    /// Returns a Bitmask where the right index is added to the left Bitmask.
    /// </summary>
    public static Bitmask operator +(Bitmask L, int R) => L.ADD(R, true);
    /// <summary>
    /// Returns a Bitmask where the right indeces are added to the left Bitmask.
    /// </summary>
    public static Bitmask operator +(Bitmask L, int[] R) => L.ADD(R, true);
    /// <summary>
    /// Returns a Bitmask where the right indeces are added to the left Bitmask.
    /// </summary>
    public static Bitmask operator +(Bitmask L, List<int> R) => L.ADD(R, true);

    /// <summary>
    /// Returns a Bitmask where the right index is removed to the left Bitmask.
    /// </summary>
    public static Bitmask operator -(Bitmask L, int R) => L.REMOVE(R, true);
    /// <summary>
    /// Returns a Bitmask where the right indeces are removed to the left Bitmask.
    /// </summary>
    public static Bitmask operator -(Bitmask L, int[] R) => L.REMOVE(R, true);
    /// <summary>
    /// Returns a Bitmask where the right indeces are removed to the left Bitmask.
    /// </summary>
    public static Bitmask operator -(Bitmask L, List<int> R) => L.REMOVE(R, true);

    /// <summary>
    /// Returns a Bitmask where flags true on R are subtracted from L.
    /// </summary>
    public static Bitmask operator -(Bitmask L, Bitmask R) => L.XAND(R, true);
    /// <summary>
    /// Returns a Bitmask that is inverted from the input. 
    /// </summary>
    public static Bitmask operator ~(Bitmask L) => L.INVERT(true);


    /// <summary>
    /// Equality operator. True if both are the same reference or both non-null with equal integer masks.
    /// </summary>
    public static bool operator ==(Bitmask L, Bitmask R) =>
        ReferenceEquals(L, R) ? true
        : L is null || R is null ? false
        : L.intValue == R.intValue;

    /// <summary>
    /// Inequality operator.
    /// </summary>
    public static bool operator !=(Bitmask L, Bitmask R) => !(L == R);

    /// <summary>
    /// Inclusion operator. True if both are the same reference or both non-null with equal integer masks.
    /// </summary>
    public static bool operator ==(Bitmask L, int R)
    {
        if (L == null) return false;
        if (R is < 0 or > 31) throw new ArgumentOutOfRangeException("Index outside of 0..31 Range");
        return L[R] == true;
    }

    /// <summary>
    /// Uninclusion operator.
    /// </summary>
    public static bool operator !=(Bitmask L, int R) => !(L == R);

    #endregion


    /// <summary>
    /// Determines whether the specified object is equal to the current BitwiseEnum.
    /// Accepts BitwiseEnum or int for comparison.
    /// </summary>
    public override bool Equals(object obj)
        => ReferenceEquals(this, obj)
           || (obj is Bitmask other && intValue == other.intValue)
           || (obj is int i && intValue == i);

    /// <summary>
    /// IEquatable implementation.
    /// </summary>
    public bool Equals(Bitmask other) => this == other;

    /// <summary>
    /// Hash code based on the integer mask.
    /// </summary>
    public override int GetHashCode() => intValue;

    /// <summary>
    /// String representation of the integer mask.
    /// </summary>
    public override string ToString() => intValue.ToString();

}

public static class Xtensions_Bitmasks_Class
{

    /// <summary>
    /// Returns a Bitmask where any flags from L OR R are true. Equivalent to | or + operators.
    /// </summary>
    public static T OR<T>(this T l, T r, bool clone = false) where T : Bitmask
    {
        if (clone) l = Bitmask.New<T>(l.intValue);
        l.intValue |= r.intValue;
        return l;
    }
    /// <summary>
    /// Returns a Bitmask where any flags on L AND R are true. Equivalent to &amp; or * operators.
    /// </summary>
    public static T AND<T>(this T l, T r, bool clone = false) where T : Bitmask
    {
        if (clone) l = Bitmask.New<T>(l.intValue);
        l.intValue &= r.intValue;
        return l;
    }
    /// <summary>
    /// Returns a Bitmask where only flags true on one of the two operands, L/R are true. Equivalent to ^ or / operators.
    /// </summary>
    public static T XOR<T>(this T l, T r, bool clone = false) where T : Bitmask
    {
        if (clone) l = Bitmask.New<T>(l.intValue);
        l.intValue ^= r.intValue;
        return l;
    }
    /// <summary>
    /// Returns a Bitmask where flags true on R are subtracted from L. Equivalent to &amp;~ or -;
    /// </summary>
    public static T XAND<T>(this T l, T r, bool clone = false) where T : Bitmask
    {
        if (clone) l = Bitmask.New<T>(l.intValue);
        l.intValue &= ~r.intValue;
        return l;
    }
    /// <summary>
    /// Returns a Bitmask where the right index is added to the left Bitmask.
    /// </summary>
    public static T ADD<T>(this T l, int idx, bool clone = false) where T : Bitmask
    {
        if (clone) l = Bitmask.New<T>(l.intValue);
        if (idx is < 0 or > 32) throw new ArgumentOutOfRangeException(nameof(idx));
        l[idx] = true;
        return l;
    }

    /// <summary>
    /// Returns a Bitmask where the right indeces are added to the left Bitmask.
    /// </summary>
    public static T ADD<T>(this T l, int[] indices, bool clone = false) where T : Bitmask
    {
        if (clone) l = Bitmask.New<T>(l.intValue);
        if (indices != null)
            for (int i = 0; i < indices.Length; i++)
            {
                if (indices[i] is < 0 or > 31) throw new ArgumentOutOfRangeException(nameof(indices), "Bit index out of range.");
                l[indices[i]] = true;
            }
        return l;
    }
    /// <summary>
    /// Returns a Bitmask where the right indeces are added to the left Bitmask.
    /// </summary>
    public static T ADD<T>(this T l, bool clone = false, params int[] indices) where T : Bitmask
    {
        if (clone) l = Bitmask.New<T>(l.intValue);
        if (indices != null)
            for (int i = 0; i < indices.Length; i++)
            {
                if (indices[i] is < 0 or > 31) throw new ArgumentOutOfRangeException(nameof(indices), "Bit index out of range.");
                l[indices[i]] = true;
            }
        return l;
    }

    /// <summary>
    /// Returns a Bitmask where the right indeces are added to the left Bitmask.
    /// </summary>
    public static T ADD<T>(this T l, List<int> indices, bool clone = false) where T : Bitmask
    {
        if (clone) l = Bitmask.New<T>(l.intValue);
        if (indices != null)
            for (int i = 0; i < indices.Count; i++)
            {
                if (indices[i] is < 0 or > 31) throw new ArgumentOutOfRangeException(nameof(indices), "Bit index out of range.");
                l[indices[i]] = true;
            }
        return l;
    }

    /// <summary>
    /// Returns a Bitmask where the right index is removed to the left Bitmask.
    /// </summary>
    public static T REMOVE<T>(this T l, int idx, bool clone = false) where T : Bitmask
    {
        if (clone) l = Bitmask.New<T>(l.intValue);
        if (idx is < 0 or > 32) throw new ArgumentOutOfRangeException(nameof(idx));
        l[idx] = false;
        return l;
    }

    /// <summary>
    /// Returns a Bitmask where the right indeces are removed to the left Bitmask.
    /// </summary>
    public static T REMOVE<T>(this T l, int[] indices, bool clone = false) where T : Bitmask
    {
        if (clone) l = Bitmask.New<T>(l.intValue);
        if (indices != null)
            for (int i = 0; i < indices.Length; i++)
            {
                if (indices[i] is < 0 or > 31) throw new ArgumentOutOfRangeException(nameof(indices), "Bit index out of range.");
                l[indices[i]] = false;
            }
        return l;
    }
    /// <summary>
    /// Returns a Bitmask where the right indeces are removed to the left Bitmask.
    /// </summary>
    public static T REMOVE<T>(this T l, bool clone = false, params int[] indices) where T : Bitmask
    {
        if (clone) l = Bitmask.New<T>(l.intValue);
        if (indices != null)
            for (int i = 0; i < indices.Length; i++)
            {
                if (indices[i] is < 0 or > 31) throw new ArgumentOutOfRangeException(nameof(indices), "Bit index out of range.");
                l[indices[i]] = false;
            }
        return l;
    }

    /// <summary>
    /// Returns a Bitmask where the right indeces are removed to the left Bitmask.
    /// </summary>
    public static T REMOVE<T>(this T l, List<int> indices, bool clone = false) where T : Bitmask
    {
        if (clone) l = Bitmask.New<T>(l.intValue);
        if (indices != null)
            for (int i = 0; i < indices.Count; i++)
            {
                if (indices[i] is < 0 or > 31) throw new ArgumentOutOfRangeException(nameof(indices), "Bit index out of range.");
                l[indices[i]] = false;
            }
        return l;
    }

    /// <summary>
    /// Returns a Bitmask that is inverted from the input. Equivalent to the ~ operator.
    /// </summary>
    public static T INVERT<T>(this T input, bool clone = false) where T : Bitmask
    {
        if (clone) input = Bitmask.New<T>(input.intValue);
        input.intValue = ~input.intValue;
        return input;
    }

}