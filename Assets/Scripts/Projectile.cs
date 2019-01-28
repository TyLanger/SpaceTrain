using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Projectile : MonoBehaviour {

    public float moveSpeed;
    public float maxTravelTime = 5;

    public float damage;

    // who does this bullet hurt?
    // the player? The enemies?
    public string hitTag = "Enemy";

    // Particles
    // Impact particles
    // Impact sound
    public GameObject bloodSplat;

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
        damage = _damage;
        
    }

    void Cleanup()
    {
        moveSpeed = 0;

        // re add to the object pool

        // this should be temporary
        Destroy(gameObject);
    }

    void OnTriggerEnter(Collider col)
    {
        if(col.CompareTag(hitTag))
        {
            // do damage
            Health h = col.GetComponent<Health>();
            if(h != null)
            {
                //bloodSplat.SetActive(true);
                // create the blood splat particles
                // using the position and roation of the bullet
                // the ParticleController class should kill the particles
                var splatCopy = Instantiate(bloodSplat, transform.position, transform.rotation);
                // having the blood explode straight away from the enemy looks much better than using the bullet's rotation
                splatCopy.transform.forward = transform.position - col.transform.position;
                h.TakeDamage(damage);
            }
            Cleanup();
        }
        else if(col.CompareTag("Obstacle"))
        {
            Cleanup();
        }
    }
}
