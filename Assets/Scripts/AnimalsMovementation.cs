using System.Collections;
using System.Collections.Generic;
using RTSEngine;
using RTSEngine.Entities;
using RTSEngine.EntityComponent;
using RTSEngine.Movement;
using UnityEngine;
using UnityEngine.AI;

public class AnimalsMovementation : MonoBehaviour
{
    public Animator animator;
    public NavMeshAgent agent;

    public float wanderRadius = 10f;
    public float waitTime = 3f;

    private float timer = 0f;

    public GameObject animalmodel;

    public float timer_tolerance = 0f;

    private UnitMovement unitMovement;
    private Unit animalEntity;
    private MovementManager movementManager;
    public UnitAttack unitAttack;
    
    // owner/tammed stuff
    public Transform ownerTarget;
    public float followDistance = 3f; // how close it stays to owner


    void Start()
    {
        unitMovement = transform.Find("unit_movement").gameObject.GetComponent<UnitMovement>();
        animalEntity = gameObject.GetComponent<Unit>();
        movementManager = FindObjectOfType<MovementManager>();

        RandomPosRTSIntegration();
    }

    void RandomPosRTSIntegration()
    {
        Vector3 targetPos;

        if (movementManager.GetRandomMovablePosition(animalEntity, animalEntity.transform.position, wanderRadius, out targetPos, playerCommand: false))
        {
            var moveData = new SetPathDestinationData<IEntity>
            {
                source = animalEntity,
                destination = targetPos,
                offsetRadius = 0f, // 0 means exact destination
                target = null,
                mvtSource = new MovementSource { playerCommand = false }
            };

            movementManager.SetPathDestinationLocal(moveData);
        }
    }

    void Update()
    {
        // return if animal is attacking, rts engine will handle it and move it around
        if (unitAttack != null)
        {
            if (unitAttack.HasTarget)
                return;
        }

        // if owner exists, follow around owner
        if (ownerTarget != null)
        {
            FollowOwner();
            return;
        }

        if (timer > 8)
        {
            RandomPosRTSIntegration();
            timer = 0;
        }
        timer += Time.deltaTime;
    }

    public void playDeathAnimation()
    {
        animator.SetInteger("states", 2);
        agent.speed = 0;
        //Debug.Log("state changed");
        //Debug.Log(animator.GetInteger("states"));
    }

    void FollowOwner()
    {
        float distance = Vector3.Distance(transform.position, ownerTarget.position);

        // Don't follow too close (prevents jitter)
        if (distance < followDistance)
            return;

        Vector3 targetPos;
        
        // Ask RTS Engine for a valid movable position near the owner
        if (movementManager.GetRandomMovablePosition(animalEntity, ownerTarget.position, 1f, out targetPos, playerCommand: false))
        {
            var moveData = new SetPathDestinationData<IEntity>
            {
                source = animalEntity,
                destination = targetPos,
                offsetRadius = 0f,
                target = null,
                mvtSource = new MovementSource { playerCommand = false }
            };

            movementManager.SetPathDestinationLocal(moveData);
        }
    }

}
