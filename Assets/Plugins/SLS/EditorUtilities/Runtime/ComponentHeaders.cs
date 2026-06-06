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
        /// Places this field into a custom header on any <see cref="MonoBehaviour"/>
        /// </summary>
        /// <param name="require">Whether this parameter is considered required. Purely Editor functionality to warn of missing components</param>
        /// <param name="subLocation">The intended sub-path where the attribute should look for a component, separated by / marks.</param>
        public HeaderItemAttribute(bool require = false, string subLocation = null)
        {
            this.require = require;
            this.subLocation = subLocation;

        }
        /// <summary>
        /// Places this field into a custom header on any <see cref="MonoBehaviour"/>
        /// </summary>
        /// <param name="subLocation">The intended sub-path where the attribute should look for a component, separated by / marks</param>
        public HeaderItemAttribute(string subLocation)
        {
            this.require = false;
            this.subLocation = subLocation;
        }

        public static Component GetRelatedComponent(MonoBehaviour target, System.Type componentType, string subDirectory, bool addIfNotFound = false) => ComponentHeaders.GetRelatedComponent(target, componentType, subDirectory, addIfNotFound);

        public static void Reset(MonoBehaviour target) => ComponentHeaders.Reset(target);
    }

    public static class ComponentHeaders
    {
        // Shared helper: centralizes logic to find a component of the given Type on the given MonoBehaviour's GameObject.
        // Returns the found Component or null. Does not log errors about missing required components (caller handles that).
        public static Component GetRelatedComponent(MonoBehaviour target, System.Type componentType, string subDirectory, bool addIfNotFound = false)
        {
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
                    Undo.RegisterCreatedObjectUndo(result, "Add Related Component");
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
                    Undo.RegisterCreatedObjectUndo(result, "Add Related Component");
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
                            var GetComp = GetRelatedComponent(target, fieldType, placeAttr.subLocation, placeAttr.require);
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