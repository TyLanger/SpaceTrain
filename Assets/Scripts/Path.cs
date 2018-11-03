using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Path : MonoBehaviour {

    public Transform[] waypoints;
    int pointIndex = 0;

    float railWidth = 1;

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
            totalPathDst += Vector3.Distance(waypoints[i].position, waypoints[i + 1].position);
        }

        //point index is where the train should currently be going.
        float currentPathDst = 0;
        // not at waypoints[pointIndex] yet, so don't include it
        for (int i = 0; i < pointIndex-2; i++)
        {
            currentPathDst += Vector3.Distance(waypoints[i].position, waypoints[i + 1].position);
        }
        // this ignores any distance the train is between 2 points.
        // Also ignores extra distance from curves.

        return currentPathDst / totalPathDst;
    }

    void SpacePointsEqually()
    {
        // take the existing points, add points in between so that the points are all equal distance apart.
        // Might make the rail texture work better?
        // maybe it's not necessary

        // calculate the distance fro point to point
        // figure out how many points go along there
        // e.x. 2 points are 7 units apart.
        // Want points every 1 unit apart, put in 7 points
        // get the vector from point to point
        // along that vector, put in points every x spacing units
        
        // remove the original points?
        // shoot past the far point?
        // carry over distance from a pair of points to the next?

        // can/should I split this into multiple meshes?
        // then I could only build them when I needed them
        // and destroy them once the train is past them
        // Haven't I run into a problem where if the parent of the mesh is off the screen, the mesh doesn't get drawn?
    }

    void CreateRailMesh()
    {
        Vector3[] verts = new Vector3[waypoints.Length *2];
        int[] tris = new int[6 * (waypoints.Length - 1)];
        //Vector2 uvs = new Vector2[??];
        int vertIndex = 0;
        int triIndex = 0;
        for (int i = 0; i < waypoints.Length; i++)
        {
            Vector3 dirToNext = Vector3.zero;
            // create 2 new points to the side of each point
            if(i == 0)
            {
                // first point
                dirToNext = waypoints[i + 1].position - waypoints[i].position;
            }
            else if (i < waypoints.Length)
            {
                // use the point ahead to find the direction to go
                // use the direction from the last point to this point as well as from this point to the next point
                Vector3 behind = waypoints[i].position - waypoints[i-1].position;
                Vector3 ahead = waypoints[i + 1].position - waypoints[i].position;
                // then average the directions to put the two new point in the middle
                dirToNext = (behind + ahead) * 0.5f;
            }
            else
            {
                //last point
                dirToNext = waypoints[i].position - waypoints[i - 1].position;
            }
            // add the new points perpendicular to the point
            verts[vertIndex] = waypoints[i].position + new Vector3(-dirToNext.z, 0, dirToNext.x) * railWidth;
            verts[vertIndex+1] = waypoints[i].position + new Vector3(dirToNext.z, 0, -dirToNext.x) * railWidth;
            vertIndex += 2;

            tris[triIndex] = i;
            tris[triIndex+1] = i + 2;
            tris[triIndex+2] = i + 1;

            tris[triIndex+3] = i + 1;
            tris[triIndex+4] = i + 2;
            tris[triIndex+5] = i + 3;
            triIndex += 6;


        }
    }

    public Vector3 GetPoint(int index)
    {
        if(index < waypoints.Length-1)
        {
            return waypoints[index].position;
        }

        return waypoints[waypoints.Length-1].position;
    }

    public Vector3 GetNextPoint()
    {
        if (pointIndex < waypoints.Length - 1)
        {
            pointIndex++;
        }
        return waypoints[pointIndex].position;
    }

    public Vector3 GetFirstPoint()
    {
        return waypoints[0].position;
    }

    public int Count
    {
        get
        {
            return waypoints.Length;
        }
    }
    
    void OnDrawGizmos()
    {
        if(waypoints.Length == 0)
        {
            waypoints = GetComponentsInChildren<Transform>();

        }
        foreach (var point in waypoints)
        {
            Gizmos.DrawWireSphere(point.position, 0.1f);
            
        }
        for (int i = 0; i < waypoints.Length-1; i++)
        {
            Gizmos.DrawLine(waypoints[i].position, waypoints[i + 1].position);
        }
    }
    
}
