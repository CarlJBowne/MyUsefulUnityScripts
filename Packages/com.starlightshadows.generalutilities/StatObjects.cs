using System;
using System.Collections.Generic;
using System.Text;
using SLS.GeneralUtilities.EventTickets;
using UnityEngine;

namespace SLS.GeneralUtilities.StatObjects
{
    /// <summary>
    /// A serializable wrapper around a value of type <typeparamref name="T"/> that exposes change events
    /// and utilities for subscribing to value changes.
    /// </summary>
    /// <typeparam name="T">A value type that implements <see cref="IEquatable{T}"/>.</typeparam>
    [System.Serializable]
    public class StatObject<T> where T : struct, IEquatable<T>
    {
        /// <summary>
        /// Backing field for the <see cref="Value"/> property. Serialized by Unity.
        /// </summary>
        [SerializeField] protected T _value;

        /// <summary>
        /// Gets or sets the current value. Will invoke Value-Changed Callbacks when the resulting value has changed.
        /// </summary>
        public virtual T Value
        {
            get => _value;
            set
            {
                T oldValue = _value;
                _value = value;
                if (!_value.Equals(oldValue)) CallOnValueChanged(_value);
            }
        }

        /// <summary>
        /// Sets the <see cref="Value"/>. Provided so it can be fed into event subscription structures.
        /// </summary>
        /// <param name="value">The new value to set.</param>
        public virtual void SetValue(T value) => Value = value;

        /// <summary>
        /// Traditional .NET event raised when the value changes.
        /// </summary>
        public event Action<T> OnValueChanged;

        /// <summary>
        /// UltEvents-compatible event that is invoked when the value changes (serialized by Unity).
        /// </summary>
        [SerializeField] protected UltEvents.UltEvent<T> OnValueChangedUlt;

        /// <summary>
        /// Invokes both <see cref="OnValueChanged"/> and <see cref="OnValueChangedUlt"/> with the provided value.
        /// </summary>
        /// <param name="v">The value passed to subscribers.</param>
        protected void CallOnValueChanged(T v)
        {
            OnValueChanged?.Invoke(v);
            OnValueChangedUlt?.Invoke(v);
        }

        /// <summary>
        /// Implicit conversion to the wrapped value type for convenience.
        /// </summary>
        /// <param name="s">The stat object to convert.</param>
        public static implicit operator T(StatObject<T> s) => s is null ? default : s.Value;

        /// <summary>
        /// Equality operator comparing the stat object's value to a raw value.
        /// </summary>
        public static bool operator ==(StatObject<T> l, T r) => l is not null && l.Value.Equals(r);

        /// <summary>
        /// Inequality operator comparing the stat object's value to a raw value.
        /// </summary>
        public static bool operator !=(StatObject<T> l, T r) => !(l == r);

        /// <summary>
        /// Determines whether the current object is equal to another object or value of type <typeparamref name="T"/>.
        /// </summary>
        public override bool Equals(object obj) => obj is T objT
            ? Value.Equals(objT)
            : base.Equals(obj);

        /// <summary>
        /// Returns the hash code of the wrapped value.
        /// </summary>
        public override int GetHashCode() => _value.GetHashCode();

        /// <summary>
        /// Returns a human-readable representation of the stat and the number of listeners attached.
        /// </summary>
        public override string ToString() => $"Value:{_value}, {OnValueChanged?.GetInvocationList().Length ?? 0} Listeners";

        /// <summary>
        /// Pseudo-assignment operator that sets the stat object's value to the provided value and returns the object.
        /// </summary>
        public static StatObject<T> operator &(StatObject<T> l, T r)
        {
            l.Value = r;
            return l;
        }

        /// <summary>
        /// Pseudo-assignment operator that copies the value from another <see cref="StatObject{T}"/>.
        /// </summary>
        public static StatObject<T> operator &(StatObject<T> l, StatObject<T> r)
        {
            l.Value = r.Value;
            return l;
        }

        /// <summary>
        /// Creates an <see cref="EventTicket"/> subscribing a method with parameter <see cref="T"/> to <see cref="OnValueChanged"/>.
        /// </summary>
        /// <param name="subscriber">Handler to invoke when the value changes.</param>
        /// <param name="subscribeNow">If true, subscribes immediately.</param>
        /// <returns>An <see cref="EventTicket"/> representing the subscription.</returns>
        public EventTicket Subscribe(Action<T> subscriber, bool subscribeNow = true) =>
            OnValueChanged.Subscribe(subscriber, subscribeNow);
    }
    /// <summary>
    /// A serializable wrapper around a value of type <typeparamref name="T"/> that exposes change events
    /// and utilities for subscribing to value changes. Includes Maximum and Minimum clamps
    /// </summary>
    /// <typeparam name="T">A value type that implements <see cref="IEquatable{T}"/> and <see cref="IComparable{T}"/>.</typeparam>
    public class StatObjectClamped<T> : StatObject<T> where T : struct, IEquatable<T>, IComparable<T>
    {
        /// <summary>
        /// Backing field for the <see cref="Max"/> property. Serialized by Unity.
        /// </summary>
        [SerializeField] protected T _max;
        /// <summary>
        /// Backing field for the <see cref="Min"/> property. Serialized by Unity.
        /// </summary>
        [SerializeField] protected T _min;
        /// <summary>
        /// Gets or sets the current value. Will invoke Value-Changed Callbacks when the resulting value has changed.
        /// Will also clamp itself between <see cref="Min"/> and <see cref="Max"/>.
        /// </summary>
        public override T Value
        {
            get => _value;
            set
            {
                T oldValue = _value;
                _value = value;
                if (_value.CompareTo(_min) < 0) _value = _min;
                if (_value.CompareTo(_max) > 0) _value = _max;
                if (!_value.Equals(oldValue)) CallOnValueChanged(_value);
            }
        }

        /// <summary>
        /// Traditional .NET event raised when the Maximum changes.
        /// </summary>
        public event Action<T> OnMaxChanged; [SerializeField] protected UltEvents.UltEvent<T> OnMaxChangedUlt;
        /// <summary>
        /// Invokes both <see cref="OnMaxChanged"/> and <see cref="OnMaxChangedUlt"/> with the provided value.
        /// </summary>
        /// <param name="v">The value passed to subscribers.</param>
        protected void CallOnMaxChanged(T v)
        {
            OnMaxChanged?.Invoke(v);
            OnMaxChangedUlt?.Invoke(v);
        }
        /// <summary>
        /// Traditional .NET event raised when the Minimum changes.
        /// </summary>
        public event Action<T> OnMinChanged; [SerializeField] protected UltEvents.UltEvent<T> OnMinChangedUlt;
        /// <summary>
        /// Invokes both <see cref="OnMinChanged"/> and <see cref="OnMinChangedUlt"/> with the provided value.
        /// </summary>
        /// <param name="v">The value passed to subscribers.</param>
        protected void CallOnMinChanged(T v)
        {
            OnMinChanged?.Invoke(v);
            OnMinChangedUlt?.Invoke(v);
        }

        /// <summary>
        /// Gets or sets the maximum value. Will invoke Max-Changed Callbacks when the resulting value has changed and trigger recalculation of the current value if it exceeds the new maximum.
        /// </summary>
        public T Max
        {
            get => _max;
            set
            {
                T oldValue = _max;
                _max = value;
                if (!_max.Equals(oldValue)) CallOnMaxChanged(_max);
                Value = Value;
            }
        }
        /// <summary>
        /// Sets the Maximum. Provided so it can be fed into event subscription structures.
        /// </summary>
        /// <param name="value"></param>
        public virtual void SetMax(T value) => Max = value;
        /// <summary>
        /// Gets or sets the minimum value. Will invoke Min-Changed Callbacks when the resulting value has changed and trigger recalculation of the current value if it is below the new minimum.
        /// </summary>
        public T Min
        {
            get => _min;
            set
            {
                T oldValue = _min;
                _min = value;
                if (!_min.Equals(oldValue)) CallOnMinChanged(_min);
                Value = Value;
            }
        }
        /// <summary>
        /// Sets the Minimum. Provided so it can be fed into event subscription structures.
        /// </summary>
        /// <param name="value"></param>
        public virtual void SetMin(T value) => Min = value;

        /// <summary>
        /// Instantly sets the current <see cref="Value"/> to the Maximum. Calling all relevant callbacks if the value has changed.
        /// </summary>
        public virtual void InstantFill() => Value = Max;
        /// <summary>
        /// Instantly sets the current <see cref="Value"/> to the Minimum. Calling all relevant callbacks if the value has changed.
        /// </summary>
        public virtual void InstantDeplete() => Value = Min;
        /// <summary>
        /// Sets the Maximum value and changes the current <see cref="Value"/> to the Maximum. Calling all relevant callbacks if the value has changed, but not calling multiple calculations.
        /// </summary>
        /// <param name="value"></param>
        public virtual void SetMaxAndFill(T value)
        {
            T preMax = Max;
            T preVal = Value;

            _max = value;
            _value = value;

            if (!preMax.Equals(_max)) CallOnMaxChanged(value);
            if (!preVal.Equals(_value)) CallOnValueChanged(value);
        }

        /// <summary>
        /// Creates an <see cref="EventTicket"/> subscribing a method with parameter <see cref="T"/> to <see cref="OnMaxChanged"/>.
        /// </summary>
        /// <param name="subscriber">Handler to invoke when the value changes.</param>
        /// <param name="subscribeNow">If true, subscribes immediately.</param>
        /// <returns>An <see cref="EventTicket"/> representing the subscription.</returns>
        public EventTicket SubscribeMax(Action<T> subscriber, bool subscribeNow = true) =>
            OnMaxChanged.Subscribe(subscriber, subscribeNow);
        /// <summary>
        /// Creates an <see cref="EventTicket"/> subscribing a method with parameter <see cref="T"/> to <see cref="OnMinChanged"/>.
        /// </summary>
        /// <param name="subscriber">Handler to invoke when the value changes.</param>
        /// <param name="subscribeNow">If true, subscribes immediately.</param>
        /// <returns>An <see cref="EventTicket"/> representing the subscription.</returns>
        public EventTicket SubscribeMin(Action<T> subscriber, bool subscribeNow = true) =>
            OnMinChanged.Subscribe(subscriber, subscribeNow);

    }
    /// <summary>
    /// A serializable wrapper for an integer value that exposes change events and utilities for subscribing to value changes. 
    /// </summary>
    public class IntStat : StatObject<int>
    {
        /// <summary> Adds the int on the right to the value of the <see cref="IntStat"/> on the left. </summary>
        public static IntStat operator +(IntStat l, int r)
        {
            l.Value += r;
            return l;
        }
        /// <summary> Subtracts the int on the right from the value of the <see cref="IntStat"/> on the left. </summary>
        public static IntStat operator -(IntStat l, int r)
        {
            l.Value -= r;
            return l;
        }
        /// <summary> Multiplies the value of the <see cref="IntStat"/> on the left by the int on the right </summary>
        public static IntStat operator *(IntStat l, int r)
        {
            l.Value *= r;
            return l;
        }
        /// <summary> Divides the value of the <see cref="IntStat"/> on the left by the int on the right </summary>
        public static IntStat operator /(IntStat l, int r)
        {
            l.Value /= r;
            return l;
        }
        /// <summary> Returns the remainder the value of the <see cref="IntStat"/> on the left by the int on the right </summary>
        public static IntStat operator %(IntStat l, int r)
        {
            l.Value %= r;
            return l;
        }

        /// <summary>
        /// Psudeo Assignment operator. Assigns the value on the right to the value.
        /// </summary>
        public static IntStat operator &(IntStat l, int r)
        {
            l.Value = r;
            return l;
        }
        /// <summary>
        /// Psudeo Assignment operator. Assigns the object on the right's value to the value.
        /// </summary>
        public static IntStat operator &(IntStat l, IntStat r)
        {
            l.Value = r.Value;
            return l;
        }
    }
    /// <summary>
    /// A serializable wrapper for an integer value that exposes change events and utilities for subscribing to value changes. Includes Maximum and Minimum clamps.
    /// </summary>
    public class IntStatClamped : StatObjectClamped<int>
    {
        /// <summary> Adds the int on the right to the value of the <see cref="IntStat"/> on the left. </summary>
        public static IntStatClamped operator +(IntStatClamped l, int r)
        {
            l.Value += r;
            return l;
        }
        /// <summary> Subtracts the int on the right from the value of the <see cref="IntStat"/> on the left. </summary>
        public static IntStatClamped operator -(IntStatClamped l, int r)
        {
            l.Value -= r;
            return l;
        }
        /// <summary> Multiplies the value of the <see cref="IntStat"/> on the left by the int on the right </summary>
        public static IntStatClamped operator *(IntStatClamped l, int r)
        {
            l.Value *= r;
            return l;
        }
        /// <summary> Divides the value of the <see cref="IntStat"/> on the left by the int on the right </summary>
        public static IntStatClamped operator /(IntStatClamped l, int r)
        {
            l.Value /= r;
            return l;
        }
        /// <summary> Returns the remainder the value of the <see cref="IntStat"/> on the left by the int on the right </summary>
        public static IntStatClamped operator %(IntStatClamped l, int r)
        {
            l.Value %= r;
            return l;
        }

        /// <summary>
        /// Psudeo Assignment operator. Assigns the value on the right to the value.
        /// </summary>
        public static IntStatClamped operator &(IntStatClamped l, int r)
        {
            l.Value = r;
            return l;
        }
        /// <summary>
        /// Psudeo Assignment operator. Assigns the object on the right's value to the value.
        /// </summary>
        public static IntStatClamped operator &(IntStatClamped l, IntStat r)
        {
            l.Value = r.Value;
            return l;
        }
    }
    /// <summary>
    /// A serializable wrapper for a float value that exposes change events and utilities for subscribing to value changes. 
    /// </summary>
    public class FloatStat : StatObject<float>
    {
        /// <summary> Adds the float on the right to the value of the <see cref="FloatStat"/> on the left. </summary>
        public static FloatStat operator +(FloatStat l, float r)
        {
            l.Value += r;
            return l;
        }
        /// <summary> Subtracts the float on the right from the value of the <see cref="IntStat"/> on the left. </summary>
        public static FloatStat operator -(FloatStat l, float r)
        {
            l.Value -= r;
            return l;
        }
        /// <summary> Multiplies the value of the <see cref="IntStat"/> on the left by the float on the right </summary>
        public static FloatStat operator *(FloatStat l, float r)
        {
            l.Value *= r;
            return l;
        }
        /// <summary> Divides the value of the <see cref="IntStat"/> on the left by the float on the right </summary>
        public static FloatStat operator /(FloatStat l, float r)
        {
            l.Value /= r;
            return l;
        }
        /// <summary> Returns the remainder the value of the <see cref="IntStat"/> on the left by the float on the right </summary>
        public static FloatStat operator %(FloatStat l, float r)
        {
            l.Value %= r;
            return l;
        }

        /// <summary>
        /// Psudeo Assignment operator. Assigns the value on the right to the value.
        /// </summary>
        public static FloatStat operator &(FloatStat l, float r)
        {
            l.Value = r;
            return l;
        }
        /// <summary>
        /// Psudeo Assignment operator. Assigns the object on the right's value to the value.
        /// </summary>
        public static FloatStat operator &(FloatStat l, FloatStat r)
        {
            l.Value = r.Value;
            return l;
        }
    }
    /// <summary>
    /// A serializable wrapper for a float value that exposes change events and utilities for subscribing to value changes. Includes Maximum and Minimum clamps.
    /// </summary>
    public class FloatStatClamped : StatObjectClamped<float>
    {
        /// <summary> Adds the float on the right to the value of the <see cref="FloatStat"/> on the left. </summary>
        public static FloatStatClamped operator +(FloatStatClamped l, float r)
        {
            l.Value += r;
            return l;
        }
        /// <summary> Subtracts the float on the right from the value of the <see cref="IntStat"/> on the left. </summary>
        public static FloatStatClamped operator -(FloatStatClamped l, float r)
        {
            l.Value -= r;
            return l;
        }
        /// <summary> Multiplies the value of the <see cref="IntStat"/> on the left by the float on the right </summary>
        public static FloatStatClamped operator *(FloatStatClamped l, float r)
        {
            l.Value *= r;
            return l;
        }
        /// <summary> Divides the value of the <see cref="IntStat"/> on the left by the float on the right </summary>
        public static FloatStatClamped operator /(FloatStatClamped l, float r)
        {
            l.Value /= r;
            return l;
        }
        /// <summary> Returns the remainder the value of the <see cref="IntStat"/> on the left by the float on the right </summary>
        public static FloatStatClamped operator %(FloatStatClamped l, float r)
        {
            l.Value %= r;
            return l;
        }

        /// <summary>
        /// Psudeo Assignment operator. Assigns the value on the right to the value.
        /// </summary>
        public static FloatStatClamped operator &(FloatStatClamped l, float r)
        {
            l.Value = r;
            return l;
        }
        /// <summary>
        /// Psudeo Assignment operator. Assigns the object on the right's value to the value.
        /// </summary>
        public static FloatStatClamped operator &(FloatStatClamped l, FloatStatClamped r)
        {
            l.Value = r.Value;
            return l;
        }
    }

}
