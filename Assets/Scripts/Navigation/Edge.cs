using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// https://www.habrador.com/tutorials/math/


public class Edge {

    public Vertex v1;
    public Vertex v2;

    // is this edge intersecting with another edge?
    public bool isIntersecting = false;

    public Edge(Vertex a, Vertex b)
    {
        v1 = a;
        v2 = b;
    }

    public Edge(Vector3 a, Vector3 b)
    {
        v1 = new Vertex(a);
        v2 = new Vertex(b);
    }

    public Vector2 GetVertex2D(Vertex v)
    {
        // why is this in the edge class?
        return new Vector2(v.position.x, v.position.z);
    }

    public void FlipEdge()
    {
        Vertex temp = v1;
        v1 = v2;
        v2 = temp;
    }

    public static bool AreEdgesIntersecting(Edge a, Edge b)
    {
        return AreLineSegmentsIntersecting(a.v1.position, a.v2.position, b.v1.position, b.v2.position);
    }

    public static bool AreLineSegmentsIntersecting(Vector3 ptA1, Vector3 ptA2, Vector3 ptB1, Vector3 ptB2)
    {
        bool isIntersecting = false;

        // need to check both lines
        // lines could be like -| or T(with a gap between lines) or +
        if(ArePointsOnDifferentSides(ptA1, ptA2, ptB1, ptB2) && ArePointsOnDifferentSides(ptB1, ptB2, ptA1, ptA2))
        {
            isIntersecting = true;
        }

        return isIntersecting;
    }

    private static bool ArePointsOnDifferentSides(Vector3 lineA, Vector3 lineB, Vector3 p1, Vector3 p2)
    {
        bool areOnDiffSides = false;

        Vector3 lineDir = lineB - lineA;

        // find normal by flipping x and z and making z negative
        Vector3 lineNormal = new Vector3(-lineDir.z, lineDir.y, lineDir.x);

        // compute the dot product of the normal and the vector from the start of the line to each point being checked
        float dot1 = Vector3.Dot(lineNormal, p1 - lineA);
        float dot2 = Vector3.Dot(lineNormal, p2 - lineA);

        // if you multiply them and get a negative, they are on different sides
        // in other words, one dot product is negative and the other is positive. 2 negatives or 2 positives mean they are on the same side
        if(dot1 * dot2 < 0f)
        {
            areOnDiffSides = true;
        }

        return areOnDiffSides;

    }
}
