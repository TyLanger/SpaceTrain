using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Train : MonoBehaviour {


    public float baseSpeed = 1;
    static float currentSpeed;
    public float maxSpeed = 2;
    public float acceleration = 1;

    //public float rotationMultiplier = 10;

    /*
    public GameObject[] path;
    int pathIndex = 0;
    */

    public Path path;
    Vector3 pathTarget;

    // 2 wheel approach
    public Transform frontWheels;
    public Transform rearWheels;
    Vector3 rearPathTarget;
    int frontWheelIndex = 0;
    int rearWheelIndex = 0;

    int currentHp;
    int maxHp;


	// Use this for initialization
	void Start () {
        currentSpeed = baseSpeed;
        pathTarget = path.GetFirstPoint();
        rearPathTarget = pathTarget;
	}
	
	// Update is called once per frame
	void Update () {
        //transform.position = Vector3.MoveTowards(transform.position, pathTarget, currentSpeed);

        frontWheels.transform.position = Vector3.MoveTowards(frontWheels.transform.position, pathTarget, currentSpeed);
        rearWheels.transform.position = Vector3.MoveTowards(rearWheels.transform.position, rearPathTarget, currentSpeed);
        transform.position = (frontWheels.position + rearWheels.position) * 0.5f;
        transform.forward = frontWheels.position - rearWheels.position;

        if(Vector3.Distance(frontWheels.position, pathTarget) < 0.1f)
        {
            //oldPathTarget = pathTarget;
            frontWheelIndex++;
            pathTarget = path.GetPoint(frontWheelIndex);
            if(frontWheelIndex > path.Count)
            {
                ReachedEndOfLine();
            }
        }
        if (Vector3.Distance(rearWheels.position, rearPathTarget) < 0.1f)
        {
            //oldPathTarget = pathTarget;
            rearWheelIndex++;
            rearPathTarget = path.GetPoint(rearWheelIndex);
        }

        //transform.forward = Vector3.LerpUnclamped(transform.forward, pathTarget - transform.position, currentSpeed * rotationMultiplier);
    }

    void ReachedEndOfLine()
    {
        currentSpeed = 0;
    }
}
