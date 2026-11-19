using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

/*
public class SceneFacade
{
    #region Static

    public static Dictionary<int, SceneFacade> AllScenes;
    public static List<SceneFacade> ActiveScenes;

    #endregion

    #region Parameters and State

    public string name { get; private set; }
    public int index { get; private set; }
    public State state { get; internal set; }

    public enum State
    {
        Null = -2,
        Unregistered,
        Registered,
        Unloading,
        Loading,
        Loaded,
        Primary,
    }

    #endregion

    #region Creation and Registration

    private SceneFacade() { }

    public static SceneFacade OfName(string name)
    {
        int index = sceneIndexFromName(name); //Get Index
        if (!AllScenes.ContainsKey(index))
        {
            SceneFacade newScene = new()
            {
                index = index,
                name = name,
                state = State.Registered,
            };
            AllScenes.Add(index, newScene);
        }
        return AllScenes[index];
    }
    public static SceneFacade OfIndex(int index)
    {
        if (index > SceneManager.sceneCountInBuildSettings - 1) throw new IndexOutOfRangeException($"Invalid index {index}");
        string name = System.IO.Path.GetFileNameWithoutExtension(SceneUtility.GetScenePathByBuildIndex(index)); //Get Name
        if (!AllScenes.ContainsKey(index))
        {
            SceneFacade newScene = new()
            {
                index = index,
                name = name,
                state = State.Registered
            };
            AllScenes.Add(index, newScene);
        }
        return AllScenes[index];
    }

    private static int sceneIndexFromName(string sceneName)
    {
        for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
            if (SceneManager.GetSceneByBuildIndex(i).name == sceneName) 
                return i; 
        return -1; 
    }

    #endregion
}
*/