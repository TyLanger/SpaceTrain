using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Train : MonoBehaviour {


    public float baseSpeed = 1;
    float currentSpeed;
    public float maxSpeed = 2;
    public float acceleration = 1;

    /*
    public GameObject[] path;
    int pathIndex = 0;
    */

    public Path path;
    Vector3 pathTarget;


    int currentHp;
    int maxHp;


	// Use this for initialization
	void Start () {
        currentSpeed = baseSpeed;
        pathTarget = path.GetFirstPoint();
	}
	
	// Update is called once per frame
	void Update () {
        transform.position = Vector3.MoveTowards(transform.position, pathTarget, currentSpeed);
        if(Vector3.Distance(transform.position, pathTarget) < 0.1f)
        {
            pathTarget = path.GetNextPoint();
        }

        
	}
}
