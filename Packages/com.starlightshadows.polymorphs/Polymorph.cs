using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using System.Reflection;
using System.Collections.Generic;


#if UNITY_EDITOR
using UnityEditor.UIElements;
#endif

[System.Serializable]
public abstract partial class Polymorph
{


    public static List<Type> GetSubtypes(Type baseType) => baseType.GetAllInheritors(true, false, !baseType.IsAbstract);

#if UNITY_EDITOR
    public virtual void OverrideBody(VisualElement container, SerializedProperty property)
    {
        // Iterate visible children of the property and add a PropertyField for each.
        SerializedProperty iterator = property.Copy();
        SerializedProperty end = iterator.GetEndProperty(); // one past the last child
                                                            // Move into the first visible child
        if (!iterator.NextVisible(true))
            return;

        while (!SerializedProperty.EqualContents(iterator, end))
        {
            // Make a copy for the PropertyField since iterator will advance
            var childProp = iterator.Copy();
            var field = new PropertyField(childProp);
            field.Bind(property.serializedObject);
            container.Add(field);

            // Advance to next visible sibling/child
            if (!iterator.NextVisible(false))
                break;
        }
    }
#endif

    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public sealed class ValidTypesAttribute : Attribute
    {
        public Type[] Types { get; }

        public ValidTypesAttribute(params Type[] types) => Types = types;
    }
}
