using System;
using System.Collections.Generic;
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
            result.Append(char.ToUpper(camelCase[0]));

            for (int i = 1; i < camelCase.Length; i++)
            {
                char c = camelCase[i];
                if ((char.IsUpper(c) || char.IsDigit(c)) && !char.IsWhiteSpace(camelCase[i - 1]) && !char.IsDigit(camelCase[i - 1]))
                    result.Append(' ');
                result.Append(c);
            }

            return result.ToString();
        }
        public static string PascalCaseToDisplay(this string pascalCase)
        {
            if (string.IsNullOrEmpty(pascalCase))
                return string.Empty;

            var result = new System.Text.StringBuilder();
            result.Append(char.ToUpper(pascalCase[0]));

            for (int i = 1; i < pascalCase.Length; i++)
            {
                char c = pascalCase[i];
                if (char.IsUpper(c) && !char.IsWhiteSpace(pascalCase[i - 1]))
                    result.Append(' ');
                result.Append(c);
            }

            return result.ToString();
        }
        public static string DisplayToCamelCase(this string display)
        {
            if (string.IsNullOrEmpty(display))
                return string.Empty;

            var words = display.Split(new[] { ' ', '_' }, StringSplitOptions.RemoveEmptyEntries);
            if (words.Length == 0)
                return string.Empty;

            var result = new System.Text.StringBuilder();
            result.Append(char.ToLower(words[0][0]));
            if (words[0].Length > 1)
                result.Append(words[0].Substring(1));

            for (int i = 1; i < words.Length; i++)
            {
                if (words[i].Length == 0) continue;
                result.Append(char.ToUpper(words[i][0]));
                if (words[i].Length > 1)
                    result.Append(words[i].Substring(1));
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

