using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraFollow : MonoBehaviour {


    public Transform focus;
    public Vector3 offset;

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
        // follow the focus target (the train) in the z direction, not in the x direction
        // plus an offset so the camera stays in the air.
        transform.position = new Vector3(transform.position.x, 0, focus.position.z) + offset;
	}
}
