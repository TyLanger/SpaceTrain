using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour {

    public float moveSpeed = 0.1f;

    public Transform spawnPoint;

    Health health;

    public Weapon mainWeapon;
    public Weapon secondaryWeapon;
    Weapon currentWeapon;
    bool usingMainWeapon = true;
    Vector3 aimPoint;

	// Use this for initialization
	void Start () {
        health = GetComponent<Health>();
        health.OnDeath += OnPlayerDeath;

        currentWeapon = mainWeapon;
        aimPoint = Vector3.forward;
	}
	
    void Update()
    {
        if(Input.GetButton("Fire1"))
        {
            // use weapon
            currentWeapon.Attack();
        }
        if(Input.GetButtonDown("Reload"))
        {
            currentWeapon.TryReload();
        }

        // face towards the mouse
        // if using a controler, face where you're moving or where the right stick is facing
        //Input.mousePosition;

        Ray cameraRay = Camera.main.ScreenPointToRay(Input.mousePosition);
        Plane aimPlane = new Plane(Vector3.up, transform.position);

        float camerDist;

        if(aimPlane.Raycast(cameraRay, out camerDist))
        {
            aimPoint = cameraRay.GetPoint(camerDist);
        }

        transform.forward = aimPoint - transform.position;
        //transform.rotation = Quaternion.Euler(0, Mathf.Atan2(transform.position.z - aimPoint.z, transform.position.x - aimPoint.x), 0);

        currentWeapon.UpdateAimPos(aimPoint);

    }

	// Update is called once per frame
	void FixedUpdate () {

        transform.position = Vector3.MoveTowards(transform.position, transform.position + new Vector3(Input.GetAxisRaw("Horizontal"), 0, Input.GetAxisRaw("Vertical")), moveSpeed);


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

    }

    void OnPlayerDeath()
    {
        GetComponent<Collider>().enabled = false;
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
