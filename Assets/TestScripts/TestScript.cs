using Newtonsoft.Json.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestScript : MonoBehaviour
{

    private void Awake()
    {

        int inputInt = 1233;
        string inputString = "YEETUS";

        JToken intToken = inputInt;
        JToken stringToken = inputString;
        
        Debug.Log($"Int Token A: {intToken}");
        Debug.Log($"Int Token B: {intToken.ToObject<int>()}");
        Debug.Log($"Int Token C: {(int)intToken}");

        Debug.Log($"String Token A: {stringToken}");
        Debug.Log($"String Token B: {stringToken.ToObject<string>()}");
        Debug.Log($"String Token C: {(string)stringToken}"); 



    }

}
