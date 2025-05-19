using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ApplyDontDestroyOnLoad : MonoBehaviour
{
    void Start()
    {
        DontDestroyOnLoad(gameObject);
    }
}
