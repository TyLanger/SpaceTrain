using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour {

    public float moveSpeed = 0.1f;
    bool canMove = true;

    public Transform spawnPoint;
    public Camera mainCam;

    Health health;

    public Weapon mainWeapon;
    public Weapon secondaryWeapon;
    Weapon currentWeapon;
    bool usingMainWeapon = true;
    Vector3 aimPoint;

    public bool ControllerAiming = false;
    public ThumbCursor thumbCursor;

	// Use this for initialization
	void Start () {
        health = GetComponent<Health>();
        health.OnDeath += OnPlayerDeath;

        if(mainCam == null)
        {
            mainCam = Camera.main;
        }

        currentWeapon = mainWeapon;
        aimPoint = Vector3.forward;

        if(ControllerAiming)
        {
            if(Input.GetJoystickNames().Length == 0)
            {
                // no controller plugged in
                Debug.Log("Trying to use controller, but no controller plugged in");
                ControllerAiming = false;
            }
            else
            {
                thumbCursor.SetPlayer(transform);
            }
        }
	}
	
    void Update()
    {
        if(Input.GetButton("Fire1"))
        {
            // use weapon
            currentWeapon.Attack();
        }
        if(Input.GetAxis("Fire1") != 0)
        {
            // triggers are axis
            currentWeapon.Attack();

        }
        if (Input.GetButtonDown("Reload"))
        {
            currentWeapon.TryReload();
        }
        if(Input.GetButtonDown("Jump"))
        {
            Debug.Break();
        }

        
        // if using a controller, aim to where the cursor is
        // cursor is moved by the right thumbstick
        if (ControllerAiming)
        {
            if (thumbCursor != null)
            {
                aimPoint = thumbCursor.GetAimLocation();
            }
        }
        else
        {
            // Find where the mouse is
            // this gives where the mouse is corrected to the height of the player
            Ray cameraRay = mainCam.ScreenPointToRay(Input.mousePosition);
            Plane aimPlane = new Plane(Vector3.up, transform.position);

            float camerDist;

            if (aimPlane.Raycast(cameraRay, out camerDist))
            {
                aimPoint = cameraRay.GetPoint(camerDist);
            }

            // this version gives the position of the first thing it hits
            // shoots a ray from the camera to where the mouse is
            // so it would get the position of the ground.
            // but it also returns the roof of the train if your mouse is there.
            // Need some sort of mask so it just casts utnil it hits a surface
            // these surfaces would be the floor of the train and the ground. It would ignore the roof of the train and entities.
            // becaues the train is higher, the ray would hit it first if you were aiming at the deck of the train
            // then I could optionally add some height so you're not aiming at the floor, but at chest height at that point.
            GameObject objectHit;
            Vector3 hitPoint = Vector3.zero;
            int layerMask = 1 << LayerMask.NameToLayer("AimSurface"); // only check surfaces you are supposed to

            if(Physics.Raycast(cameraRay, out RaycastHit rayHit, 10000f, layerMask))
            {
                objectHit = rayHit.transform.gameObject;
                aimPoint = rayHit.point;
                aimPoint += new Vector3(0, 0.65f, 0); // add some height

                hitPoint = rayHit.point; // just for visuals
                if (objectHit != null)
                {
                    Debug.DrawLine(mainCam.transform.position, hitPoint, Color.blue, 0.5f);
                }
                
            }
        }

        // look towards where you're aiming
        //transform.forward = aimPoint - transform.position;
        transform.forward = new Vector3(aimPoint.x - transform.position.x, 0, aimPoint.z - transform.position.z);

        // tell your weapon where you're aiming
        currentWeapon.UpdateAimPos(aimPoint);

    }

	// Update is called once per frame
	void FixedUpdate () {
        if (canMove)
        {
            moveSpeed = StatsManager.Instance().BaseMoveSpeed * StatsManager.Instance().PlayerMoveMultiplier;
            transform.position = Vector3.MoveTowards(transform.position, transform.position + new Vector3(Input.GetAxisRaw("Horizontal"), 0, Input.GetAxisRaw("Vertical")), moveSpeed);
        }

	}


    void SwapWeapons()
    {
        if(usingMainWeapon)
        {
            currentWeapon = secondaryWeapon;
        }
        else
        {
            currentWeapon = mainWeapon;
        }
        usingMainWeapon = !usingMainWeapon;
    }

    void Spawn()
    {
        transform.position = spawnPoint.position;
        health.ResetHealth();
        GetComponent<Collider>().enabled = true;
        GetComponent<Rigidbody>().useGravity = true;
        canMove = true;
    }

    void OnPlayerDeath()
    {
        GetComponent<Rigidbody>().useGravity = false;
        GetComponent<Collider>().enabled = false;
        canMove = false;
        // respawn in 3 seconds
        Invoke("Spawn", 3);
    }


    void OnCollisionEnter(Collision col)
    {
        if(col.transform.CompareTag("Train") && transform.parent != col.transform)
        {
            transform.parent = col.transform;
            // train.boardedTrain(this);
            col.gameObject.GetComponent<Train>()?.BoardedTrain(gameObject);
        }
    }
}
