using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Projectile : MonoBehaviour {

    public float moveSpeed;
    public float maxTravelTime = 5;
    public bool isHitScan = false;

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
        if (!isHitScan)
        {
            transform.position = Vector3.MoveTowards(transform.position, transform.position + transform.forward, moveSpeed);
        }
        

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
        
        if(isHitScan)
        {
            FireHitScan();
        }
    }

    void FireHitScan()
    {
        Ray r = new Ray(transform.position, transform.forward);
        if(Physics.Raycast(r, out RaycastHit hit))
        {
            if(hit.transform.CompareTag(hitTag))
            {
                DealDamage(hit.transform.GetComponent<Health>(), hit.point, hit.transform);
            }
        }
        Cleanup();
    }

    void DealDamage(Health health, Vector3 hitPosition, Transform targetTransform)
    {
        // Projectile: use your current positon as hitPosition
        // Hitscan: use where the ray hit as your hitPosition

        // create the blood splat particles
        // using the position and roation of the bullet
        // the ParticleController class should kill the particles
        var splatCopy = Instantiate(bloodSplat, hitPosition, transform.rotation);
        // having the blood explode straight away from the enemy looks much better than using the bullet's rotation
        splatCopy.transform.forward = hitPosition - targetTransform.position;

        health?.TakeDamage(damage);
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
        if (col.CompareTag(hitTag))
        {
            // do damage
            Health h = col.GetComponent<Health>();
            if (h != null)
            {
                DealDamage(h, transform.position, col.transform);
                
            }
            Cleanup();
        }
        else if (col.CompareTag("Obstacle"))
        {
            Cleanup();
        }
        else if (col.gameObject.layer == LayerMask.NameToLayer("AimSurface"))
        {
            Cleanup();
        }
    }
}
