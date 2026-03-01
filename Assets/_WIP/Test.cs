using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using EditorAttributes;

using Stopwatch = System.Diagnostics.Stopwatch;
using System;

public class Test : MonoBehaviour
{



    private void Start()
    {
        fun();
    }

    
    void fun()
    {
		int time1 = UnitTest(()=> 
		{
			Vector3 result = Direction.upRight;
		});
		int time2 = UnitTest(()=> 
		{

		});



		Debug.LogFormat("Addition: {0}, Aggressive {1}", time1, time2);


	}

	public int UnitTest(Action action)
	{
		Stopwatch time = new();
		time.Start();
		for (int i = 0; i < 100000000; i++)
		{
			action();
		}
		time.Stop();
		return (int)time.ElapsedMilliseconds;
	}


}
