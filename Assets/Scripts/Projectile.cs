using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Projectile : MonoBehaviour {

    public float moveSpeed;
    public float maxTravelTime = 5;

    public float damage;

    // Particles
    // Impact particles
    // Impact sound

    void Start()
    {
        // clean this up (destroy) in maxTravelTime seconds
        Invoke("Cleanup", maxTravelTime);
    }

    // Update is called once per frame
    void FixedUpdate () {

        transform.position = Vector3.MoveTowards(transform.position, transform.position + transform.forward, moveSpeed);
        

        // Raycast for collision detection?
        // probably more reliable than colliders teleporting around
	}

    /// <summary>
    /// Set up the projectile when it's spawned
    /// </summary>
    public void Create(float speed, Vector3 direction, float _damage)
    {
        moveSpeed = speed;
        transform.forward = direction;
        this.damage = _damage;
        
    }

    void Cleanup()
    {
        moveSpeed = 0;

        // re add to the object pool

        // this should be temporary
        Destroy(gameObject);
    }
}
