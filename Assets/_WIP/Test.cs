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
    [SerializeField, SerializeReference] PolymorphTest test;
    public Polymorph.ListOf<PolymorphTest> testList = new();
    //public Testy tesy;

    [System.Serializable]
    public class Testy
    {
        public int yes = 1;
        public char no = 'a';
    }
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