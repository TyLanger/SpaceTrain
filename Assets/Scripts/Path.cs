using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Path : MonoBehaviour {

    public Transform[] waypoints;
    int pointIndex = 0;
    Vector3[] evenPoints;

    public float pointSpacing = 2;
    public float railWidth = 1;

    public float maxAngle = 45;

	// Use this for initialization
	void Awake () {
        evenPoints = new Vector3[1];

        // convert given transforms to a list of vector3
        LinkedList<Vector3> trackPoints = new LinkedList<Vector3>();
        foreach (var t in waypoints)
        {
            // add the position of each waypoint to the list
            trackPoints.AddLast(t.position);
        }
        // create corners for each point
        // track the relevent info for the corner. Need to know where it's coming from, where the corner is, and where the corner is going towards
        LinkedListNode<Vector3> from = trackPoints.First;
        LinkedListNode<Vector3> corner = from.Next;
        LinkedListNode<Vector3> to = corner.Next;
        for (int i = 1; i < waypoints.Length-1; i++)
        {
            // skip the first element and the last element because only interested in smoothing corners. The ends aren't corners
            Vector3[] cornerPoints = SmoothCorner(from.Value, corner.Value, to.Value, maxAngle, pointSpacing);
            //LinkedList<Vector3> cornerList = new LinkedList<Vector3>(cornerPoints);
            foreach (var c in cornerPoints)
            {
                // insert each new corner point into the list
                // insert before the to point so I never need to increment
                // each subsequent point goes after the last one, but all before the to point
                trackPoints.AddBefore(to, c);
            }
            // remove the point and replace it with the corner points
            trackPoints.Remove(corner);
            // increment to move to the next corner
            // from is the last of the cornerPoints.
            from = to.Previous;
            corner = to;
            to = to.Next;
        }

        // spacePointsEqually() using this list of points instead of waypoints transforms (need to refactor spacePointsEqually())
        // create mesh
        // when mesh is getting to big, try to split it into 2 meshes. 
        // Split at 3 points that are all going in the same direction so split is seemless

        // convert from a linked list to an array
        Vector3[] trackPointsArray = new Vector3[trackPoints.Count];
        LinkedListNode<Vector3> current = trackPoints.First;
        for (int i = 0; i < trackPointsArray.Length; i++)
        {
            trackPointsArray[i] = current.Value;
            current = current.Next;
        }
        // clear up memory
        // well, shouldn't it be deleted when the function ends?
        trackPoints.Clear();

        SpacePointsEqually(trackPointsArray);

        GetComponent<MeshFilter>().mesh = CreateRailMesh();

        //cornerTestPoints = SmoothCorner(cornerTestTransforms[0].position, cornerTestTransforms[1].position, cornerTestTransforms[2].position, maxAngle, pointSpacing);
    }
	
	// Update is called once per frame
	void Update () {

	}

    // TOOD Rework for evenly spaced points
    // May not have to do any checking. Just use the index of the current point * pointSpacing
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

    void SpacePointsEqually(Vector3[] rawPoints)
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

        float totalDistance = 0;
        for (int i = 0; i < rawPoints.Length-1; i++)
        {
            totalDistance += Vector3.Distance(rawPoints[i], rawPoints[i + 1]);
        }
        //Debug.Log(totalDistance);
        Vector3[] points = new Vector3[(int)(totalDistance / pointSpacing)+2];
        //Debug.Log(points.Length);
        Vector3 dir;

        int pointsIndex = 0;
        Vector3 lastPoint;
        float overshoot = 0;
        for (int i = 0; i < rawPoints.Length-1; i++)
        {
            dir = (rawPoints[i + 1] - rawPoints[i]).normalized;
            //Debug.Log(dir);
            int index = 0;
            do
            {
                lastPoint = rawPoints[i] + dir *(overshoot + pointSpacing * index);
                points[pointsIndex] = lastPoint;
                index++;
                pointsIndex++;

                // assume all points are always moving up
                // therefore you can't have track that runs straight horizontal
            } while (lastPoint.z < rawPoints[i + 1].z);
            // carry over how much you overshot the last point to the next one
            overshoot = Vector3.Distance(lastPoint, rawPoints[i + 1]);
            // remove the last point
            // so that the new points don't go past the original points
            pointsIndex--;
        }
        //Debug.Log("Actual: " + pointIndex);
        evenPoints = points;
        // remove the original points?
        // shoot past the far point?
        // carry over distance from a pair of points to the next?

        // can/should I split this into multiple meshes?
        // then I could only build them when I needed them
        // and destroy them once the train is past them
        // Haven't I run into a problem where if the parent of the mesh is off the screen, the mesh doesn't get drawn?
    }

    Vector3[] SmoothCorner(Vector3 fromPt, Vector3 cornerPt, Vector3 toPt, float maxAngle, float segmentLength)
    {
        // angle between vectors
        // cos(angle) = dot(vectorA, VectorB) / magnitude(VectorA)*magnitude(VectorB)
        //                  toPt
        //              |  /
        //              |A/
        // cornerPt ___ |/
        //              |B
        //              |
        //              fromPt
        // cornerPt is where they intersect
        //
        // I think I want angle A, not B
        // And I think signedAngle gets me that
        // Is B always 180-A for any orientation of these vectors? Probably

        // Can I also assume the vectors are rotated around Vector3.up?
        // Or should I find the cross between the 2 vectors?
        float cornerAngle = Vector3.SignedAngle(cornerPt-fromPt, toPt-cornerPt, Vector3.up);
        //Debug.Log("Corner Angle: " + cornerAngle);
        // how to round? truncate or round? If I truncate, I think sometimes I may end up with 1 too few segments
        // corner angle is negative if it's measuring counter clockwise
        int numNewPoints = Mathf.Abs(Mathf.RoundToInt(cornerAngle / maxAngle));
        if (numNewPoints > 0)
        {
            //Debug.Log("Num new points: " + numNewPoints);
            // this is the real angle all the segments will be at
            // should be close as close to maxAngle as they can get
            float angle = cornerAngle / numNewPoints;
            //Debug.Log("Angle: " + angle);
            // there will be one fewer segments than the number of points
            int numSegments = numNewPoints - 1;

            Vector3[] cornerPoints = new Vector3[numNewPoints];


            // find the direction the points are going from fromPt to cornerPt
            Vector3 currentDir = (cornerPt - fromPt).normalized;
            //Debug.Log("Current Dir: " + currentDir);
            cornerPoints[0] = Vector3.zero;


            for (int i = 1; i <= numSegments; i++)
            {
                // starting from the start,
                // I want a new point segmentLength away at angle angle from the current vector

                // rotate current direction by angle
                // 2D rotation
                // x' = xCos() - zSin()
                // z' = xSin() + zCos()
                float x = currentDir.x;
                float z = currentDir.z;
                float sin = Mathf.Sin(-angle * Mathf.Deg2Rad);
                float cos = Mathf.Cos(-angle * Mathf.Deg2Rad);
                currentDir = new Vector3(x * cos - z * sin, currentDir.y, x * sin + z * cos).normalized;
                //Debug.Log("Current Dir: " + currentDir);
                // then the new point is
                // points are added onto where the last one was
                // all points are segmentLength apart
                cornerPoints[i] = cornerPoints[i - 1] + currentDir * segmentLength;
            }

            // figure out the distance from the first point to the last in a straight line
            float dist = Vector3.Distance(cornerPoints[0], cornerPoints[cornerPoints.Length - 1]);

            // solve the triangle made by the first point, the corner, and the last point
            // Made a right triangle by taking the midpoint between the first point and last point
            // Could have done it with the whole triangle, but I did it this way first
            // TODO: don't cut the triangle in half
            // lowercase are side lengths
            // uppercase are angles
            // a is half the length from point[0] to point[length-1] or half the measured dist
            // in the figure above, cornerAngle is A. We want B from the figure above so sub 180 and then half because we're using half the triangle
            // C is 90 because it's a right triangle
            float a = dist / 2;
            float A = (180 - cornerAngle) / 2;
            float C = 90;
            //Debug.LogFormat("a: {0}, A: {1}, B: {2}, C: {3}", a, A, A, C);

            // calculate the side length using Sine Law
            float distFromCorner = (a * Mathf.Sin(C * Mathf.Deg2Rad)) / Mathf.Sin(A * Mathf.Deg2Rad);
            //Debug.Log("Dist from Corner: " + distFromCorner);

            // dist from the corner tells us how far away from the corner the end points are (it's the same for either end point
            // using the vector from the corner to fromPt, place the first point distFromCorner distance in that direction
            cornerPoints[0] = cornerPt + distFromCorner * (fromPt - cornerPt).normalized;
            // all the points are oriented in relation to the first point.
            // Just add the first point's new position to their position and it translates their positions to world coordinates.
            for (int i = 1; i <= numSegments; i++)
            {
                cornerPoints[i] += cornerPoints[0];
            }

            return cornerPoints;
        }
        else
        {
            Vector3[] singlePtArray = new Vector3[1];
            singlePtArray[0] = cornerPt;
            return singlePtArray;
        }
    }

    Mesh CreateRailMesh()
    {
        Vector3[] verts = new Vector3[evenPoints.Length *2];
        int[] tris = new int[6 * (evenPoints.Length - 1)];
        Vector2[] uvs = new Vector2[verts.Length];
        int vertIndex = 0;
        int triIndex = 0;
        for (int i = 0; i < evenPoints.Length; i++)
        {
            Vector3 dirToNext = Vector3.zero;
            // create 2 new points to the side of each point
            if(i == 0)
            {
                // first point
                dirToNext = evenPoints[i + 1] - evenPoints[i];
            }
            else if (i < evenPoints.Length-1)
            {
                // use the point ahead to find the direction to go
                // use the direction from the last point to this point as well as from this point to the next point
                Vector3 behind = evenPoints[i] - evenPoints[i-1];
                Vector3 ahead = evenPoints[i + 1] - evenPoints[i];
                // then average the directions to put the two new point in the middle
                dirToNext = (behind + ahead) * 0.5f;
            }
            else
            {
                //last point
                dirToNext = evenPoints[i] - evenPoints[i - 1];
            }
            // add the new points perpendicular to the point
            verts[vertIndex] = evenPoints[i] + new Vector3(-dirToNext.z, 0, dirToNext.x) * railWidth - transform.position;
            verts[vertIndex+1] = evenPoints[i] + new Vector3(dirToNext.z, 0, -dirToNext.x) * railWidth - transform.position;

            float completionPercent = i / (float)(evenPoints.Length - 1);
            uvs[vertIndex] = new Vector2(0, completionPercent);
            uvs[vertIndex+1] = new Vector2(1, completionPercent);


            if (i < evenPoints.Length - 1)
            {
                tris[triIndex] = vertIndex;
                tris[triIndex + 1] = vertIndex + 2;
                tris[triIndex + 2] = vertIndex + 1;

                tris[triIndex + 3] = vertIndex + 1;
                tris[triIndex + 4] = vertIndex + 2;
                tris[triIndex + 5] = vertIndex + 3;
                triIndex += 6;
            }
            vertIndex += 2;
        }

        Mesh mesh = new Mesh
        {
            vertices = verts,
            triangles = tris
        };
        mesh.uv = uvs;
        //Debug.Log("Vets: " + mesh.vertexCount+" Triangles: "+mesh.triangles.Length);
        return mesh;
    }

    // Uses Evenly spaced points

    public Vector3 GetPoint(int index)
    {
        if (index < evenPoints.Length - 1)
        {
            return evenPoints[index];
        }

        return evenPoints[evenPoints.Length - 1];
    }

    public Vector3 GetNextPoint()
    {
        if (pointIndex < evenPoints.Length - 1)
        {
            pointIndex++;
        }
        return evenPoints[pointIndex];
    }

    public Vector3 GetFirstPoint()
    {
        return evenPoints[0];
    }

    public int Count
    {
        get
        {
            return evenPoints.Length;
        }
    }
    /* Uses given points
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
    */
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

        if( evenPoints != null)
        {
            if (evenPoints.Length > 1)
            {
                // only draw once the points have been calculated

                foreach (var p in evenPoints)
                {
                    Gizmos.color = Color.red;
                    Gizmos.DrawWireSphere(p, 0.05f);
                }
                for (int i = 0; i < evenPoints.Length - 1; i++)
                {
                    Gizmos.DrawLine(evenPoints[i], evenPoints[i + 1]);
                }
            }
        }

        /*
        Gizmos.color = Color.white;
        // Corner Test
        foreach (var t in cornerTestTransforms)
        {
            Gizmos.DrawWireSphere(t.position, 0.1f);
        }
        for (int i = 0; i < cornerTestTransforms.Length-1; i++)
        {
            Gizmos.DrawLine(cornerTestTransforms[i].position, cornerTestTransforms[i + 1].position);
        }

        if(cornerTestPoints != null)
        {
            if(cornerTestPoints.Length > 1)
            {
                Gizmos.color = Color.blue;
                foreach (var p in cornerTestPoints)
                {
                    Gizmos.DrawWireSphere(p, 0.05f);
                }
                for (int i = 0; i < cornerTestPoints.Length - 1; i++)
                {
                    Gizmos.DrawLine(cornerTestPoints[i], cornerTestPoints[i + 1]);
                }
            }
        }

        if(trackPoints != null)
        {
            if(trackPoints.Count > 0)
            {
                Gizmos.color = Color.blue;
                foreach (var tp in trackPoints)
                {
                    Gizmos.DrawWireSphere(tp, 0.075f);
                }
            }
        }
        */
    }
    
}
