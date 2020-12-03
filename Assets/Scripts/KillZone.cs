using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class KillZone : MonoBehaviour
{
    void OnTriggerEnter(Collider col)
    {
        // if you hit something that has a health component, do a bunch of damage to it
        col.GetComponent<Health>()?.TakeDamage(99999);
    }
}
