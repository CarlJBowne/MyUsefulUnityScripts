using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using System.Reflection;

#if UNITY_EDITOR
using UnityEditor.UIElements;
#endif

[System.Serializable]
public abstract partial class Polymorph
{


    public static Type[] GetSubtypes(Type baseType, bool excludeSelf = true//, bool buildGenericPossibilities = false
        )
    {
        Type[] initialList = baseType.GetAllInheritors(false, false, false);
        System.Collections.Generic.List<Type> finaList = new();

        for (int i = 0; i < initialList.Length; i++)
        {
            Type t = initialList[i];
            if (t == baseType && excludeSelf) continue;
            if (!t.IsGenericType)
            {
                if (t.IsAbstract) continue;
                finaList.Add(t);
            }
            //else if(buildGenericPossibilities)
            //{
            //    if (t.GetCustomAttribute(typeof(ValidTypesAttribute)) is not ValidTypesAttribute attr || attr.Types == null) continue;
            //
            //    for (int i2 = 0; i2 < attr.Types.Length; i2++)
            //    {
            //        Type arg = attr.Types[i2];
            //        Type genType;
            //        try { genType = t.MakeGenericType(arg); }
            //        catch (ArgumentException)
            //        {
            //            // invalid type arg for this generic definition
            //            continue;
            //        }
            //
            //        if (genType.IsAbstract) continue;
            //        finaList.Add(genType);
            //    }
            //}
        }

        return finaList.ToArray();
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

    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public sealed class ValidTypesAttribute : Attribute
    {
        public Type[] Types { get; }

        public ValidTypesAttribute(params Type[] types) => Types = types;
    }
}
