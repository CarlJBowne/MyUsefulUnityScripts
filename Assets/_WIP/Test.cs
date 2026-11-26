using System;
using System.Collections.Generic;
using UnityEngine;
using SLS.ListUtilities;
using System.Diagnostics;











#if UNITY_EDITOR
#endif

public class Test : MonoBehaviour
{
    //public Yeet tht;
    public DictionaryS<string, int> D;

    public void Awake()
    {
        Dictionary<string, int> stringdic = new();
        stringdic.Add("Test1", 1);
        stringdic.Add("Test2", 2);
        stringdic.Add("Test3", 3);
        Dictionary<int, int> intdic = new();
        intdic.Add("Test1".Hash(), 1);
        intdic.Add("Test2".Hash(), 2);
        intdic.Add("Test3".Hash(), 3);
        int whatever;
        string Test2Name = "Test2";
        int Test2Hash = "Test2".Hash();

        DoTest("StringDictionaryGet", () =>
        {
            whatever = stringdic[Test2Name];
        });
        DoTest("IntDictionaryGet", () =>
        {
            whatever = intdic[Test2Hash];
        });
        DoTest("HashOnly", () =>
        {
            whatever = Test2Name.Hash();
        });
        DoTest("DoHashAndIntDictionaryGet", () =>
        {
            int hash = Test2Name.Hash();
            whatever = intdic[hash];
        });
    }

    private void DoTest(string name, Action res)
    {
        Stopwatch sw = Stopwatch.StartNew();
        for (int i = 0; i < 1000000; i++) res?.Invoke();
        sw.Stop();
        UnityEngine.Debug.Log($"{name} test : {sw.ElapsedMilliseconds}");
    }

//#if UNITY_EDITOR
//    [CustomEditor(typeof(Test))]
//    public class TestEditor : Editor
//    {
//        public override VisualElement CreateInspectorGUI()
//        {
//            var root = new VisualElement();
//            root.Add(new PropertyField(serializedObject.FindProperty("tht")));
//            Foldout f = new Foldout() { text = "Test", value = false, name = "TestFoldout" };
//            f.Add(new Label("AAAAAAAA"));
//            root.Add(f);
//            return root;
//        }
//    }
//#endif 
}


[System.Serializable]
public class Yeet
{
    public int test1;
    public bool test2;
    public Yeet2 test3;
    [System.Serializable]
    public class Yeet2
    {
        public int AAA;
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