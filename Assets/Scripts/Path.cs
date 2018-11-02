using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Path : MonoBehaviour {

    public GameObject[] waypoints;
    int pointIndex = 0;

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {

	}

    public float GetPathCompletion()
    {
        // returns how far along the path you are.
        // from 0 to 1

        float totalPathDst = 0;
        for (int i = 0; i < waypoints.Length-1; i++)
        {
            totalPathDst += Vector3.Distance(waypoints[i].transform.position, waypoints[i + 1].transform.position);
        }

        //point index is where the train should currently be going.
        float currentPathDst = 0;
        // not at waypoints[pointIndex] yet, so don't include it
        for (int i = 0; i < pointIndex-2; i++)
        {
            currentPathDst += Vector3.Distance(waypoints[i].transform.position, waypoints[i + 1].transform.position);
        }
        // this ignores any distance the train is between 2 points.
        // Also ignores extra distance from curves.

        return currentPathDst / totalPathDst;
    }

    public Vector3 GetNextPoint()
    {
        if (pointIndex < waypoints.Length - 1)
        {
            pointIndex++;
        }
        return waypoints[pointIndex].transform.position;
    }

    public Vector3 GetFirstPoint()
    {
        return waypoints[0].transform.position;
    }

    void OnDrawGizmos()
    {
        foreach (var point in waypoints)
        {
            Gizmos.DrawWireSphere(point.transform.position, 0.1f);
            
        }
        for (int i = 0; i < waypoints.Length-1; i++)
        {
            Gizmos.DrawLine(waypoints[i].transform.position, waypoints[i + 1].transform.position);
        }
    }
}
