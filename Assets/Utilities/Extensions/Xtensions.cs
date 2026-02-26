using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public delegate void Delegate();

public static class XtensionsBasic
{
    // Boolean helpers
    public static bool Toggle(this ref bool boolean) => boolean = !boolean;
    public static bool IsTrue(this bool boolean) => boolean == true;
    public static bool IsFalse(this bool boolean) => boolean == false;
    public static bool True(this ref bool boolean) => boolean = true;
    public static bool False(this ref bool boolean) => boolean = false;

    public static int Int(this bool boolean) => boolean ? 1 : 0;
    public static bool Bool(this int integral) => integral > 0;

    // Color helpers - corrected channel assignments
    public static Color SetRed(this ref Color color, float set) => color = new Color(set, color.g, color.b, color.a);
    public static Color ChangeRed(this ref Color color, float change) => color = new Color(color.r + change, color.g, color.b, color.a);
    public static Color SetGreen(this ref Color color, float set) => color = new Color(color.r, set, color.b, color.a);
    public static Color ChangeGreen(this ref Color color, float change) => color = new Color(color.r, color.g + change, color.b, color.a);
    public static Color SetBlue(this ref Color color, float set) => color = new Color(color.r, color.g, set, color.a);
    public static Color ChangeBlue(this ref Color color, float change) => color = new Color(color.r, color.g, color.b + change, color.a);
    public static Color SetAlpha(this ref Color color, float set) => color = new Color(color.r, color.g, color.b, set);
    public static Color ChangeAlpha(this ref Color color, float change) => color = new Color(color.r, color.g, color.b, color.a + change);

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

#if UNITY_EDITOR
    [MenuItem("Assets/Force Compile")]
    public static void ForceCompile() => UnityEditor.Compilation.CompilationPipeline.RequestScriptCompilation();
#endif

    public static bool Gotten<T>(this T obj, out T result) where T : class
    {
        result = obj as T;
        return result != null;
    }
    public static bool Exists<T>(this T obj) where T : class => obj != null;
}

public static class XtensionsMath
{
    public static float P(this float F) => Mathf.Pow(F, 2);
    public static float P(this float F, int power) => Mathf.Pow(F, power);
    public static float SQRT(this float F) => Mathf.Sqrt(F);
    public static float Sin(this float F) => Mathf.Sin(F);
    public static float Cos(this float F) => Mathf.Cos(F);
    public static float Tan(this float F) => Mathf.Tan(F);
    public static float ASin(this float F) => Mathf.Asin(F);
    public static float ACos(this float F) => Mathf.Acos(F);
    public static float ATan(this float F) => Mathf.Atan(F);

    public static float Clamp(this float value, float min, float max) => (value < min) ? min : (value > max) ? max : value;
    public static float Min(this float value, float min) => (value < min) ? min : value;
    public static float Max(this float value, float max) => (value > max) ? max : value;

    public static int Int(this float value) => (int)value;
    public static float Float(this int value) => (float)value;
    public static int Floor(this float value) => Mathf.FloorToInt(value);
    public static int Ceil(this float value) => Mathf.CeilToInt(value);

    public static int Sign(this float value) => (int)Mathf.Sign(value);
    public static float Abs(this float value) => Mathf.Abs(value);
    public static float Repeat(this float value, float length) => Mathf.Repeat(value, length);

    // Random helpers
    public static float Randomize(this float value, float min, float max) => UnityEngine.Random.Range(min, max);
    public static int Randomize(this int value, int min, int max) => UnityEngine.Random.Range(min, max);

    public static float RandomTo(this float value, float min = 0) => UnityEngine.Random.Range(min, value);
    public static int RandomTo(this int value, int min = 0) => UnityEngine.Random.Range(min, value);

    public static float RandomBetween(this Vector2 input) => UnityEngine.Random.Range(input.x, input.y);
    public static int RandomBetween(this Vector2Int input) => UnityEngine.Random.Range(input.x, input.y);

    // Probability helper: returns true if random value in [0,1) is >= input (keeps original semantics)
    public static bool RandomChance(this float input) => UnityEngine.Random.Range(0f, 1f) >= input;

    // Movement helpers
    public static float MoveTowards(this float current, float rate, float target)
    {
        return current == target
            ? target
            : target > current
                ? current + rate >= target
                    ? target
                    : current + rate
                : current - rate <= target
                    ? target
                    : current - rate;
    }

    public static float MoveUp(this float current, float amount, float limit)
    {
        return current == limit
            ? limit
            : current < limit
                ? limit - current <= amount
                    ? limit
                    : current + amount
                : throw new System.Exception("You are trying to move a float upwards to something below it.");
    }

    public static float MoveDown(this float current, float amount, float limit)
    {
        return current == limit
            ? limit
            : current > limit
                ? current - limit <= amount
                    ? limit
                    : current - amount
                : throw new System.Exception("You are trying to move a float downwards to something above it.");
    }

    // Recasting / remapping
    public static float Recast(this float input, float fromA, float fromB, float toA, float toB) => Mathf.Lerp(toA, toB, Mathf.InverseLerp(fromA, fromB, input));
    public static float Recast(this float input, Vector2 from, Vector2 to) => Mathf.Lerp(to.x, to.y, Mathf.InverseLerp(from.x, from.y, input));
    public static float Recast(this float input, float fromA, float fromB, AnimationCurve to) => to.Evaluate(Mathf.InverseLerp(fromA, fromB, input));
}

public static class XtensionsMonoBehavior
{
    public static void LateAwake(this MonoBehaviour m, Delegate result)
    {
        m.StartCoroutine(Enum(result));
        static IEnumerator Enum(Delegate result)
        {
            yield return new WaitForEndOfFrame();
            result?.Invoke();
        }
    }
    

    public static bool Unloading(this MonoBehaviour M) => !M.gameObject.scene.isLoaded;


    public static void SafeDestroyers(this MonoBehaviour M, Delegate SafeDestroy, Delegate UnloadDestroy)
    {
        if (!M.gameObject.scene.isLoaded) SafeDestroy?.Invoke();
        else UnloadDestroy?.Invoke();
    }

    public static void Set(this Transform T, Vector3? pos = null, Vector3? rot = null, Vector3? scale = null, Transform parent = null)
    {
        if (pos != null) T.localPosition = pos.Value;
        if (rot != null) T.localEulerAngles = rot.Value;
        if (scale != null) T.localScale = scale.Value;
        if (parent != null) T.parent = parent;
    }

    public static GameObject NewGameObject(this UnityEngine.Object O, string name = "NewGameObject", Vector3? pos = null, Quaternion? rot = null, Vector3? scale = null, Transform parent = null, params System.Type[] additions)
    {
        GameObject result = new(name, additions);

        if (parent != null) result.transform.parent = parent;
        if (pos != null) result.transform.localPosition = pos.Value;
        if (rot != null) result.transform.localRotation = rot.Value;
        if (scale != null) result.transform.localScale = scale.Value;

        return result;
    }

    public static void Reset(this Transform transform, bool position = true, bool rotation = true, bool scale = true)
    {
        if (position) transform.localPosition = Vector3.zero;
        if (rotation) transform.localRotation = Quaternion.identity;
        if (scale) transform.localScale = Vector3.one;
    }


    public static T GetOrAddComponent<T>(this Component O) where T : Component
    {
        O.gameObject.TryGetComponent(out T V);
        V ??= O.gameObject.AddComponent<T>();
        return V;
    }
    public static T GetOrAddComponent<T>(this GameObject O) where T : Component
    {
        O.TryGetComponent(out T V);
        V ??= O.AddComponent<T>();
        return V;
    }

    public static void SetPositionAndRotation(this Transform target, Transform influence) => target.SetPositionAndRotation(influence.position, influence.rotation);

    public static List<T> GetComponentsRecursive<T>(this Component This) where T : Component
    {
        int length = This.transform.childCount;
        List<T> components = new List<T>(length + 1);
        T comp = This.transform.GetComponent<T>();
        if (comp != null) components.Add(comp);
        for (int i = 0; i < length; i++)
        {
            comp = This.transform.GetChild(i).GetComponent<T>();
            if (comp != null) components.Add(comp);
        }
        return components;
    }

    public static List<T> GetComponentsInDirectChildren<T>(this Component This) where T : Component
    {
        int length = This.transform.childCount;
        List<T> components = new List<T>(length);
        for (int i = 0; i < length; i++)
        {
            T comp = This.transform.GetChild(i).GetComponent<T>();
            if (comp != null) components.Add(comp);
        }
        return components;
    }

    public static T FindComponentInAncestry<T>(this Component This) where T : Component
    {
        Transform current = This.transform;
        while (current != null)
        {
            if (current.TryGetComponent(out T component))
                return component;
            current = current.parent;
        }
        return null;
    }

    public static void DoIfGetComponent<T>(this Component C, Action<T> IfTrue, Action<T> IfFalse = null) where T : Component
    {
        T com = C.GetComponent<T>();
        if (com != null) IfTrue?.Invoke(com);
        else IfFalse?.Invoke(null);
    }

    public static T InitComponent<T>(this Component C, ref T com) where T : Component
    {
        if (com != null) return com;
        C.TryGetComponent(out com);

        if (com == null)
        {
#if UNITY_EDITOR
            Debug.LogError($"MonoBehaviour {C.name} Missing Necessary Component of type {typeof(T)}");
#else
            // In builds, you might choose to handle this differently.
#endif
        }

        return com;
    }

    public static Vector2 GetUp(this PlatformEffector2D P) => P.transform.up.Rotated(P.rotationalOffset, Direction.back);
}

public static class XtensionsCollections
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

public static class XtensionsTransform
{
    /// <summary>
    ///     Rotates the transform so the specified vector points at a targets current position.
    /// </summary>
    public static void LookAt(this Transform trans, Vector3 target, LookDirection direction)
    {
        trans.LookAt(target);
        switch (direction)
        {
            case LookDirection.Forward:
                trans.eulerAngles += new Vector3(0, 0, 0);
                break;
            case LookDirection.Backward:
                trans.eulerAngles += new Vector3(180, 0, 0);
                break;
            case LookDirection.Upward:
                trans.eulerAngles += new Vector3(90, 0, 0);
                break;
            case LookDirection.Downward:
                trans.eulerAngles += new Vector3(-90, 0, 0);
                break;
            case LookDirection.Rightward:
                trans.eulerAngles += new Vector3(-trans.eulerAngles.x, -90, -trans.eulerAngles.x);
                break;
            case LookDirection.Leftward:
                trans.eulerAngles += new Vector3(-trans.eulerAngles.x, 90, trans.eulerAngles.x);
                break;
        }
    }

    /// <summary>
    ///     Rotates the transform so the specified vector points at a targets current position.
    /// </summary>
    public static void LookAt(this Transform trans, Transform target, LookDirection direction)
    {
        trans.LookAt(target);
        switch (direction)
        {
            case LookDirection.Forward:
                trans.eulerAngles += new Vector3(0, 0, 0);
                break;
            case LookDirection.Backward:
                trans.eulerAngles += new Vector3(180, 0, 0);
                break;
            case LookDirection.Upward:
                trans.eulerAngles += new Vector3(90, 0, 0);
                break;
            case LookDirection.Downward:
                trans.eulerAngles += new Vector3(-90, 0, 0);
                break;
            case LookDirection.Rightward:
                trans.eulerAngles += new Vector3(-trans.eulerAngles.x, -90, -trans.eulerAngles.x);
                break;
            case LookDirection.Leftward:
                trans.eulerAngles += new Vector3(-trans.eulerAngles.x, 90, trans.eulerAngles.x);
                break;
        }
    }

    /// <summary>
    ///     The Direction pointed at the target
    /// </summary>
    public enum LookDirection
    {
        Forward = 1,
        Backward = 2,
        Upward = 3,
        Downward = 4,
        Rightward = 5,
        Leftward = 6
    };
}