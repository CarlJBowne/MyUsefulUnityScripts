using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using EditorAttributes;
using UnityEngine.UIElements;



#if UNITY_EDITOR
using UnityEditor.UIElements;
using UnityEditor;
using Utilities.Xtensions.VisualElements;
#endif

public class Test : MonoBehaviour
{
    //public Polymorph.UniqueList<PolymorphTest> testList = new();
    public List<int> ints = new();

#if UNITY_EDITOR
    [CustomEditor(typeof(Test))]
    public class Editor : UnityEditor.Editor
    {
        public override VisualElement CreateInspectorGUI()
        {
            VisualElement root = new();

            SuperList<int> List = new(serializedObject.FindProperty("ints"));

            //root.Add(new PropertyField(serializedObject.FindProperty("testList")));
            root.Add(List);

            return root;
        }
    }
#endif
}

[System.Serializable]
public abstract class PolymorphTest : Polymorph
{
    public string str = "";

    [System.Serializable]
    public class Int : PolymorphTest
    {
        public int I = 1;
    }
    [System.Serializable]
    public class Float : PolymorphTest
    {
        public float F = 1.0f;
    }
    [System.Serializable]
    public class Char : PolymorphTest
    {
        public char C = 'a';
    }
}