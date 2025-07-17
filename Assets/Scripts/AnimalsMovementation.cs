using System.Collections;
using System.Collections.Generic;
using RTSEngine;
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

    public GameObject animalmodel;

    public float timer_tolerance = 0f;

    void Start()
    {
        SetNewDestination();

    }

    void Update()
    {
        if (animator.GetInteger("states") == 2) return;
        if (agent.isOnNavMesh == false) return;
        FaceTarget();
        // Check if agent has reached destination
        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
        {
            if (!isWaiting)
            {
                isWaiting = true;
                timer = 0f;
                agent.isStopped = true;
                // animator.speed = 0f; // stop animation
                animator.SetInteger("states", 0);
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
            // animator.speed = 1f;
            animator.SetInteger("states", 1);

            // check if it's trying to go to a place that is not really reachable, if it is stucked then change destination after 6 seconds
            if (agent.velocity.magnitude < 0.5f)
            {
                timer_tolerance += Time.deltaTime;
                if (timer_tolerance >= 6f)
                {
                    isWaiting = false;
                    SetNewDestination(true);
                    timer_tolerance = 0f;
                }
            }
        }
    }

    public void playDeathAnimation()
    {
        animator.SetInteger("states", 2);
        agent.speed = 0;
        Debug.Log("state changed");
        Debug.Log(animator.GetInteger("states"));
    }

    void SetNewDestination(bool overwrite = false)
    {
        if (animator.GetInteger("states") == 2 && overwrite == false) return;
        agent.speed = 1;
        agent.angularSpeed = 1;
        agent.updateRotation = true;
        Vector3 randomDirection = Random.insideUnitSphere * wanderRadius;
        randomDirection += transform.position;

        if (NavMesh.SamplePosition(randomDirection, out NavMeshHit hit, wanderRadius, NavMesh.AllAreas))
        {
            if (!agent.isOnNavMesh) return;
            agent.SetDestination(hit.position);
        }
    }
    
    void FaceTarget()
    {
        Vector3 direction = agent.velocity;
        direction.y = 0; // Ignore vertical component for rotation
        if (direction.sqrMagnitude > 0.01f) // Only rotate if there's significant movement
        {
            Quaternion lookRotation = Quaternion.LookRotation(direction);
            animalmodel.transform.rotation = Quaternion.Slerp(animalmodel.transform.rotation, lookRotation, Time.deltaTime * 5f);
        }
    }

}
