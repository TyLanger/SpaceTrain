using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// https://www.habrador.com/tutorials/math/

public class Triangle {

    // corners of the triangle
    public Vertex v1;
    public Vertex v2;
    public Vertex v3;

    // if we're using the half edge mesh structure, just need one half edge
    // (I don't really know why)
    public HalfEdge halfEdge;

    public Edge[] edges;

    public Triangle(Vertex a, Vertex b, Vertex c)
    {
        
        TriangleSetup(a, b, c);
    }

    public Triangle(Vector3 a, Vector3 b, Vector3 c, int aIndex, int bIndex, int cIndex)
    {
        TriangleSetup(new Vertex(a, aIndex), new Vertex(b, bIndex), new Vertex(c, cIndex));
    }

    public Triangle(HalfEdge halfEdge)
    {
        this.halfEdge = halfEdge;
    }

    void TriangleSetup(Vertex a, Vertex b, Vertex c)
    {
        v1 = a;
        v2 = b;
        v3 = c;

        edges = new Edge[3];
        edges[0] = new Edge(a, b);
        edges[1] = new Edge(b, c);
        edges[2] = new Edge(c, a);
    }

    // swap the triangle from clockwise to counterclockwise or vice versa
    public void ChangeOrientation()
    {
        Vertex temp = this.v1;
        this.v1 = this.v2;
        this.v2 = temp;
    }

    public void OrientClockwise()
    {
        if(!IsTriangleClockwise(v1.position, v2.position, v3.position))
        {
            ChangeOrientation();
        }
    }

    public static bool IsTriangleClockwise(Vector2 a, Vector2 b, Vector2 c)
    {
        bool isClockwise = true;

        float determinate = a.x * b.y + c.x * a.y + b.x * c.y - a.x * c.y - c.x * b.y - b.x * a.y;

        if(determinate > 0f)
        {
            isClockwise = false;
        }
        return isClockwise;
    }

    public bool IsTriangleIntersecting(Triangle t)
    {
        // is the given triangle intersecting with this one?

        return AreTrianglesIntersecting(this, t);
    }

    public bool AreTrianglesIntersecting(Triangle t1, Triangle t2)
    {
        bool areIntersecting = false;

        //step 1: AABB intersectiong
        // approximate the triangles to rectangles and see if the rectangles intersect
        if(AreIntersectingAABB(t1, t2))
        {
            // boxes are colliding
            // check if lines are colliding
            if(AreAnyLineSegmentsIntersecting(t1, t2))
            {
                areIntersecting = true;
            }
            else if(AreCornersIntersecting(t1, t2))
            {
                areIntersecting = true;
            }
        }

        return areIntersecting;
    }

    bool AreIntersectingAABB(Triangle a, Triangle b)
    {
        bool areIntersecting = true;

        // create bounding boxes

        // Traingle A
        // find the min and max x position of the first triangle out of the 3 points
        float aMinX = Mathf.Min(a.v1.position.x, Mathf.Min(a.v2.position.x, a.v3.position.x));
        float aMaxX = Mathf.Max(a.v1.position.x, Mathf.Max(a.v2.position.x, a.v3.position.x));
        // then the same for z positiongs
        float aMinZ = Mathf.Min(a.v1.position.z, Mathf.Min(a.v2.position.z, a.v3.position.z));
        float aMaxZ = Mathf.Max(a.v1.position.z, Mathf.Max(a.v2.position.z, a.v3.position.z));

        // Triangle B
        float bMinX = Mathf.Min(b.v1.position.x, Mathf.Min(b.v2.position.x, b.v3.position.x));
        float bMaxX = Mathf.Max(b.v1.position.x, Mathf.Max(b.v2.position.x, b.v3.position.x));

        float bMinZ = Mathf.Min(b.v1.position.z, Mathf.Min(b.v2.position.z, b.v3.position.z));
        float bMaxZ = Mathf.Max(b.v1.position.z, Mathf.Max(b.v2.position.z, b.v3.position.z));

        // are the rectangles intersecting?
        // X axis
        if(aMinX > bMaxX)
        {
            areIntersecting = false;
        }
        else if(bMinX > aMaxX)
        {
            areIntersecting = false;
        }
        
        // Z axis
        if(aMinZ > bMaxZ)
        {
            areIntersecting = false;
        }
        else if(bMinZ > aMaxZ)
        {
            areIntersecting = false;
        }

        return areIntersecting;
    }

    bool AreAnyLineSegmentsIntersecting(Triangle a, Triangle b)
    {
        bool areIntersecting = false;

        // loop through all edges
        for (int i = 0; i < a.edges.Length; i++)
        {
            for (int j = 0; j < b.edges.Length; j++)
            {
                if(Edge.AreEdgesIntersecting(a.edges[i], b.edges[j]))
                {
                    areIntersecting = true;
                    // stop outer for loop
                    i = a.edges.Length + 10;
                    break;
                }
            }
        }

        return areIntersecting;
    }

    // Is one triangle inside of the other?
    bool AreCornersIntersecting(Triangle a, Triangle b)
    {
        bool areIntersecting = false;

        // need to check one corner from each triangle
        // doesn't matter what corner
        if(IsPointInTriangle(a, b.v1.position))
        {
            areIntersecting = true;
        }
        else if(IsPointInTriangle(b, a.v1.position))
        {
            areIntersecting = true;
        }

        return areIntersecting;
    }

    public bool IsPointInTriangle(Triangle t, Vector3 p)
    {
        bool isWithinTriangle = false;

        // http://blackpawn.com/texts/pointinpoly/default.html
        // Barycentric Technique
        // using one point of the triangle as an origin
        // the other 2 points to act as vectors to describe a plane
        // then find the vector from the origin to the point
        // in coordintes based on the given axis
        // if the vector is negative, the point is outside the triangle
        // also if it is >1 in either direction, it is past the triangle
        // if u+v is >1, the point is past the edge opposite v1
        // point can be described via
        // P = v1 + u*(v3-v1) + v*(v2-v1)
        // it gets rearranged and then there are 2 unknowns (u and v)
        // Need 2 equations for 2 unknowns so dot both sides by v0 for one and both sides by v1 for the other

        // compute vectors
        // assume v1 is the origin
        // v0 is one axis of the plane. Pointing from v1 to v3
        // v1 is the other axis. Pointing from v1 o v2
        // v2 is the vector from v1 to the point p (the point we are seeing if is inside)
        //Vector3 v0 = t.v3.position - t.v1.position;
        //Vector3 v1 = t.v2.position - t.v1.position;
        //Vector3 v2 = p - t.v1.position;

        Vector2 v0 = t.v3.GetPos2D_XZ() - t.v1.GetPos2D_XZ();
        Vector2 v1 = t.v2.GetPos2D_XZ() - t.v1.GetPos2D_XZ();
        Vector2 v2 = new Vector2(p.x, p.z) - t.v1.GetPos2D_XZ();

        float dot00 = Vector2.Dot(v0, v0);
        float dot01 = Vector2.Dot(v0, v1);
        float dot02 = Vector2.Dot(v0, v2);
        float dot11 = Vector2.Dot(v1, v1);
        float dot12 = Vector2.Dot(v1, v2);

        float inverseDenom = 1 / (dot00 * dot11 - dot01 * dot01);
        float u = (dot11 * dot02 - dot01 * dot12) * inverseDenom;
        float v = (dot00 * dot12 - dot01 * dot02) * inverseDenom;

        // with (u+v == 1), the point is one of the vertices of the triangle
        // I want that to be considered inside the triangle
        // also in the < implementation, one of the points counts as inside the triangle, but the other 2 do not
        isWithinTriangle = (u >= 0) && (v >= 0) && (u + v <= 1);

        return isWithinTriangle;
    }
}
