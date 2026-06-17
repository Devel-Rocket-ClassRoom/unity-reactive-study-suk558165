using R3;
using UnityEngine;

public class HelloR3 : MonoBehaviour
{
    private void Start()
    {
        Observable
            .Range(1, 3)                            
            .Subscribe(x => Debug.Log($"받음: {x}")); 
    }
}