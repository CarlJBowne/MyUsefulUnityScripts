using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using EditorAttributes;

using Stopwatch = System.Diagnostics.Stopwatch;
using System;

public class Test : MonoBehaviour
{
    public Polymorph.ListOf<PolymorphTest> testList = new();
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