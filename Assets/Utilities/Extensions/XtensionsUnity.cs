using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Utilities.Xtensions.Unity
{
    public static class Xtensions_Unity_Core
    {
        public static void GetExecutionDetails(this MonoBehaviour M, out bool gameIsEditor, out bool gameIsPlaying, out bool objectSceneIsLoaded)
        {
#if UNITY_EDITOR
            gameIsEditor = true;
            gameIsPlaying = EditorApplication.isPlayingOrWillChangePlaymode;
#else
        gameIsEditor = false;
        gameIsPlaying = true;
#endif
            objectSceneIsLoaded = M.gameObject.scene.isLoaded;
        }

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

        public static GameObject NewGameObject(this UnityEngine.Object O, string name = "NewGameObject", Vector3? pos = null, Quaternion? rot = null, Vector3? scale = null, Transform parent = null, params System.Type[] additions)
        {
            GameObject result = new(name, additions);

            if (parent != null) result.transform.parent = parent;
            if (pos != null) result.transform.localPosition = pos.Value;
            if (rot != null) result.transform.localRotation = rot.Value;
            if (scale != null) result.transform.localScale = scale.Value;

            return result;
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
            List<T> components = new(length + 1);
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
            List<T> components = new(length);
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

    public static class Xtensions_Unity_Math
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

    public static class Xtensions_Unity_Transform
    {
        public static void Set(this Transform T, Vector3? pos = null, Vector3? rot = null, Vector3? scale = null, Transform parent = null)
        {
            if (pos != null) T.localPosition = pos.Value;
            if (rot != null) T.localEulerAngles = rot.Value;
            if (scale != null) T.localScale = scale.Value;
            if (parent != null) T.parent = parent;
        }

        public static void Reset(this Transform transform, bool position = true, bool rotation = true, bool scale = true)
        {
            if (position) transform.localPosition = Vector3.zero;
            if (rotation) transform.localRotation = Quaternion.identity;
            if (scale) transform.localScale = Vector3.one;
        }

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

    public static class Xtensions_Unity_ScriptableObjects
    {

    }
}