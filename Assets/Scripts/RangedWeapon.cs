using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RangedWeapon : Weapon {

    
    // amount of time in seconds to wait between shots
    public float fireRate;
    float fireTimeout = 0;

    public float bulletSpeed;
    public float bulletDamage;

    public int clipSize;
    int numBulletsInClip;

    public float reloadTime;

    // can a weapon have multiple projectiles?
    // Probably. Also a way to swap between them
    public Projectile projectile;
    public Transform bulletSpawnPoint;

    // Use this for initialization
    void Start () {
		
	}
	
	// Update is called once per frame
	void FixedUpdate () {
		if(fireTimeout > 0)
        {
            fireTimeout -= Time.fixedDeltaTime;
        }
	}

    public override void Attack()
    {
        base.Attack();
        Fire();
    }

    void Fire()
    {
        // TODO: Support for shotgun type weapons
        if (CanFire())
        {
            numBulletsInClip--;
            fireTimeout = fireRate;
            Projectile copy = Instantiate(projectile, bulletSpawnPoint.position, Quaternion.identity) as Projectile;
            copy.Create(bulletSpeed, transform.forward, bulletDamage);
        }
    }

    bool CanFire()
    {
        // numBullets > 0
        return (fireTimeout <= 0);
    }

    void Reload()
    {
        numBulletsInClip = clipSize;
    }
}
