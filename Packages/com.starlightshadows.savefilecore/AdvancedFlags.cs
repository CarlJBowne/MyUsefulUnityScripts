using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SLS.GeneralUtilities.Events;
using SLS.ListUtilities;
using static SLS.SaveFileCore.Flag;





#if UNITY_EDITOR
using UnityEditor;
#endif

#if SLS_POLYMORPHS
namespace SLS.SaveFileCore
{
    /// <summary>Base class for typed, serializable values that can be stored in a flag collection.</summary>
    [Serializable]
    public abstract class Flag : Polymorph
    {
        /// <summary>Gets or sets the flag's value as an object.</summary>
        public abstract object valueObject { get; set; }
        /// <summary>Gets the runtime type represented by this flag's value.</summary>
        public abstract Type ValueType { get; }

        /// <summary>Loads this flag's value from a JSON token when the token has a supported shape.</summary>
        /// <param name="input">JSON token containing the value.</param>
        public abstract void LoadFromJson(JToken input);
        /// <summary>Serializes this flag's value to a JSON token.</summary>
        /// <returns>A token representing the current value.</returns>
        public abstract JToken SaveToJson();
        /// <summary>Copies the value from a compatible flag into this instance.</summary>
        /// <param name="source">Flag whose value is copied.</param>
        /// <returns>This flag after the copy, or unchanged when the source is incompatible.</returns>
        public abstract Flag Clone(Flag source);

        /// <summary>Raises the value-changed notification after a value update.</summary>
        protected abstract void CallValueChangedCallback();

        /// <summary>Converts a flag to its JSON representation.</summary>
        /// <param name="source">Flag to serialize.</param>
        /// <returns>The JSON token produced by the flag.</returns>
        public static implicit operator JToken(Flag source) => source.SaveToJson();
        /// <summary>Sets a flag's value through an assignment-like operator and returns the same flag.</summary>
        /// <param name="flag">Flag to update.</param>
        /// <param name="value">Value to assign.</param>
        /// <returns>The updated flag.</returns>
        public static Flag operator &(Flag flag, object value)
        {
            flag.valueObject = value;
            return flag;
        }


        /// <summary>
        /// Base implementation for flags whose values have a specific type. <br/>
        /// Current supported types are <see cref="bool"/>, <see cref="int"/>, <see cref="float"/>, <see cref="UnityEngine.Vector3"/>, <see cref="string"/>, and <see cref="char"/>.
        /// </summary>
        /// <typeparam name="T">Type of the value stored by this flag.</typeparam>
        public abstract class Generic<T> : Flag
        {
            /// <summary>Gets or sets the typed value and notifies subscribers when it changes.</summary>
            public T Value
            {
                get => value;
                set
                {
                    if ((this.value == null && value == null) || (this.value != null && this.value.Equals(value))) return;
                    this.value = value;
                    CallValueChangedCallback();
                }
            }

            /// <summary>Gets or sets the value through the base flag's object-typed interface.</summary>
            public override object valueObject
            {
                get => value;
                set
                {
                    if (value.GetType() != typeof(T)) return;
                    this.value = (T)value;
                    CallValueChangedCallback();
                }
            }

            /// <summary>Serialized backing field for the current typed value.</summary>
            [SerializeField] protected T value;

            /// <summary>Gets the type of the value stored by this flag.</summary>
            public override Type ValueType => typeof(T);

            /// <summary>Raised after the flag's value changes.</summary>
            public event Action<T> OnValueChanged;

            /// <summary>Subscribes a callback to value changes.</summary>
            /// <param name="callback">Callback invoked with the new value.</param>
            /// <param name="now">Whether to activate the subscription immediately.</param>
            /// <returns>A ticket that controls the subscription lifetime.</returns>
            public EventTicket Subscribe(Action<T> callback, bool now = true)
                => new(() => OnValueChanged += callback, () => OnValueChanged -= callback, now);

            /// <summary>Notifies all subscribers with the current value.</summary>
            protected override void CallValueChangedCallback() => OnValueChanged?.Invoke(Value);

            /// <summary>Copies a compatible flag's value into this instance.</summary>
            /// <param name="source">Flag to copy.</param>
            /// <returns>This flag, unchanged if the source is null or has a different value type.</returns>
            public override Flag Clone(Flag source)
            {
                if (source == null) return this;
                if (source.valueObject.GetType() != typeof(T)) return this;
                valueObject = source.valueObject;
                return this;
            }

            /// <summary>Copies another flag's typed value into this instance.</summary>
            /// <param name="source">Flag to copy.</param>
            /// <returns>This flag, unchanged if the source is null.</returns>
            public Generic<T> Clone(Generic<T> source)
            {
                if (source == null) return this;
                Value = source.Value;
                return this;
            }

            /// <summary>Converts a generic flag to its typed value.</summary>
            /// <param name="input">Flag to read.</param>
            /// <returns>The flag's current value.</returns>
            public static implicit operator T(Generic<T> input) => input.Value;

            /// <summary>Sets the flag's typed value and returns the same flag.</summary>
            /// <param name="flag">Flag to update.</param>
            /// <param name="value">Value to assign.</param>
            /// <returns>The updated flag.</returns>
            public static Generic<T> operator &(Generic<T> flag, T value)
            {
                flag.Value = value;
                return flag;
            }
        }

        /// <summary>A flag storing a Boolean value.</summary>
        [Serializable]
        public class Bool : Generic<bool>
        {
            /// <summary>Loads a Boolean value when the token is a JSON Boolean.</summary>
            /// <param name="input">Token containing the value.</param>
            public override void LoadFromJson(JToken input)
            {
                if (input == null || input.Type != JTokenType.Boolean) return;
                Value = (bool)input;
            }

            /// <summary>Serializes the Boolean value as a JSON token.</summary>
            /// <returns>A JSON Boolean token.</returns>
            public override JToken SaveToJson() => (JToken)Value;
        }

        /// <summary>A flag storing a 32-bit integer value.</summary>
        [Serializable]
        public class Int : Generic<int>
        {
            /// <summary>Loads an integer value when the token is a JSON integer.</summary>
            /// <param name="input">Token containing the value.</param>
            public override void LoadFromJson(JToken input)
            {
                if (input == null || input.Type != JTokenType.Integer) return;
                Value = (int)input;
            }

            /// <summary>Serializes the integer value as a JSON token.</summary>
            /// <returns>A JSON integer token.</returns>
            public override JToken SaveToJson() => (JToken)Value;
        }

        /// <summary>A flag storing a single-precision floating-point value.</summary>
        [Serializable]
        public class Float : Generic<float>
        {
            /// <summary>Loads a floating-point value when the token is a JSON floating-point number.</summary>
            /// <param name="input">Token containing the value.</param>
            public override void LoadFromJson(JToken input)
            {
                if (input == null || input.Type != JTokenType.Float) return;
                Value = (float)input;
            }

            /// <summary>Serializes the floating-point value as a JSON token.</summary>
            /// <returns>A JSON floating-point token.</returns>
            public override JToken SaveToJson() => (JToken)Value;
        }

        /// <summary>A flag storing a Unity <see cref="UnityEngine.Vector3"/> as three JSON array values.</summary>
        [Serializable]
        public class Vector3 : Generic<UnityEngine.Vector3>
        {
            /// <summary>Loads a three-element JSON array into the vector value.</summary>
            /// <param name="input">Array containing the vector's x, y, and z components.</param>
            public override void LoadFromJson(JToken input)
            {
                if (input == null || input is not JArray array || array.Count != 3) return;
                Value = new((float)array[0], (float)array[1], (float)array[2]);
            }

            /// <summary>Serializes the vector as an array of its x, y, and z components.</summary>
            /// <returns>A three-element JSON array.</returns>
            public override JToken SaveToJson() => new JArray { value.x, value.y, value.z };
        }

        /// <summary>A flag storing a string value.</summary>
        [Serializable]
        public class String : Generic<string>
        {
            /// <summary>Loads a string when the token is a JSON string.</summary>
            /// <param name="input">Token containing the value.</param>
            public override void LoadFromJson(JToken input)
            {
                if (input == null || input.Type != JTokenType.String) return;
                Value = (string)input;
            }

            /// <summary>Serializes the string value as a JSON token.</summary>
            /// <returns>A JSON string token.</returns>
            public override JToken SaveToJson() => (JToken)Value;
        }

        /// <summary>A flag storing a character as its integer code point value.</summary>
        [Serializable]
        public class Char : Generic<char>
        {
            /// <summary>Loads a character from a JSON integer.</summary>
            /// <param name="input">Token containing the integer character value.</param>
            public override void LoadFromJson(JToken input)
            {
                if (input == null || input.Type != JTokenType.Integer) return;
                Value = (char)(int)input;
            }

            /// <summary>Serializes the character as its integer code point value.</summary>
            /// <returns>A JSON integer token.</returns>
            public override JToken SaveToJson() => (JToken)(int)Value;
        }

        /// <summary>A named collection of typed flags with JSON serialization and value-change subscriptions.</summary>
        [Serializable]
        public class Collection : Dictionary<Flag>
        {
            /// <summary>Gets a named flag when it exists and has the requested value type.</summary>
            /// <typeparam name="T">Expected flag value type.</typeparam>
            /// <param name="name">Flag name.</param>
            /// <returns>The matching flag, or <see langword="null"/> if it is missing or has another type.</returns>
            public Generic<T> Get<T>(string name)
            {
                if (TryGet(name, out Flag existing, true) && existing is Generic<T> f) return f;
                return null;
            }

            /// <summary>Attempts to retrieve a named flag of the requested value type.</summary>
            /// <typeparam name="T">Expected flag value type.</typeparam>
            /// <param name="name">Flag name.</param>
            /// <param name="value">Receives the matching flag, or the default value when no match exists.</param>
            /// <returns><see langword="true"/> when the named flag exists and has the requested type.</returns>
            public bool TryGet<T>(string name, out Generic<T> value)
            {
                value = default;
                if (!ContainsKey(name)) return false;
                if (this[name].ValueType != typeof(T)) return false;
                if (this[name] is not Generic<T> gen) return false;

                value = gen;
                return true;
            }

            /// <summary>Subscribes to value changes for a named flag of the requested type.</summary>
            /// <typeparam name="T">Expected flag value type.</typeparam>
            /// <param name="name">Flag name.</param>
            /// <param name="callback">Callback invoked with the new flag value.</param>
            /// <param name="now">Whether to activate the subscription immediately.</param>
            /// <returns>A subscription ticket, or <see langword="null"/> if no matching flag exists.</returns>
            public EventTicket Subscribe<T>(string name, Action<T> callback, bool now = true) =>
                TryGet(name, out Generic<T> res) ? res.Subscribe(callback, now) : null;

            /// <summary>Gets the names of all flags whose value type is <typeparamref name="T"/>.</summary>
            /// <typeparam name="T">Value type to match.</typeparam>
            /// <returns>Names of flags with the requested value type.</returns>
            public string[] AllNamesOfType<T>() => AllNamesOfType(typeof(T));

            /// <summary>Gets the names of all flags whose value type matches the specified type.</summary>
            /// <param name="type">Value type to match.</param>
            /// <returns>Names of flags with the specified value type.</returns>
            public string[] AllNamesOfType(Type type)
            {
                List<string> result = new();
                for (int i = 0; i < Count; i++)
                    if (Values[i].ValueType == type)
                        result.Add(Keys[i]);
                return result.ToArray();
            }

            /// <summary>Creates and stores a flag with a supported value type under the specified name.</summary>
            /// <typeparam name="T">Value type for the new flag. Supported types are <see cref="bool"/>, <see cref="int"/>, <see cref="float"/>, <see cref="UnityEngine.Vector3"/>, <see cref="string"/>, and <see cref="char"/>.</typeparam>
            /// <param name="name">Name to assign to the new flag.</param>
            /// <returns>The new typed flag.</returns>
            /// <exception cref="ArgumentException"><typeparamref name="T"/> is not a supported flag value type.</exception>
            public Generic<T> AddNew<T>(string name)
            {
                Flag newFlag = typeof(T) switch
                {
                    Type t when t == typeof(bool) => new Bool(),
                    Type t when t == typeof(int) => new Int(),
                    Type t when t == typeof(float) => new Float(),
                    Type t when t == typeof(UnityEngine.Vector3) => new Vector3(),
                    Type t when t == typeof(string) => new String(),
                    Type t when t == typeof(char) => new Char(),
                    _ => throw new ArgumentException($"Type {typeof(T)} is not a valid Flag type.")
                };
                this[name] = newFlag;
                return newFlag as Generic<T>;
            }

            /// <summary>Creates and stores a flag of the specified supported value type under the specified name.</summary>
            /// <param name="name">Name to assign to the new flag.</param>
            /// <param name="type">Supported value type for the new flag.</param>
            /// <returns>The new flag.</returns>
            /// <exception cref="ArgumentException"><paramref name="type"/> is not a supported flag value type.</exception>
            public Flag AddNew(string name, Type type)
            {
                Flag newFlag = type switch
                {
                    Type t when t == typeof(bool) => new Bool(),
                    Type t when t == typeof(int) => new Int(),
                    Type t when t == typeof(float) => new Float(),
                    Type t when t == typeof(UnityEngine.Vector3) => new Vector3(),
                    Type t when t == typeof(string) => new String(),
                    Type t when t == typeof(char) => new Char(),
                    _ => throw new ArgumentException($"Type {type} is not a valid Flag type.")
                };
                this[name] = newFlag;
                return newFlag;
            }

            /// <summary>Copies flags from another collection according to the selected clone operation.</summary>
            /// <param name="source">Collection to copy; a null source leaves this collection unchanged.</param>
            /// <param name="op">Operation controlling how entries are copied or replaced.</param>
            public void Clone(Collection source, DictionaryCloneOp op = DictionaryCloneOp.Transfer)
            {
                if (source == null) return;
                if (Count == 0) op = DictionaryCloneOp.TransferAndAdd;
                if (op is DictionaryCloneOp.ReplaceEntirely) Clear();
                for (int i = 0; i < source.Count; i++)
                {
                    if (!ContainsHash(source.Hashes[i]) && op is not DictionaryCloneOp.Transfer)
                        this.AddNew(source.Keys[i], source.Values[i].ValueType);
                    this.Add(source.Keys[i], Activator.CreateInstance(source.Values[i].GetType()) as Flag);
                    this[source.Keys[i]].Clone(source.Values[i]);
                }
            }

            /// <summary>Loads values for names already present in this collection from a JSON object.</summary>
            /// <param name="list">Object whose properties contain flag values.</param>
            public void LoadFromJson(JObject list)
            {
                if (!list.HasValues) return;
                foreach (JProperty prop in list.Properties())
                    if (ContainsKey(prop.Name))
                        this[prop.Name].LoadFromJson(prop.Value);
            }

            /// <summary>Serializes all named flags to a JSON object.</summary>
            /// <returns>An object with one property per flag.</returns>
            public JObject SaveToJson()
            {
                JObject result = new();
                for (int i = 0; i < Count; i++)
                    result.Add(new JProperty(Keys[i], Values[i].SaveToJson()));
                return result;
            }

            /// <summary>Calculates the fraction of flags whose values differ from their type's default.</summary>
            /// <param name="mult">Multiplier applied to the resulting fraction.</param>
            /// <returns>The fraction of non-default flags, multiplied by <paramref name="mult"/>; zero for an empty collection.</returns>
            public float PercentageIsntDefault(float mult = 1f)
            {
                if (Count == 0) return 0;
                int notDefault = 0;
                for (int i = 0; i < Count; i++)
                    if (Values[i].valueObject != null && !Values[i].valueObject.Equals(default))
                        notDefault++;
                return (float)notDefault / (float)Count * mult;
            }
        }

    }

}

#endif