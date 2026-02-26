using Newtonsoft.Json.Linq;
using System.Linq;
using System;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

[CreateAssetMenu(fileName = "NewSOCollection", menuName ="SO Collection"), System.Serializable]
public class ScriptableCollection : ScriptableObject
{
    public string selectedType = null;
    public string SelectedType => selectedType;

    public List<ScriptableObject> List = new();

    public void Create()
    {
#if UNITY_EDITOR
        ScriptableObject NEWObject = Activator.CreateInstance(Type.GetType(selectedType)) as ScriptableObject;

        NEWObject.name = $"{List.Count}_NewObject";
        AssetDatabase.AddObjectToAsset(NEWObject, this);
        Undo.RegisterCreatedObjectUndo(NEWObject, "Added New Object");
        AssetDatabase.SaveAssets();
        List.Add(NEWObject);
#endif
    }
    public void DeleteAt(int i)
    {
#if UNITY_EDITOR
        Undo.RecordObject(List[i], "Object Deleted");
        DestroyImmediate(List[i], true);
        List.RemoveAt(i);
        AssetDatabase.SaveAssets();
#endif
    }

    public List<T> Cast<T>() where T : ScriptableObject => List.Cast<T>().ToList();

    public JToken Serialize()
    {
        return new JObject(
                new JProperty(nameof(selectedType), selectedType),
                new JProperty(nameof(List), new JArray(
                    from I in List
                    select new JObject().Serialize(I)
                    ))
                );
    }
    public void Deserialize(JToken Data)
    {
        string readSOType = Data[nameof(selectedType)].Value<string>();
        if (readSOType != selectedType) throw new Exception("WRONG SCRIPTABLE OBJECT TYPE.");

        var jsonArray = Data[nameof(List)] as JArray;

        for (int i = 0; i < jsonArray.Count && i < List.Count; i++)
        {
            jsonArray[i].DeserializeInto(List[i]);
#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(List[i]);
#endif
        }

    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(ScriptableCollection), true)]
public class ScriptableCollectionEditor : Editor
{
    ScriptableCollection This;
    bool initialized;
    string TypeSetupString;

    void OnEnable()
    {
        This = target as ScriptableCollection;
        initialized = This.SelectedType != null;
        TypeSetupString = "Insert Type Name Here";
    }

    public override void OnInspectorGUI()
    {
        //base.OnInspectorGUI();
        serializedObject.Update();

        if (initialized)
        {
            for (int i = 0; i < This.List.Count; i++)
            {
                GUILayout.BeginHorizontal();

                string editedName = EditorGUILayout.DelayedTextField(This.List[i].name.Substring(2), GUILayout.ExpandWidth(true));
                if (editedName != This.List[i].name.Substring(2))
                {
                    This.List[i].name = $"{i}_{editedName}";
                    EditorUtility.SetDirty(This.List[i]);
                    AssetDatabase.SaveAssets();
                    AssetDatabase.Refresh();
                }

                GUI.backgroundColor = Color.red;
                if (GUILayout.Button("-", GUILayout.Width(30))) This.DeleteAt(i);
                GUI.backgroundColor = Color.white;
                GUILayout.EndHorizontal();
            }

            GUI.backgroundColor = Color.green;
            if (GUILayout.Button("+")) This.Create();
            GUI.backgroundColor = Color.white;

            if(This.List.Count == 0)
            {
                GUI.backgroundColor = Color.blue;
                if (GUILayout.Button("UnreferencedChildren?"))
                {
                    var ALL = AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GetAssetPath(This)).ToList();
                    ALL.Remove(This);
                    This.List = ALL.Cast<ScriptableObject>().ToList();
                }
                GUI.backgroundColor = Color.white;
            }
            
        }
        else
        {
            TypeSetupString = GUILayout.TextField(TypeSetupString);
            if(GUILayout.Button("Initialize"))
            {
                var resultType = Type.GetType(TypeSetupString);
                This.selectedType = resultType != null ? resultType.AssemblyQualifiedName : throw new Exception("Invalid Type Name.");
                AssetDatabase.SaveAssetIfDirty(This);
                initialized = true;
            }
        }

        serializedObject.ApplyModifiedProperties();
    }

    //private void Load() => assets = AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GetAssetPath(target)).Cast<ScriptableObject>().ToList();

}

#endif