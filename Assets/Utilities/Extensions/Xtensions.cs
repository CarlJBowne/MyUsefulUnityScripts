using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;

namespace Utilities.Xtensions
{
    public delegate void Delegate();

    public static class Xtensions_C
    {
        // Boolean helpers
        public static bool Toggle(this ref bool boolean) => boolean = !boolean;
        public static bool IsTrue(this bool boolean) => boolean == true;
        public static bool IsFalse(this bool boolean) => boolean == false;
        public static bool True(this ref bool boolean) => boolean = true;
        public static bool False(this ref bool boolean) => boolean = false;

        public static int Int(this bool boolean) => boolean ? 1 : 0;
        public static bool Bool(this int integral) => integral > 0;

        public static bool NotNull<T>(this T obj, out T store) where T : class
        {
            store = obj as T;
            return store != null;
        }
        public static bool NotNull<T>(this T obj) where T : class => obj != null;

        /// <summary>
        /// Clones an object aat the deepest Binary. <br/>
        /// Requires the type to be <see cref="System.SerializableAttribute"/>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="input"></param>
        /// <returns></returns>
        public static T DeepClone<T>(this T input)
        {
            using var ms = new MemoryStream();
            var formatter = new BinaryFormatter();
            formatter.Serialize(ms, input);
            ms.Position = 0;
            return (T)formatter.Deserialize(ms);
        }
    }

    public static class Xtensions_String
    {
        // String helpers
        public static string Join(this List<string> list, string buffer = null) => string.Join(buffer, list);
        public static string CamelCaseToDisplay(this string camelCase)
        {
            if (string.IsNullOrEmpty(camelCase))
                return string.Empty;

            var result = new System.Text.StringBuilder();

            if(char.IsLetter(result[0])) result[0] = char.ToUpper(result[0]);
            for (int i = 0; i < result.Length; i++)
                if (char.IsUpper(result[i])) 
                    result.Insert(i - 1, ' ');
            
            return result.ToString();
        }
        public static string PascalCaseToDisplay(this string pascalCase)
        {
            if (string.IsNullOrEmpty(pascalCase))
                return string.Empty;

            var result = new System.Text.StringBuilder();

            for (int i = 0; i < result.Length; i++)
                if (char.IsUpper(result[i]))
                    result.Insert(i - 1, ' ');

            return result.ToString();
        }
        public static string DisplayToCamelCase(this string display)
        {
            if (string.IsNullOrEmpty(display))
                return string.Empty;

            var result = new System.Text.StringBuilder(display);
            if (char.IsUpper(result[0])) result[0] = char.ToLower(result[0]);

            for (int i = 0; i < result.Length; i++)
            {
                if (result[0] == ' ')
                {
                    result.Remove(0, 1);
                    i--;
                }
            }
            return result.ToString();
        }
        /// <summary>
        /// Adds the surrounding <>k__BackingField to a property name, to reference the backing field of an auto-property.
        /// </summary>
        /// <param name="propertyName">the input property name. Generally advised to use a "nameof()"</param>
        /// <returns>the identifier of the backing field for use in a FindProperty method.</returns>
        public static string BackingField(this string propertyName) => $"<{propertyName}>k__BackingField";
    }

    public static class Xtensions_Collections
    {
        public static T Random<T>(this T[] array) => array[UnityEngine.Random.Range(0, array.Length)];
        public static T Random<T>(this List<T> array) => array[UnityEngine.Random.Range(0, array.Count)];
        public static void RemoveAtLast<T>(this List<T> array, int i = 1)
        {
            if (array == null || array.Count == 0) return;
            int index = array.Count - i;
            if (index >= 0 && index < array.Count) array.RemoveAt(index);
        }
        public static void ClearNull<T>(this List<T> list) where T : class
        {
            if (list == null) return;
            for (int i = list.Count - 1; i >= 0; i--)
                if (list[i] == null)
                    list.RemoveAt(i);
        }


    }

    public static class Xtensions_Color
    {
        public static Color Lighter(this Color color, float value, bool clamp = true)
        {
            color.MakeLighter(value, clamp);
            return color;
        }
        public static void MakeLighter(this ref Color color, float value, bool clamp = true)
        {
            color.r += value;
            color.g += value;
            color.b += value;
            if (clamp)
            {
                if (color.r < 0f) color.r = 0f; if (color.r > 1f) color.r = 1f;
                if (color.g < 0f) color.g = 0f; if (color.g > 1f) color.g = 1f;
                if (color.b < 0f) color.b = 0f; if (color.b > 1f) color.b = 1f;
            }
        }

        public static void Set(this ref Color color, float? r = null, float? g = null, float? b = null, float? a = null)
        {
            if (r != null) color.r = r.Value;
            if (g != null) color.g = g.Value;
            if (b != null) color.b = b.Value;
            if (a != null) color.a = a.Value;
        }
        public static Color Changed(this Color color, float? r = null, float? g = null, float? b = null, float? a = null)
        {
            color.Set(r, g, b, a);
            return color;
        }

        public static void Shift(this ref Color color, float? r = null, float? g = null, float? b = null, float? a = null, bool clamp = true)
        {
            if (r != null)
            {
                color.r += r.Value;
                if (clamp && color.r < 0f) color.r = 0f;
                if (clamp && color.r > 1f) color.r = 1f;
            }
            if (g != null)
            {
                color.g += g.Value;
                if (clamp && color.g < 0f) color.g = 0f;
                if (clamp && color.g > 1f) color.g = 1f;
            }
            if (b != null)
            {
                color.b += b.Value;
                if (clamp && color.b < 0f) color.b = 0f;
                if (clamp && color.b > 1f) color.b = 1f;
            }
            if (a != null)
            {
                color.a += a.Value;
                if (clamp && color.a < 0f) color.a = 0f;
                if (clamp && color.a > 1f) color.a = 1f;
            }
        }

        public static Color Shifted(this Color color, float? r = null, float? g = null, float? b = null, float? a = null, bool clamp = true)
        {
            color.Shift(r, g, b, a, clamp);
            return color;
        }
    }
}

