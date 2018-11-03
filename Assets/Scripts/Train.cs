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
	void FixedUpdate () {
        //transform.position = Vector3.MoveTowards(transform.position, pathTarget, currentSpeed);
        frontWheels.transform.position = Vector3.MoveTowards(frontWheels.transform.position, pathTarget, currentSpeed);
        rearWheels.transform.position = Vector3.MoveTowards(rearWheels.transform.position, rearPathTarget, currentSpeed);
        transform.position = (frontWheels.position + rearWheels.position) * 0.5f;
        transform.forward = frontWheels.position - rearWheels.position;


        if (Vector3.Distance(frontWheels.position, pathTarget) < 0.1f)
        {
            //oldPathTarget = pathTarget;
            frontWheelIndex++;
            pathTarget = path.GetPoint(frontWheelIndex);
            if (frontWheelIndex > path.Count)
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

    public Vector3 PositionInTime(float t)
    {
        // calculate where on the tracks the train will be in t seconds
        float totalDist = currentSpeed * (t / Time.fixedDeltaTime);
        int i = frontWheelIndex;
        Vector3 lastPoint = transform.position;
        while(totalDist > 0)
        {
            Vector3 nextPoint = path.GetPoint(i);
            totalDist -= Vector3.Distance(lastPoint, nextPoint);
            lastPoint = nextPoint;
            i++;
        }
        // doesn't return the exact point at that time
        // returns the next point on the tracks that the train would be going to.
        // the point returned will not be passed by that time if the train stays at the same speed
        Debug.Log("Train Pos in " + t + ": " + lastPoint);
        return lastPoint;
    }

    void ReachedEndOfLine()
    {
        currentSpeed = 0;
    }
}
