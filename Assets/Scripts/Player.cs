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
                Debug.Log("Trying to us controller, but no controller plugged in");
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
            Ray cameraRay = mainCam.ScreenPointToRay(Input.mousePosition);
            Plane aimPlane = new Plane(Vector3.up, transform.position);

            float camerDist;

            if (aimPlane.Raycast(cameraRay, out camerDist))
            {
                aimPoint = cameraRay.GetPoint(camerDist);
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
        if(col.transform.tag == "Train" && transform.parent != col.transform)
        {
            transform.parent = col.transform;
        }
    }
}
