using System;
using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

[System.Serializable]
public abstract partial class Polymorph
{


    public static Type[] GetSubtypes(Type baseType)
    {
        return AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(a =>
            {
                try { return a.GetTypes(); }
                catch { return Type.EmptyTypes; }
            })
            .Where(t =>
                !t.IsAbstract &&
                // For interfaces, include implementers; for classes, include strict subclasses only.
                t.IsSubclassOf(baseType) && (t.IsPublic || t.IsNestedPublic || t.IsNestedFamORAssem || t.IsNestedFamily)
            )
            .ToArray();
    }

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
}
