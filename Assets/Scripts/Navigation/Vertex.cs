using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// https://www.habrador.com/tutorials/math/

public class Vertex {

    public Vector3 position;
    // this vertex links to a transform
    // this index is the index of that transform
    public int index;

    // outgoing halfedge. The halfedge that starts at this vertex
    public HalfEdge halfEdge;

    // which triangle this vertex is a part of
    // pretty sure this does nothing
    //public Triangle triangle;

    // other vertices this vertex is attached to
    public Vertex prevVertex;
    public Vertex nextVertex;

    // attributes this vertex may have
    // relfex is concave
    public bool isReflex;
    public bool isConvex;
    public bool isEar;

    public Vertex(Vector3 position, int _index)
    {
        this.position = position;
        index = _index;
    }

    // Get 2D coordinates of this vertex
    public Vector2 GetPos2D_XZ()
    {
        // convert vector3 to vector 2
        // want the top down view of the point (x and z, not y)
        Vector2 pos2d = new Vector2(position.x, position.z);

        return pos2d;
    }

    public void CheckIfReflexOrConvex()
    {
        isReflex = false;
        isConvex = false;

        Vector2 a = prevVertex.GetPos2D_XZ();
        Vector2 b = GetPos2D_XZ();
        Vector2 c = nextVertex.GetPos2D_XZ();

        if(Triangle.IsTriangleClockwise(a,b,c))
        {
            isReflex = true;
        }
        else
        {
            isConvex = true;
        }
    }
}
