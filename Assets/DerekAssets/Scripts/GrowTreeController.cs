using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GrowTreeController : MonoBehaviour
{
    [SerializeField] float growSpeed = 0.0019f;

    void Update()
    {
        transform.localScale = Vector3.zero;

        Vector3.MoveTowards(
            transform.localScale,
            Vector3.one,
            Time.deltaTime * growSpeed
        );

    }
}
