using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour {

    public float moveSpeed = 0.1f;

    public Transform spawnPoint;

    Health health;

	// Use this for initialization
	void Start () {
        health = GetComponent<Health>();
        health.OnDeath += OnPlayerDeath;
	}
	
	// Update is called once per frame
	void FixedUpdate () {

        transform.position = Vector3.MoveTowards(transform.position, transform.position + new Vector3(Input.GetAxisRaw("Horizontal"), 0, Input.GetAxisRaw("Vertical")), moveSpeed);


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
