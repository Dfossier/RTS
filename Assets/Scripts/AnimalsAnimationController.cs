using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class AnimalsAnimationController : MonoBehaviour
{
    public Animator animator;
    public NavMeshAgent agent;

    

    void Update()
    {
        // If agent is moving, set animator speed to 1; otherwise 0
        // animator.speed = agent.velocity.sqrMagnitude > 0.01f ? 1f : 0f;
    }
}
