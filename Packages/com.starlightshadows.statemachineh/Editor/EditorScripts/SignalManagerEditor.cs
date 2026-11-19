using System;
using System.Collections;
using System.Collections.Generic;
using SLS.ListUtilities.Editor;
using SLS.StateMachineUltimate.Editor;
using SLS.StateMachineUltimate.Utils;
using SLS.EditorUtilities;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;


#if ULT_EVENTS
using EVENT = UltEvents.UltEvent;
#else
using EVENT = UnityEngine.Events.UnityEvent;
#endif

namespace SLS.StateMachineUltimate.Signals
{
    [CustomPropertyDrawer(typeof(SignalSet))]
    internal class SignalSetDrawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            VisualElement Display;
    
            Type DrawerType = typeof(HashedListDrawer.ListDrawer<>)
                .MakeGenericType(typeof(EVENT));
            var literal = fieldInfo.GetValue(property.serializedObject.targetObject)
                as SLS.ListUtilities.ILookupTable;
    
            // Pass the live literal (the actual dictionary instance) to the drawer so it
            // can recalculate occurrences and provide proper binding. Using property.boxedValue
            // here returned a boxed/copy and left Literal null which caused blank/uneditable fields.
            Display = Activator.CreateInstance(DrawerType, property, literal, true) as VisualElement;
    
            return Display;
        }
    }
}