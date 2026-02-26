using EditorAttributes;
using System;
using System.Collections.Generic;
using Timer;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Timer
{

    [System.Serializable]
    public struct Loop
    {
        [SerializeField] public float rate;
        [SerializeField, DisableInEditMode, DisableInPlayMode] public float current;
        [HideInInspector] public bool disabled;

        public Loop(float rate, bool disable = false)
        {
            this.rate = rate;
            current = 0f;
            disabled = disable;
        }

        public void Tick(Action callback)
        {
            if (disabled) return;
            current += Time.deltaTime;
            if(current > rate)
            {
                current %= rate;
                callback?.Invoke();
            }
        }
    }

    [System.Serializable]
    public struct OneTime
    {
        [SerializeField] public float length;
        [SerializeField, DisableInEditMode, DisableInPlayMode] public float current;
        [HideInInspector] public bool running;

        public OneTime(float length, bool activate = false)
        {
            this.length = length;
            current = 0f;
            running = false;
            if (activate) Begin();
        }

        public void Begin()
        {
            current = 0f;
            running = true;
        }

        public void Tick(Action callback)
        {
            if (!running) return;
            current += Time.deltaTime;
            if (current > length)
            {

                running = false;
                callback?.Invoke();
            }
        }
    }

}

#if UNITY_EDITOR
[CustomPropertyDrawer(typeof(Loop))]
public class LoopPropertyDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);

        // Retrieve the serialized fields
        SerializedProperty rateProperty = property.FindPropertyRelative("rate");
        SerializedProperty currentProperty = property.FindPropertyRelative("current");

        // Draw the label
        position = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);

        // Adjust the width for the fields
        float fieldWidth = position.width / (EditorApplication.isPlaying ? 2 : 1);
        Rect rateRect = new Rect(position.x, position.y, fieldWidth, position.height);

        // Draw the "rate" field
        EditorGUI.PropertyField(rateRect, rateProperty, GUIContent.none);

        if (EditorApplication.isPlaying)
        {
            // Draw the range slider for "current" if in play mode
            Rect sliderRect = new Rect(position.x + fieldWidth + 5, position.y, fieldWidth - 5, position.height);
            float rateValue = rateProperty.floatValue;
            float currentValue = currentProperty.floatValue;
            currentValue = EditorGUI.Slider(sliderRect, currentValue, 0f, rateValue);
            currentProperty.floatValue = currentValue;
        }

        EditorGUI.EndProperty();
    }
}
[CustomPropertyDrawer(typeof(OneTime))]
public class OneTimePropertyDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);

        // Retrieve the serialized fields
        SerializedProperty lengthProperty = property.FindPropertyRelative("length");
        SerializedProperty currentProperty = property.FindPropertyRelative("current");

        // Draw the label
        position = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);

        // Adjust the width for the fields
        float fieldWidth = position.width / (EditorApplication.isPlaying ? 2 : 1);
        Rect rateRect = new Rect(position.x, position.y, fieldWidth, position.height);

        // Draw the "rate" field
        EditorGUI.PropertyField(rateRect, lengthProperty, GUIContent.none);

        if (EditorApplication.isPlaying)
        {
            // Draw the range slider for "current" if in play mode
            Rect sliderRect = new Rect(position.x + fieldWidth + 5, position.y, fieldWidth - 5, position.height);
            float lengthValue = lengthProperty.floatValue;
            float currentValue = currentProperty.floatValue;
            currentValue = EditorGUI.Slider(sliderRect, currentValue, 0f, lengthValue);
            currentProperty.floatValue = currentValue;
        }

        EditorGUI.EndProperty();
    }
}

#endif