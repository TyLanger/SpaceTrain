using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// https://www.habrador.com/tutorials/math/

public class HalfEdge {

    // the vertex this edge points to
    public Vertex v;

    // the face this edge is a part of
    public Triangle t;

    // the next edge
    public HalfEdge nextEdge;
    // previous
    public HalfEdge prevEdge;
    // the edge going in the opposite direction
    public HalfEdge oppositeEdge;

    // this assumes we have a vertex class that has a reference to the half edge from that vertex
    // and a face (triangle) class with a reference to a half edge that is a part of that face
    public HalfEdge(Vertex v)
    {
        this.v = v;
    }
}
