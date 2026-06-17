using UnityEngine;
using System;
using R3;

public class MyLifeTimeProde : MonoBehaviour
{
   
    private void Start()
    {
        Observable
        .Interval(TimeSpan.FromSeconds(5))
        .Subscribe(_ => Debug.Log("MyLifeTimeProbe Inverval"))
        .AddTo(this);
    }
   
}
