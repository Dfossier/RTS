using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnimalsOwnerController : MonoBehaviour
{
    public GameObject Owner;
    private AnimalsMovementation movement;

    void Start()
    {
        movement = gameObject.GetComponent<AnimalsMovementation>();
    }

    void Update()
    {
        if (movement == null)
            return;
        if (Owner != null)
            movement.ownerTarget = Owner.transform; // owner exists, so it was tammed and this need to follow around the owner position
        else
            movement.ownerTarget = null;
    }
}
