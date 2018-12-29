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
    bool reloading = false;
    public float autoReloadDelay = 1;
    public float reloadTime;
    Coroutine delayedReloadCoroutine;

    public float recoilForce = 0.35f;
    public float parentRecoilMultiplier = 300;
    public float recoilPercent = 0;
    public float recoilDecayRate = 0.1f;
    Rigidbody parentRBody;
    Vector3 startOffset;
    public Vector3 maxRecoilOffset;
    Quaternion startRotation;
    public float maxRecoilRotationX;


    // can a weapon have multiple projectiles?
    // Probably. Also a way to swap between them
    public Projectile projectile;
    public Transform bulletSpawnPoint;

    // Use this for initialization
    void Start () {
        parentRBody = GetComponentInParent<Rigidbody>();
        startOffset = transform.localPosition;
        startRotation = transform.localRotation;
        numBulletsInClip = clipSize;
	}
	
    void Update()
    {
        transform.localPosition = Vector3.Lerp(startOffset, maxRecoilOffset, recoilPercent * recoilPercent * recoilPercent);
        // this works, but then the bullets fire up into the air instead of horizontally
        // either bullets need to drop or this needs to be visual only
        //transform.localRotation = Quaternion.Lerp(startRotation, Quaternion.Euler(maxRecoilRotationX, 0, 0), recoilPercent * recoilPercent * recoilPercent);
    }

	// Update is called once per frame
	void FixedUpdate () {
		if(fireTimeout > 0)
        {
            fireTimeout -= Time.fixedDeltaTime;
        }

        if(recoilPercent > 0)
        {
            recoilPercent -= recoilDecayRate;
            recoilPercent = Mathf.Max(0, recoilPercent);
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
            Recoil();
            if(numBulletsInClip <= 0 && !reloading)
            {
                delayedReloadCoroutine = StartCoroutine(DelayedReload(autoReloadDelay));
            }
        }
    }

    void Recoil()
    {
        // move the gun back a bit with recoil
        // it will move back to its starting position over time
        // if it moves too far back, it starts to move the player back

        // recoil is not just backwards movement, but also upwards rotation

        if(recoilPercent < 1)
        {
            recoilPercent += recoilForce;
        }

        if (recoilPercent > 0f)
        {
            if (parentRBody != null)
            {
                // add force to the parent (player holding the gun) in the direction away from where the gun is pointing
                parentRBody.AddForce(transform.forward * -1 * recoilPercent * recoilPercent * recoilPercent * recoilForce * parentRecoilMultiplier);
            }
        }
    }

    bool CanFire()
    {
        // numBullets > 0
        return (fireTimeout <= 0) && (numBulletsInClip > 0) && !reloading;
    }

    bool CanReload()
    {
        // don't reload if you're already reloading
        // also don't reload if you're full of ammo
        return !reloading && (numBulletsInClip < clipSize);
    }

    IEnumerator DelayedReload(float delayTime)
    {
        yield return new WaitForSeconds(delayTime);

        StartCoroutine(Reload());
    }

    IEnumerator Reload()
    {

        // if reloading automatically, wait a delay time before starting to reload
        // if the player pressed the reload button, there is no delay; just the reload time
        // auto uses DelayedReload
        // player uses Reload
        if (!reloading)
        {
            reloading = true;
            yield return new WaitForSeconds(reloadTime);
            numBulletsInClip = clipSize;
            reloading = false;
        }
    }

    public override void TryReload()
    {
        if (CanReload())
        {
            if(delayedReloadCoroutine != null)
            {
                // if the auto reload is waiting for the delay time
                // but the player manually reloads,
                // kill the waiting reload
                StopCoroutine(delayedReloadCoroutine);
            }

            Debug.Log("Forced Reload: " + numBulletsInClip + "/" + clipSize);

            StartCoroutine(Reload());
        }
    }
    /*
    void Reload()
    {
        if (!reloading)
        {
            reloading = false;
            numBulletsInClip = clipSize;
        }
    }*/
}
