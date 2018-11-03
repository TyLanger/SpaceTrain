using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour {

    public float moveSpeed = 0.1f;

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {

        transform.position = Vector3.MoveTowards(transform.position, transform.position + new Vector3(Input.GetAxisRaw("Horizontal"), 0, Input.GetAxisRaw("Vertical")), moveSpeed);


	}

    void OnCollisionEnter(Collision col)
    {
        if(col.transform.tag == "Train" && transform.parent != col.transform)
        {
            transform.parent = col.transform;
        }
    }
}
