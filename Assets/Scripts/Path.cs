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
