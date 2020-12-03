using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RangedWeapon : Weapon {

    
    // amount of time in seconds to wait between shots
    public float fireRate;
    float fireTimeout = 0;

    public float bulletSpeedMultiplier;
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
    // the rotation the gun wants given no spread
    Quaternion intendedRotation;
    public float maxRecoilRotationX;
    public float maxRecoilRotationY;
    // what was the recoilPercent when you last shot?
    // for use with lerping the rotation
    float recoilAtLastShot = 0;

    // can a weapon have multiple projectiles?
    // Probably. Also a way to swap between them
    public Projectile projectile;
    public Transform bulletSpawnPoint;
    Vector3 aimPosition;

    // Audio
    AudioSource audioSource;

    // Aim Arc
    LineRenderer lr;
    public int numAimSegments = 10;

    // Use this for initialization
    void Start () {
        parentRBody = GetComponentInParent<Rigidbody>();
        startOffset = transform.localPosition;
        startRotation = transform.localRotation;
        intendedRotation = startRotation;
        numBulletsInClip = clipSize;

        audioSource = GetComponent<AudioSource>();
        lr = GetComponent<LineRenderer>();

        aimPosition = transform.forward;
        //DrawAimArc();
	}
	
    void Update()
    {
        // move the gun backwards based on the recoilPercent
        transform.localPosition = Vector3.Lerp(startOffset, maxRecoilOffset, recoilPercent * recoilPercent * recoilPercent);
        // this works, but then the bullets fire up into the air instead of horizontally
        // either bullets need to drop or this needs to be visual only
        //transform.localRotation = Quaternion.Lerp(startRotation, Quaternion.Euler(maxRecoilRotationX, 0, 0), recoilPercent * recoilPercent * recoilPercent);
        
        // left to right recoil on the gun. AKA bullet spread
        if (recoilAtLastShot > 0)
        {
            transform.rotation = Quaternion.Lerp(intendedRotation, transform.rotation, recoilPercent / recoilAtLastShot);
        }
        else
        {
            transform.rotation = intendedRotation;
        }

        //DrawAimArc();
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

    public override void UpdateAimPos(Vector3 aimPos)
    {
        // aim pos at the moment only affects the aiming line
        DrawAimArc(aimPos);
        //aimPosition = aimPos;
        // the gun wants to point towards where the aim pos is
        // then recoil controls where it's actually facing
        intendedRotation = Quaternion.LookRotation(aimPos - transform.position);
    }

    void DrawAimArc(Vector3 endPoint)
    {
        // draws an arc that shows where bullets should go
        // maxDist = vel * vel * Mathf.Sin(2 * angleRadians) / gravity
        // maxTravelTime for bullets is 5
        //float maxDistance = bulletSpeed * (5/Time.fixedDeltaTime);
        //Vector3 maxPoint = transform.position + (transform.forward * maxDistance);
        Vector3 maxPoint = endPoint;

        Vector3[] arcArray = new Vector3[numAimSegments +1];

        // <= so you make the line go right to the end
        for (int i = 0; i <= numAimSegments; i++)
        {
            arcArray[i] = Vector3.Lerp(bulletSpawnPoint.position, maxPoint, (float)i / (float)numAimSegments);
        }
        lr.positionCount = numAimSegments + 1;
        lr.SetPositions(arcArray);
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
            if (!audioSource.isPlaying)
            {
                audioSource.Play();
            }
            else
            {
                // if it skips a sound, the sound is too long
                // or the fire rate is too high
                Debug.Log("Skipped sound");
            }
            numBulletsInClip--;
            fireTimeout = fireRate;
            Projectile copy = Instantiate(projectile, bulletSpawnPoint.position, Quaternion.identity) as Projectile;
            copy.Create(StatsManager.Instance().BaseProjectileSpeed * bulletSpeedMultiplier, transform.forward, bulletDamage);
            //copy.Create(bulletSpeed, aimPosition - bulletSpawnPoint.position, bulletDamage);

            Recoil();
            if (numBulletsInClip <= 0 && !reloading)
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
            recoilPercent = Mathf.Min(recoilPercent, 1.0f);
        }

        if (recoilPercent > 0f)
        {
            if (parentRBody != null)
            {
                // add force to the parent (player holding the gun) in the direction away from where the gun is pointing
                parentRBody.AddForce(transform.forward * -1 * recoilPercent * recoilPercent * recoilPercent * recoilForce * parentRecoilMultiplier);
            }
        }

        // get a random spread given the current recoil
        // spread gets larger as recoil gets higher
        float maxAngle = maxRecoilRotationY * recoilPercent;
        float randAngle = Random.Range(-maxAngle, maxAngle);

        transform.localEulerAngles = new Vector3(0, randAngle, 0);
        recoilAtLastShot = recoilPercent;
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

    void OnDrawGizmos()
    {
        Gizmos.color = Color.blue;
        //Gizmos.DrawLine(intendedRotation)
        
        Gizmos.DrawLine(transform.position, transform.position + intendedRotation * Vector3.forward * 10);

    }

}
