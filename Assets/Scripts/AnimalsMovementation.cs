using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class AnimalsMovementation : MonoBehaviour
{
    public Animator animator;
    public NavMeshAgent agent;

    public float wanderRadius = 10f;
    public float waitTime = 3f;

    private float timer = 0f;
    private bool isWaiting = false;

    void Start()
    {
        SetNewDestination();

    }

    void Update()
    {
        FaceTarget();
        // Check if agent has reached destination
        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
        {
            if (!isWaiting)
            {
                isWaiting = true;
                timer = 0f;
                agent.isStopped = true;
                animator.speed = 0f; // stop animation
            }

            // Wait before moving again
            timer += Time.deltaTime;
            if (timer >= waitTime)
            {
                isWaiting = false;
                SetNewDestination();
            }
        }
        else
        {
            // Keep moving
            agent.isStopped = false;
            animator.speed = 1f;
        }
    }

    void SetNewDestination()
    {
        agent.speed = 1;
        agent.angularSpeed = 1;
        agent.updateRotation = true;
        Vector3 randomDirection = Random.insideUnitSphere * wanderRadius;
        randomDirection += transform.position;

        if (NavMesh.SamplePosition(randomDirection, out NavMeshHit hit, wanderRadius, NavMesh.AllAreas))
        {
            agent.SetDestination(hit.position);
        }
    }
    
    void FaceTarget()
    {
        Vector3 direction = agent.velocity;
        Quaternion lookRotation = Quaternion.LookRotation(new Vector3(direction.x, direction.y, 0));
        transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * 5);
    }

}
