using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using System.Reflection;
using System.Linq;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.UIElements;
#endif

namespace SLS.EditorUtilities.ComponentHeaders
{
    /// <summary>
    /// Places this field into a custom header on any <see cref="MonoBehaviour"/>
    /// </summary>
    public class HeaderItemAttribute : PropertyAttribute
    {
        public bool require;
        public string subLocation;
        /// <summary>
        /// Optional name of an instance method (on the target MonoBehaviour) to call via reflection
        /// to obtain the component or a GameObject/Transform that contains the component.
        /// The method must be parameterless.
        /// Use as a named argument: [HeaderItem(methodName = "GetTarget")]
        /// </summary>
        public string methodName;

        /// <summary>
        /// Places this field into a custom header on any <see cref="MonoBehaviour"/>
        /// </summary>
        /// <param name="require">Whether this parameter is considered required. Purely Editor functionality to warn of missing components</param>
        /// <param name="subLocation">The intended sub-path where the attribute should look for a component, separated by / marks.</param>
        /// <param name="methodName">Optional name of a parameterless method on the target that returns a Component/GameObject/Transform to resolve the component from.</param>
        public HeaderItemAttribute(bool require = false, string subLocation = null, string methodName = null)
        {
            this.require = require;
            this.subLocation = subLocation;
            this.methodName = methodName;
        }

        /// <summary>
        /// Places this field into a custom header on any <see cref="MonoBehaviour"/>
        /// </summary>
        /// <param name="subLocation">The intended sub-path where the attribute should look for a component, separated by / marks</param>
        public HeaderItemAttribute(string subLocation)
        {
            this.require = false;
            this.subLocation = subLocation;
            this.methodName = null;
        }

        public static Component GetRelatedComponent(MonoBehaviour target, System.Type componentType, string subDirectory, string methodName = null, bool addIfNotFound = false) => ComponentHeaders.GetRelatedComponent(target, componentType, subDirectory, methodName, addIfNotFound);

        public static void Reset(MonoBehaviour target) => ComponentHeaders.Reset(target);
    }

    public static class ComponentHeaders
    {
        // Shared helper: centralizes logic to find a component of the given Type on the given MonoBehaviour's GameObject.
        // Returns the found Component or null. Does not log errors about missing required components (caller handles that).
        public static Component GetRelatedComponent(MonoBehaviour target, System.Type componentType, string subDirectory, string accessorMethodName = null, bool addIfNotFound = false)
        {
            // First, if an accessor method name is provided, attempt to invoke it and resolve from the returned object.
            if (!string.IsNullOrEmpty(accessorMethodName))
            {
                try
                {
                    var flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy;
                    var method = target.GetType().GetMethod(accessorMethodName, flags);
                    if (method != null && method.GetParameters().Length == 0)
                    {
                        var returned = method.Invoke(target, null);
                        if (returned != null)
                        {
                            // If the returned object is directly the requested component type
                            if (returned is Component directComp && componentType.IsAssignableFrom(directComp.GetType()))
                            {
                                return directComp;
                            }

                            // If returned a GameObject, get/add the component there
                            if (returned is GameObject go)
                            {
                                var comp = go.GetComponent(componentType);
                                if (comp) return comp;
                                if (addIfNotFound)
                                {
                                    comp = go.AddComponent(componentType);
#if UNITY_EDITOR
                                    Undo.RegisterCreatedObjectUndo(comp, "Add Related Component");
#endif
                                    return comp;
                                }
                                // Not found; continue to fallback behavior
                            }

                            // If returned a Transform, search on its GameObject
                            if (returned is Transform t)
                            {
                                var comp = t.gameObject.GetComponent(componentType);
                                if (comp) return comp;
                                if (addIfNotFound)
                                {
                                    comp = t.gameObject.AddComponent(componentType);
#if UNITY_EDITOR
                                    Undo.RegisterCreatedObjectUndo(comp, "Add Related Component");
#endif
                                    return comp;
                                }
                            }

                            // If returned some other Component, try to locate requested component on its GameObject
                            if (returned is Component someComp)
                            {
                                // If returned component is assignable, return it already handled above.
                                var comp = someComp.GetComponent(componentType);
                                if (comp) return comp;
                                if (addIfNotFound)
                                {
                                    comp = someComp.gameObject.AddComponent(componentType);
#if UNITY_EDITOR
                                    Undo.RegisterCreatedObjectUndo(comp, "Add Related Component");
#endif
                                    return comp;
                                }
                            }

                            // If returned object itself is of a type assignable to the requested type (non-Component)
                            // unlikely for Unity components, but attempt a cast if possible.
                            if (componentType.IsInstanceOfType(returned) && returned is Component asComp)
                            {
                                return asComp;
                            }
                        }
                    }
                }
                catch
                {
                    // Swallow reflection exceptions and fall back to original behavior.
                }
            }

            // Fallback: existing subDirectory-based search/add logic.
            GameObject foundSubTarget = null;
            if (subDirectory != null)
            {
                string[] directory = subDirectory.Split('/');
                foundSubTarget = target.gameObject;

                foreach (var d in directory)
                {
                    Transform child = foundSubTarget.transform.Find(d);
                    if (child != null) foundSubTarget = child.gameObject;
                    else
                    {
                        foundSubTarget = null;
                        break;
                    }
                }
            }

            Component result = null;

            if (subDirectory != null && foundSubTarget)
            {
                result = foundSubTarget.GetComponent(componentType);
                if (result) return result;

                if (addIfNotFound)
                {
                    result = foundSubTarget.AddComponent(componentType);
#if UNITY_EDITOR
                    Undo.RegisterCreatedObjectUndo(result, "Add Related Component");
#endif
                    return result;
                }
                else
                {
                    result = target.GetComponent(componentType);
                    return result;
                }
            }
            else
            {
                if (addIfNotFound)
                {
                    result = target.gameObject.AddComponent(componentType);
#if UNITY_EDITOR
                    Undo.RegisterCreatedObjectUndo(result, "Add Related Component");
#endif
                    return result;
                }
                else
                {
                    result = target.GetComponent(componentType);
                    return result;
                }

            }

        }

        public static void Reset(MonoBehaviour target)
        {
            //Run through all fields with RelatedComponentAttribute or PlaceInHeaderAttribute
            var fields = target.GetType().GetFields();
            foreach (var field in fields)
            {
                // Get all attributes and check for either RelatedComponentAttribute or PlaceInHeaderAttribute
                var attrs = field.GetCustomAttributes(true);
                foreach (var a in attrs)
                {
                    if (a is HeaderItemAttribute placeAttr)
                    {
                        var fieldType = field.FieldType;
                        if (typeof(Component).IsAssignableFrom(fieldType))
                        {
                            var GetComp = GetRelatedComponent(target, fieldType, placeAttr.subLocation, placeAttr.methodName, placeAttr.require);
                            if (GetComp != null)
                            {
                                field.SetValue(target, GetComp);
                            }
                            else if (placeAttr.require)
                            {
                                var addComp = target.gameObject.AddComponent(fieldType);
                                field.SetValue(target, addComp);
                            }
                        }
                        break;
                    }
                }
            }
        }
    }
}