using System;
using System.Collections.Generic;
using System.Linq;

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
    public Bitmask(int intValue) => this.intValue = intValue;

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

    #endregion

    #region CENTRAL IMPLEMENTATIONS

    /// <summary>
    /// Returns a Bitmask where any flags from L OR R are true. Equivalent to | or + operators.
    /// </summary>
    public static Bitmask OR(Bitmask L, Bitmask R)
    {
        L ??= new(); R ??= new();
        return new(L.intValue | R.intValue);
    }

    /// <summary>
    /// Returns a Bitmask where any flags on L AND R are true. Equivalent to &amp; or * operators.
    /// </summary>
    public static Bitmask AND(Bitmask L, Bitmask R)
    {
        L ??= new(); R ??= new();
        return new(L.intValue & R.intValue);
    }

    /// <summary>
    /// Returns a Bitmask where only flags true on one of the two operands, L/R are true. Equivalent to ^ or / operators.
    /// </summary>
    public static Bitmask XOR(Bitmask L, Bitmask R)
    {
        L ??= new(); R ??= new();
        return new(L.intValue ^ R.intValue);
    }

    /// <summary>
    /// Returns a Bitmask where flags true on R are subtracted from L. Equivalent to &amp;~ or -;
    /// </summary>
    public static Bitmask XAND(Bitmask L, Bitmask R)
    {
        L ??= new(); R ??= new();
        return new(L.intValue & ~R.intValue);
    }

    /// <summary>
    /// Returns a Bitmask where the right index is added to the left Bitmask.
    /// </summary>
    public static Bitmask ADD(Bitmask L, int idx)
    {
        L ??= new();
        if (idx is < 0 or > 32) throw new ArgumentOutOfRangeException(nameof(idx));
        return new(L.intValue | (1 << idx));
    }

    /// <summary>
    /// Returns a Bitmask where the right indeces are added to the left Bitmask.
    /// </summary>
    public static Bitmask ADD(Bitmask L, int[] indices)
    {
        L ??= new();
        Bitmask res = new(L.intValue);
        if (indices != null)
            for (int i = 0; i < indices.Length; i++)
            {
                if (indices[i] is < 0 or > 31) throw new ArgumentOutOfRangeException(nameof(indices), "Bit index out of range.");
                res[indices[i]] = true;
            }
        return res;
    }

    /// <summary>
    /// Returns a Bitmask where the right indeces are added to the left Bitmask.
    /// </summary>
    public static Bitmask ADD(Bitmask L, List<int> indices)
    {
        L ??= new();
        Bitmask res = new(L.intValue);
        if (indices != null)
            for (int i = 0; i < indices.Count; i++)
            {
                if (indices[i] is < 0 or > 31) throw new ArgumentOutOfRangeException(nameof(indices), "Bit index out of range.");
                res[indices[i]] = true;
            }
        return res;
    }

    /// <summary>
    /// Returns a Bitmask where the right index is removed to the left Bitmask.
    /// </summary>
    public static Bitmask REMOVE(Bitmask L, int idx)
    {
        L ??= new();
        if (idx is < 0 or > 32) throw new ArgumentOutOfRangeException(nameof(idx));
        Bitmask res = new(L.intValue);
        res[idx] = false;
        return res;
    }

    /// <summary>
    /// Returns a Bitmask where the right indeces are removed to the left Bitmask.
    /// </summary>
    public static Bitmask REMOVE(Bitmask L, int[] indices)
    {
        L ??= new();
        Bitmask res = new(L.intValue);
        if (indices != null)
            for (int i = 0; i < indices.Length; i++)
            {
                if (indices[i] is < 0 or > 31) throw new ArgumentOutOfRangeException(nameof(indices), "Bit index out of range.");
                res[indices[i]] = false;
            }
        return res;
    }

    /// <summary>
    /// Returns a Bitmask where the right indeces are removed to the left Bitmask.
    /// </summary>
    public static Bitmask REMOVE(Bitmask L, List<int> indices)
    {
        L ??= new();
        Bitmask res = new(L.intValue);
        if (indices != null)
            for (int i = 0; i < indices.Count; i++)
            {
                if (indices[i] is < 0 or > 31) throw new ArgumentOutOfRangeException(nameof(indices), "Bit index out of range.");
                res[indices[i]] = false;
            }
        return res;
    }

    /// <summary>
    /// Returns a Bitmask that is inverted from the input. Equivalent to the ~ operator.
    /// </summary>
    public static Bitmask INVERT(Bitmask input) => input != null ? new(~input.intValue) : new(~0);

    #endregion

    #region Functions
    /// <summary>
    /// Returns a Bitmask where any flags from L OR R are true. Equivalent to | or + operators.
    /// </summary>
    public Bitmask Or(Bitmask Other) => OR(this, Other);

    /// <summary>
    /// Returns a Bitmask where any flags on L AND R are true. Equivalent to & or * operators.
    /// </summary>
    public Bitmask And(Bitmask Other) => AND(this, Other);

    /// <summary>
    /// Returns a Bitmask where only flags true on one of the two operands, L/R are true. Equivalent to ^ or / operators.
    /// </summary>
    public Bitmask Xor(Bitmask Other) => XOR(this, Other);

    /// <summary>
    /// Returns a Bitmask where flags true on R are subtracted from L. Equivalent to &~ or -;
    /// </summary>
    public Bitmask Combine(Bitmask Other) => XOR(this, Other);

    /// <summary>
    /// Returns a Bitmask where any flags from L OR R are true. Equivalent to | or + operators.
    /// </summary>
    public Bitmask Add(Bitmask Other) => OR(this, Other);

    /// <summary>
    /// Subtract bits present in <paramref name="Other"/>.
    /// Layman: "Clears bits from the left operand that are present in the right operand."
    /// Equivalent bitwise operation: L & ~R.
    /// </summary>
    public Bitmask Subtract(Bitmask Other) => XAND(this, Other);

    /// <summary>
    /// Multiply as AND (alias).
    /// Layman: "Keeps only bits present in both operands."
    /// Equivalent bitwise operation: AND (&).
    /// </summary>
    public Bitmask Multiply(Bitmask Other) => AND(this, Other);

    /// <summary>
    /// Divide as XOR (alias).
    /// Layman: "Keeps bits that differ between operands."
    /// Equivalent bitwise operation: XOR (^).
    /// </summary>
    public Bitmask Divide(Bitmask Other) => XOR(this, Other);

    /// <summary>
    /// Add single bit index. Sets the specified bit and returns a new instance.
    /// Layman: "Sets the specified bit index."
    /// Equivalent bitwise operation: base | (1 << index).
    /// </summary>
    public Bitmask Add(int Other) => ADD(this, Other);

    /// <summary>
    /// Remove single bit index. Clears the specified bit and returns a new instance.
    /// Layman: "Clears the specified bit index."
    /// Equivalent bitwise operation: base & ~(1 << index).
    /// </summary>
    public Bitmask Subtract(int Other) => REMOVE(this, Other);

    /// <summary>
    /// Add array of bit indices. Sets all specified indices and returns a new instance.
    /// Layman: "Sets all indices provided in the array."
    /// Equivalent bitwise operation: OR each (1<<index).
    /// </summary>
    public Bitmask Add(int[] Other) => ADD(this, Other);

    /// <summary>
    /// Remove array of bit indices. Clears all specified indices and returns a new instance.
    /// Layman: "Clears all indices provided in the array."
    /// Equivalent bitwise operation: AND with inverted bits for each index.
    /// </summary>
    public Bitmask Subtract(int[] Other) => REMOVE(this, Other);

    /// <summary>
    /// Add list of bit indices. Sets all specified indices and returns a new instance.
    /// Layman: "Sets all indices provided in the list."
    /// Equivalent bitwise operation: OR each (1<<index).
    /// </summary>
    public Bitmask Add(List<int> Other) => ADD(this, Other);

    /// <summary>
    /// Remove list of bit indices. Clears all specified indices and returns a new instance.
    /// Layman: "Clears all indices provided in the list."
    /// Equivalent bitwise operation: AND with inverted bits for each index.
    /// </summary>
    public Bitmask Subtract(List<int> Other) => REMOVE(this, Other);

    /// <summary>
    /// Returns a new instance with all bits inverted.
    /// Layman: "Flips every bit in the mask."
    /// Equivalent bitwise operation: ~.
    /// </summary>
    public Bitmask Inverted() => INVERT(this);

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
    #endregion

    #region Operators

    /// <summary>
    /// Returns a Bitmask where any flags from L OR R are true. Equivalent to | or + operators.
    /// </summary>
    public static Bitmask operator |(Bitmask L, Bitmask R) => OR(L, R);
    /// <summary>
    /// Returns a Bitmask where any flags from L OR R are true. Equivalent to | or + operators.
    /// </summary>
    public static Bitmask operator +(Bitmask L, Bitmask R) => OR(L, R);

    /// <summary>
    /// Returns a Bitmask where any flags on L AND R are true. 
    /// </summary>
    public static Bitmask operator &(Bitmask L, Bitmask R) => AND(L, R);
    /// <summary>
    /// Returns a Bitmask where any flags on L AND R are true. 
    /// </summary>
    public static Bitmask operator *(Bitmask L, Bitmask R) => AND(L, R);

    /// <summary>
    /// Returns a Bitmask where only flags true on one of the two operands, L/R are true. Equivalent to ^ or / operators.
    /// </summary>
    public static Bitmask operator ^(Bitmask L, Bitmask R) => XOR(L, R);
    /// <summary>
    /// Returns a Bitmask where only flags true on one of the two operands, L/R are true. Equivalent to ^ or / operators.
    /// </summary>
    public static Bitmask operator /(Bitmask L, Bitmask R) => XOR(L, R);



    /// <summary>
    /// Returns a Bitmask where the right index is added to the left Bitmask.
    /// </summary>
    public static Bitmask operator +(Bitmask L, int R) => ADD(L, R);
    /// <summary>
    /// Returns a Bitmask where the right indeces are added to the left Bitmask.
    /// </summary>
    public static Bitmask operator +(Bitmask L, int[] R) => ADD(L, R);
    /// <summary>
    /// Returns a Bitmask where the right indeces are added to the left Bitmask.
    /// </summary>
    public static Bitmask operator +(Bitmask L, List<int> R) => ADD(L, R);

    /// <summary>
    /// Returns a Bitmask where the right index is removed to the left Bitmask.
    /// </summary>
    public static Bitmask operator -(Bitmask L, int R) => REMOVE(L, R);
    /// <summary>
    /// Returns a Bitmask where the right indeces are removed to the left Bitmask.
    /// </summary>
    public static Bitmask operator -(Bitmask L, int[] R) => REMOVE(L, R);
    /// <summary>
    /// Returns a Bitmask where the right indeces are removed to the left Bitmask.
    /// </summary>
    public static Bitmask operator -(Bitmask L, List<int> R) => REMOVE(L, R);

    /// <summary>
    /// Returns a Bitmask where flags true on R are subtracted from L.
    /// </summary>
    public static Bitmask operator -(Bitmask L, Bitmask R) => XAND(L, R);
    /// <summary>
    /// Returns a Bitmask that is inverted from the input. 
    /// </summary>
    public static Bitmask operator ~(Bitmask L) => INVERT(L);


    /// <summary>
    /// Equality operator. True if both are the same reference or both non-null with equal integer masks.
    /// </summary>
    public static bool operator ==(Bitmask L, Bitmask R)
    {
        if (ReferenceEquals(L, R)) return true;
        if (L is null || R is null) return false;
        return L.intValue == R.intValue;
    }

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
[System.Serializable]
public class LongBitMask : IEquatable<LongBitMask>
{
    /// <summary>
    /// Backing integer value representing the bitmask.
    /// </summary>
    public long longValue;

    /// <summary>
    /// Indexer to get or set an individual bit.
    /// Valid indices are 0..63. Negative or out-of-range indices throw <see cref="ArgumentOutOfRangeException"/>.
    /// </summary>
    /// <param name="i">Bit index (0-based).</param>
    public bool this[int i]
    {
        get
        {
            if (i is < 0 or > 63) throw new ArgumentOutOfRangeException(nameof(i));
            return (longValue & (1 << i)) != 0;
        }
        set
        {
            if (i is < 0 or > 63) throw new ArgumentOutOfRangeException(nameof(i));
            if (value) longValue |= 1L << i;
            else longValue &= ~(1 << i);
        }
    }


    #region Creation

    /// <summary>
    /// Create a BitwiseEnum with specific integer bitmask.
    /// </summary>
    /// <param name="longValue">Integer bitmask.</param>
    public LongBitMask(long longValue) => this.longValue = longValue;

    /// <summary>
    /// Create a BitwiseEnum from an array of booleans. Each true value sets the corresponding bit.
    /// </summary>
    /// <param name="inputs">Boolean array where index i sets bit i if true.</param>
    public LongBitMask(params bool[] inputs)
    {
        longValue = 0;
        if (inputs == null) return;
        int maxBits = sizeof(int) * 8;
        int len = Math.Min(inputs.Length, maxBits);
        for (int i = 0; i < len; i++)
            if (inputs[i]) longValue |= 1L << i;
    }

    /// <summary>
    /// Create a BitwiseEnum Cloned from an existing one.
    /// </summary>
    /// <param name="source">The Source.</param>
    public LongBitMask(LongBitMask source) => new LongBitMask(source.longValue);

    /// <summary>
    /// Explicit conversion to <see cref="int"/> returning the underlying bitmask.
    /// If <paramref name="value"/> is null, 0 is returned.
    /// </summary>
    public static explicit operator long(LongBitMask value) => value?.longValue ?? 0;

    /// <summary>
    /// Explicit conversion from <see cref="int"/> to <see cref="LongBitMask"/>.
    /// </summary>
    public static explicit operator LongBitMask(int value) => new(value);

    /// <summary>
    /// Explicit conversion from <see cref="bool[]"/> to <see cref="LongBitMask"/>.
    /// </summary>
    public static explicit operator LongBitMask(bool[] inputs) => new(inputs);

    #endregion

    #region CENTRAL IMPLEMENTATIONS

    /// <summary>
    /// Returns a LongBitMask where any flags from L OR R are true. Equivalent to | or + operators.
    /// </summary>
    public static LongBitMask OR(LongBitMask L, LongBitMask R)
    {
        L ??= new(); R ??= new();
        return new(L.longValue | R.longValue);
    }

    /// <summary>
    /// Returns a LongBitMask where any flags on L AND R are true. Equivalent to &amp; or * operators.
    /// </summary>
    public static LongBitMask AND(LongBitMask L, LongBitMask R)
    {
        L ??= new(); R ??= new();
        return new(L.longValue & R.longValue);
    }

    /// <summary>
    /// Returns a LongBitMask where only flags true on one of the two operands, L/R are true. Equivalent to ^ or / operators.
    /// </summary>
    public static LongBitMask XOR(LongBitMask L, LongBitMask R)
    {
        L ??= new(); R ??= new();
        return new(L.longValue ^ R.longValue);
    }

    /// <summary>
    /// Returns a LongBitMask where flags true on R are subtracted from L. Equivalent to &amp;~ or -;
    /// </summary>
    public static LongBitMask XAND(LongBitMask L, LongBitMask R)
    {
        L ??= new(); R ??= new();
        return new(L.longValue & ~R.longValue);
    }

    /// <summary>
    /// Returns a LongBitMask where the right index is added to the left LongBitMask.
    /// </summary>
    public static LongBitMask ADD(LongBitMask L, int idx)
    {
        L ??= new();
        if (idx is < 0 or > 32) throw new ArgumentOutOfRangeException(nameof(idx));
        return new(L.longValue | (1L << idx));
    }

    /// <summary>
    /// Returns a LongBitMask where the right indeces are added to the left LongBitMask.
    /// </summary>
    public static LongBitMask ADD(LongBitMask L, int[] indices)
    {
        L ??= new();
        LongBitMask res = new(L.longValue);
        if (indices != null)
            for (int i = 0; i < indices.Length; i++)
            {
                if (indices[i] is < 0 or > 63) throw new ArgumentOutOfRangeException(nameof(indices), "Bit index out of range.");
                res[indices[i]] = true;
            }
        return res;
    }

    /// <summary>
    /// Returns a LongBitMask where the right indeces are added to the left LongBitMask.
    /// </summary>
    public static LongBitMask ADD(LongBitMask L, List<int> indices)
    {
        L ??= new();
        LongBitMask res = new(L.longValue);
        if (indices != null)
            for (int i = 0; i < indices.Count; i++)
            {
                if (indices[i] is < 0 or > 63) throw new ArgumentOutOfRangeException(nameof(indices), "Bit index out of range.");
                res[indices[i]] = true;
            }
        return res;
    }

    /// <summary>
    /// Returns a LongBitMask where the right index is removed to the left LongBitMask.
    /// </summary>
    public static LongBitMask REMOVE(LongBitMask L, int idx)
    {
        L ??= new();
        if (idx is < 0 or > 32) throw new ArgumentOutOfRangeException(nameof(idx));
        LongBitMask res = new(L.longValue);
        res[idx] = false;
        return res;
    }

    /// <summary>
    /// Returns a LongBitMask where the right indeces are removed to the left LongBitMask.
    /// </summary>
    public static LongBitMask REMOVE(LongBitMask L, int[] indices)
    {
        L ??= new();
        LongBitMask res = new(L.longValue);
        if (indices != null)
            for (int i = 0; i < indices.Length; i++)
            {
                if (indices[i] is < 0 or > 63) throw new ArgumentOutOfRangeException(nameof(indices), "Bit index out of range.");
                res[indices[i]] = false;
            }
        return res;
    }

    /// <summary>
    /// Returns a LongBitMask where the right indeces are removed to the left LongBitMask.
    /// </summary>
    public static LongBitMask REMOVE(LongBitMask L, List<int> indices)
    {
        L ??= new();
        LongBitMask res = new(L.longValue);
        if (indices != null)
            for (int i = 0; i < indices.Count; i++)
            {
                if (indices[i] is < 0 or > 63) throw new ArgumentOutOfRangeException(nameof(indices), "Bit index out of range.");
                res[indices[i]] = false;
            }
        return res;
    }

    /// <summary>
    /// Returns a LongBitMask that is inverted from the input. Equivalent to the ~ operator.
    /// </summary>
    public static LongBitMask INVERT(LongBitMask input) => input != null ? new(~input.longValue) : new(~0);

    #endregion

    #region Functions
    /// <summary>
    /// Returns a LongBitMask where any flags from L OR R are true. Equivalent to | or + operators.
    /// </summary>
    public LongBitMask Or(LongBitMask Other) => OR(this, Other);

    /// <summary>
    /// Returns a LongBitMask where any flags on L AND R are true. Equivalent to & or * operators.
    /// </summary>
    public LongBitMask And(LongBitMask Other) => AND(this, Other);

    /// <summary>
    /// Returns a LongBitMask where only flags true on one of the two operands, L/R are true. Equivalent to ^ or / operators.
    /// </summary>
    public LongBitMask Xor(LongBitMask Other) => XOR(this, Other);

    /// <summary>
    /// Returns a LongBitMask where flags true on R are subtracted from L. Equivalent to &~ or -;
    /// </summary>
    public LongBitMask Combine(LongBitMask Other) => XOR(this, Other);

    /// <summary>
    /// Returns a LongBitMask where any flags from L OR R are true. Equivalent to | or + operators.
    /// </summary>
    public LongBitMask Add(LongBitMask Other) => OR(this, Other);

    /// <summary>
    /// Subtract bits present in <paramref name="Other"/>.
    /// Layman: "Clears bits from the left operand that are present in the right operand."
    /// Equivalent bitwise operation: L & ~R.
    /// </summary>
    public LongBitMask Subtract(LongBitMask Other) => XAND(this, Other);

    /// <summary>
    /// Multiply as AND (alias).
    /// Layman: "Keeps only bits present in both operands."
    /// Equivalent bitwise operation: AND (&).
    /// </summary>
    public LongBitMask Multiply(LongBitMask Other) => AND(this, Other);

    /// <summary>
    /// Divide as XOR (alias).
    /// Layman: "Keeps bits that differ between operands."
    /// Equivalent bitwise operation: XOR (^).
    /// </summary>
    public LongBitMask Divide(LongBitMask Other) => XOR(this, Other);

    /// <summary>
    /// Add single bit index. Sets the specified bit and returns a new instance.
    /// Layman: "Sets the specified bit index."
    /// Equivalent bitwise operation: base | (1 << index).
    /// </summary>
    public LongBitMask Add(int Other) => ADD(this, Other);

    /// <summary>
    /// Remove single bit index. Clears the specified bit and returns a new instance.
    /// Layman: "Clears the specified bit index."
    /// Equivalent bitwise operation: base & ~(1 << index).
    /// </summary>
    public LongBitMask Subtract(int Other) => REMOVE(this, Other);

    /// <summary>
    /// Add array of bit indices. Sets all specified indices and returns a new instance.
    /// Layman: "Sets all indices provided in the array."
    /// Equivalent bitwise operation: OR each (1<<index).
    /// </summary>
    public LongBitMask Add(int[] Other) => ADD(this, Other);

    /// <summary>
    /// Remove array of bit indices. Clears all specified indices and returns a new instance.
    /// Layman: "Clears all indices provided in the array."
    /// Equivalent bitwise operation: AND with inverted bits for each index.
    /// </summary>
    public LongBitMask Subtract(int[] Other) => REMOVE(this, Other);

    /// <summary>
    /// Add list of bit indices. Sets all specified indices and returns a new instance.
    /// Layman: "Sets all indices provided in the list."
    /// Equivalent bitwise operation: OR each (1<<index).
    /// </summary>
    public LongBitMask Add(List<int> Other) => ADD(this, Other);

    /// <summary>
    /// Remove list of bit indices. Clears all specified indices and returns a new instance.
    /// Layman: "Clears all indices provided in the list."
    /// Equivalent bitwise operation: AND with inverted bits for each index.
    /// </summary>
    public LongBitMask Subtract(List<int> Other) => REMOVE(this, Other);

    /// <summary>
    /// Returns a new instance with all bits inverted.
    /// Layman: "Flips every bit in the mask."
    /// Equivalent bitwise operation: ~.
    /// </summary>
    public LongBitMask Inverted() => INVERT(this);

    /// <summary>
    /// True if this instance contains the specified bit index.
    /// </summary>
    public bool Contains(int Other)
    {
        if (Other < 0 || Other >= sizeof(int) * 8) throw new ArgumentOutOfRangeException(nameof(Other));
        return (longValue & (1 << Other)) != 0;
    }

    /// <summary>
    /// Returns true if any bit in <paramref name="Other"/> is also set in this instance.
    /// Treats null as empty set.
    /// </summary>
    public bool ContainsAnyFrom(LongBitMask Other)
    {
        if (Other == null) return false;
        return (longValue & Other.longValue) != 0;
    }

    /// <summary>
    /// Returns true if all bits set in <paramref name="Other"/> are also set in this instance.
    /// If <paramref name="Other"/> is null or zero, returns true.
    /// </summary>
    public bool ContainsAllOf(LongBitMask Other)
    {
        if (Other == null) return true;
        return (longValue & Other.longValue) == Other.longValue;
    }
    #endregion

    #region Operators

    /// <summary>
    /// Returns a LongBitMask where any flags from L OR R are true. Equivalent to | or + operators.
    /// </summary>
    public static LongBitMask operator |(LongBitMask L, LongBitMask R) => OR(L, R);
    /// <summary>
    /// Returns a LongBitMask where any flags from L OR R are true. Equivalent to | or + operators.
    /// </summary>
    public static LongBitMask operator +(LongBitMask L, LongBitMask R) => OR(L, R);

    /// <summary>
    /// Returns a LongBitMask where any flags on L AND R are true. 
    /// </summary>
    public static LongBitMask operator &(LongBitMask L, LongBitMask R) => AND(L, R);
    /// <summary>
    /// Returns a LongBitMask where any flags on L AND R are true. 
    /// </summary>
    public static LongBitMask operator *(LongBitMask L, LongBitMask R) => AND(L, R);

    /// <summary>
    /// Returns a LongBitMask where only flags true on one of the two operands, L/R are true. Equivalent to ^ or / operators.
    /// </summary>
    public static LongBitMask operator ^(LongBitMask L, LongBitMask R) => XOR(L, R);
    /// <summary>
    /// Returns a LongBitMask where only flags true on one of the two operands, L/R are true. Equivalent to ^ or / operators.
    /// </summary>
    public static LongBitMask operator /(LongBitMask L, LongBitMask R) => XOR(L, R);



    /// <summary>
    /// Returns a LongBitMask where the right index is added to the left LongBitMask.
    /// </summary>
    public static LongBitMask operator +(LongBitMask L, int R) => ADD(L, R);
    /// <summary>
    /// Returns a LongBitMask where the right indeces are added to the left LongBitMask.
    /// </summary>
    public static LongBitMask operator +(LongBitMask L, int[] R) => ADD(L, R);
    /// <summary>
    /// Returns a LongBitMask where the right indeces are added to the left LongBitMask.
    /// </summary>
    public static LongBitMask operator +(LongBitMask L, List<int> R) => ADD(L, R);

    /// <summary>
    /// Returns a LongBitMask where the right index is removed to the left LongBitMask.
    /// </summary>
    public static LongBitMask operator -(LongBitMask L, int R) => REMOVE(L, R);
    /// <summary>
    /// Returns a LongBitMask where the right indeces are removed to the left LongBitMask.
    /// </summary>
    public static LongBitMask operator -(LongBitMask L, int[] R) => REMOVE(L, R);
    /// <summary>
    /// Returns a LongBitMask where the right indeces are removed to the left LongBitMask.
    /// </summary>
    public static LongBitMask operator -(LongBitMask L, List<int> R) => REMOVE(L, R);

    /// <summary>
    /// Returns a LongBitMask where flags true on R are subtracted from L.
    /// </summary>
    public static LongBitMask operator -(LongBitMask L, LongBitMask R) => XAND(L, R);
    /// <summary>
    /// Returns a LongBitMask that is inverted from the input. 
    /// </summary>
    public static LongBitMask operator ~(LongBitMask L) => INVERT(L);


    /// <summary>
    /// Equality operator. True if both are the same reference or both non-null with equal integer masks.
    /// </summary>
    public static bool operator ==(LongBitMask L, LongBitMask R)
    {
        if (ReferenceEquals(L, R)) return true;
        if (L is null || R is null) return false;
        return L.longValue == R.longValue;
    }

    /// <summary>
    /// Inequality operator.
    /// </summary>
    public static bool operator !=(LongBitMask L, LongBitMask R) => !(L == R);

    /// <summary>
    /// Inclusion operator. True if both are the same reference or both non-null with equal integer masks.
    /// </summary>
    public static bool operator ==(LongBitMask L, int R)
    {
        if (L == null) return false;
        if (R is < 0 or > 63) throw new ArgumentOutOfRangeException("Index outside of 0..63 Range");
        return L[R] == true;
    }

    /// <summary>
    /// Uninclusion operator.
    /// </summary>
    public static bool operator !=(LongBitMask L, int R) => !(L == R);

    #endregion


    /// <summary>
    /// Determines whether the specified object is equal to the current BitwiseEnum.
    /// Accepts BitwiseEnum or int for comparison.
    /// </summary>
    public override bool Equals(object obj)
        => ReferenceEquals(this, obj)
           || (obj is LongBitMask other && longValue == other.longValue)
           || (obj is int i && longValue == i);

    /// <summary>
    /// IEquatable implementation.
    /// </summary>
    public bool Equals(LongBitMask other) => this == other;

    /// <summary>
    /// Hash code based on the integer mask.
    /// </summary>
    public override int GetHashCode() => (int)longValue;

    /// <summary>
    /// String representation of the integer mask.
    /// </summary>
    public override string ToString() => longValue.ToString();

}