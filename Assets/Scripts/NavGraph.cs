using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NavGraph : MonoBehaviour {

    // https://medium.com/@mscansian/a-with-navigation-meshes-246fd9e72424
    // http://blackpawn.com/texts/pointinpoly/default.html
    // http://aigamedev.com/open/tutorials/theta-star-any-angle-paths/

    // This will be attached to the train
    // to be used by agents to pathfind

    // assign some points for the bounds of the graph
    public Transform[] points;
    // TODO find/create points automatically
    public Hole[] interiorHoles;
    public bool destroyInner = true;

    Node[] nodes;
    List<Triangle> listOfTris;
    int[] trianglesByPointIndex;

    public int triNumber = 0;
    public int nodeNumber = 0;

    public int pathStartNode = 0;
    public int pathEndNode = 1;
    public Transform[] testPath;

    [System.Serializable]
    public class Hole
    {
        // workaround to get info about multiple holes in the inspector
        // what I really needed was an array of arrays of transforms
        // what I have is an array of holes which is an array of tranforms
        public Transform[] points;
    }

    void Start()
    {
        
        CreateGraph();
        if((pathStartNode >= 0 && pathStartNode < nodes.Length) && (pathEndNode >= 0 && pathEndNode < nodes.Length))
        {
            Transform[] tempPath = FindPath(nodes[pathStartNode].transform.position, nodes[pathEndNode].transform.position);
            testPath = new Transform[tempPath.Length + 1];
            testPath[0] = nodes[pathStartNode].transform;
            for (int i = 0; i < tempPath.Length; i++)
            {
                testPath[i + 1] = tempPath[i];
            }
        }
    }

    void CreateGraph()
    {
        // concave triangulation
        List<Vector3> pointList = new List<Vector3>();
        nodes = new Node[points.Length];
        List<Vector3> innerPointsList = new List<Vector3>();


        for (int i = 0; i < points.Length; i++)
        {
            // reverse the list so the points are counter clockwise
            pointList.Add(points[points.Length - i - 1].position);
            
        }
        for (int i = 0; i < points.Length/2; i++)
        {
            // reverse the points array so the points are counter clockwise
            // so that the indecies for the vertices match up
            Transform temp = points[i];
            points[i] = points[points.Length - i - 1];
            points[points.Length - i - 1] = temp;
        }
        for (int i = 0; i < interiorHoles.Length; i++)
        {
            for (int j = 0; j < interiorHoles[i].points.Length; j++)
            {
                // add inner points from all holes
                // add them all and the algorithm will figure out what their neighbours are
                innerPointsList.Add(interiorHoles[i].points[j].position);
            }
        }

        listOfTris = TriangulateConcavePolygon(pointList);
        if (innerPointsList.Count > 0)
        {
            listOfTris = TriangulateInner(innerPointsList, listOfTris, pointList.Count);
            // combine the array of outer points and inner points
            // nodes use the index to find the corresponding transform
            // so neeed it in one array
            //points += innerPoints
            Transform[] allPoints = new Transform[points.Length + innerPointsList.Count];
            for (int i = 0; i < points.Length; i++)
            {
                allPoints[i] = points[i];
            }
           
            for (int i = 0; i < interiorHoles.Length; i++)
            {
                for (int j = 0; j < interiorHoles[i].points.Length; j++)
                {
                    allPoints[points.Length + interiorHoles[i].points.Length * i + j] = interiorHoles[i].points[j];

                }
            }

            points = allPoints;
        }
        listOfTris = TriangulateByFlippingEdges(listOfTris);

        // constrained delaunay
        // assume inner points are the constraining points
        if (destroyInner)
        {
            // run addConstraints once for each hole
            // need to run for each hole because it assumes you give it a list of points that are all neighbours and determine the hole
            for (int i = 0; i < interiorHoles.Length; i++)
            {
                // need to make new separate lists
                // each list is one hole
                List<Vector3> interiorPointsList = new List<Vector3>();
                for (int j = 0; j < interiorHoles[i].points.Length; j++)
                {
                    interiorPointsList.Add(interiorHoles[i].points[j].position);
                }
                listOfTris = AddConstraints(listOfTris, interiorPointsList);

            }
        }


        // turn the triangles into nodes that keep track of their neighbours
        nodes = CreateNodes(listOfTris, points.Length);
       
    }

    List<Triangle> AddConstraints(List<Triangle> triangulation, List<Vector3> constraints)
    {

        for (int i = 0; i < constraints.Count; i++)
        {
            // create the constrained edges out of neighbouring vertices
            Vector3 v1 = constraints[i];
            // loop around if too high 
            Vector3 v2 = constraints[(((i + 1) % constraints.Count) + constraints.Count) % constraints.Count];

            // check if this edge already exists in the triangulation.
            // if it does, we are good
            if (IsEdgePartOfTriangulation(triangulation, v1, v2))
            {
                continue;
            }

            // Find all edges in the triangulation that intersect this constraining edge
            List<HalfEdge> intersectingEdges = FindIntersectingEdges(triangulation, v1, v2);

            // remove these intersecting edges by adding new edges
            List<HalfEdge> newEdges = RemoveIntersectingEdges(v1, v2, intersectingEdges);

            // Restore delaunay triangulation
            RestoreDelaunayTriangulation(v1, v2, newEdges);
        }

        // remove the interior triangles


        RemoveSuperfluousTriangles(triangulation, constraints);


        return triangulation;
    }

    // is an edge between p1-p2 a part of an edge in the triangulation?
    bool IsEdgePartOfTriangulation(List<Triangle> triangulation, Vector3 p1, Vector3 p2)
    {
        for (int i = 0; i < triangulation.Count; i++)
        {
            // the vertices positions of the current triangle
            Vector3 tp1 = triangulation[i].v1.position;
            Vector3 tp2 = triangulation[i].v2.position;
            Vector3 tp3 = triangulation[i].v3.position;

            // check if any of the triangle's edges ahve the same coordinates as the constrained edge
            // we have no idea about directio so we have to check both directions
            if ((tp1 == p1 && tp2 == p2) || (tp1 == p2 && tp2 == p1))
            {
                return true;
            }
            if ((tp2 == p1 && tp3 == p2) || (tp2 == p2 && tp3 == p1))
            {
                return true;
            }
            if ((tp3 == p1 && tp1 == p2) || (tp3 == p2 && tp1 == p1))
            {
                return true;
            }
        }

        return false;
    }

    // find all edges of the current triangulation that intersects with the constraint edge between p1 and p2
    List<HalfEdge> FindIntersectingEdges(List<Triangle> triangulation, Vector3 p1, Vector3 p2)
    {
        List<HalfEdge> intersectingEdges = new List<HalfEdge>();

        // 

        // begin at a triangle connected to the first vertex in the constraint edge
        Triangle t = null;

        for (int i = 0; i < triangulation.Count; i++)
        {
            HalfEdge e1 = triangulation[i].halfEdge;
            HalfEdge e2 = e1.nextEdge;
            HalfEdge e3 = e2.nextEdge;

            // does one of these edges include the first vertex in the constraint edge?
            if(e1.v.position == p1 || e2.v.position == p1 || e3.v.position == p1)
            {
                t = triangulation[i];

                break;
            }
        }

        // walk around p1 until we find a triangle with an edge that intersects with the edge p1-p2
        int safety = 0;

        // this is the last edge on the previous triangle we crossed so we know which way to rotate
        HalfEdge lastEdge = null;

        // when we rotate, we might pick the wrong start direction if the edge is on the border,
        // so we can't rotate all the way around
        // if that happens, we have to restart and rotate the other way
        Triangle startTriangle = t;

        bool restart = false;

        while (true)
        {
            safety++;

            if (safety > 10000)
            {
                Debug.Log("Stuck in an infinite loop when finding the start triangle when finding intersecting edges");

                break;
            }

            // check if the current triangle is intersecting with the constraint
            HalfEdge e1 = t.halfEdge;
            HalfEdge e2 = e1.nextEdge;
            HalfEdge e3 = e2.nextEdge;

            // the only edge that can intersect with the constraint is the edge that doesn't include p1, so find it
            HalfEdge eDoesntIncludeP1 = null;

            if (e1.v.position != p1 && e1.prevEdge.v.position != p1)
            {
                eDoesntIncludeP1 = e1;
            }
            else if (e2.v.position != p1 && e2.prevEdge.v.position != p1)
            {
                eDoesntIncludeP1 = e2;
            }
            else
            {
                eDoesntIncludeP1 = e3;
            }

            // is the edge that doesn't include p1 intersect with the constrained edge?
            if(Edge.AreLineSegmentsIntersecting(eDoesntIncludeP1.v.position, eDoesntIncludeP1.prevEdge.v.position, p1, p2))
            {
                // we have found the triangle where we should begin the walk
                break;
            }

            // we have not found the triangle where we should begin the walk, so we should rotate to another triangle which includes p1

            // find the two edges that include p1 so we can rotate across one of them
            List<HalfEdge> includesP1 = new List<HalfEdge>();

            if(e1 != eDoesntIncludeP1)
            {
                includesP1.Add(e1);
            }
            if (e2 != eDoesntIncludeP1)
            {
                includesP1.Add(e2);
            }
            if (e3 != eDoesntIncludeP1)
            {
                includesP1.Add(e3);
            }

            // this is the first rotation we do from the triangle we found at the start, so we rotate in a direction
            if(lastEdge == null)
            {
                // but if we are on the border of the triangulationwe cant just pick a direction because
                // one of the directions may not be valid and end up outside the triangulation
                // this problem could be solved if we add a "supertriangle" covering all points

                lastEdge = includesP1[0];

                // dont go in this direction because then we are outside the triangulation
                // sometimes we may have picked the wrong direction when we rotate from the first triangle
                // and rotated around towards a triangle that's at the border, if so we have to restart and rotate
                // in the other direction

                if(lastEdge.oppositeEdge == null || restart)
                {
                    lastEdge = includesP1[1];
                }

                // trh triangle we rotate to
                t = lastEdge.oppositeEdge.t;
            }
            else
            {
                // move in the direction that doesn't include the last edge
                if(includesP1[0].oppositeEdge != lastEdge)
                {
                    lastEdge = includesP1[0];
                }
                else
                {
                    lastEdge = includesP1[1];
                }

                // if we have hit a border edge, we should have rotated in the other direction when we started at the first triangle
                // so we have to jump bakc
                if(lastEdge.oppositeEdge == null)
                {
                    restart = true;
                    t = startTriangle;
                    lastEdge = null;
                }
                else
                {
                    // the triangle we rotate to
                    t = lastEdge.oppositeEdge.t;
                }
            }
        }

        // march from one triangle to the next in the direction of p2
        // this means we always move across the edge of the triangle that intersects with the constraint
        int safety2 = 0;

        lastEdge = null;

        while(true)
        {
            safety2++;

            if(safety2 > 10000)
            {
                Debug.Log("STuck in an infinite loop when finding intersecting edges");

                break;
            }

            // the three edges belonging to the current triangle
            HalfEdge e1 = t.halfEdge;
            HalfEdge e2 = e1.nextEdge;
            HalfEdge e3 = e2.nextEdge;

            // does this triangle include the last vertex on the constraint edge?
            // if so, we have found all edges that intersect
            if(e1.v.position == p2 || e2.v.position == p2 || e3.v.position == p2)
            {
                break;
            }
            // find which edge that intersects with the constraint
            // more than one edge might intersect, so we have to check if it's not the edge we are coming from
            else
            {
                // save the edge that intersects in case the triangle intersects with two edges
                if(e1.oppositeEdge != lastEdge && Edge.AreLineSegmentsIntersecting(e1.v.position, e1.prevEdge.v.position, p1, p2))
                {
                    lastEdge = e1;
                }
                else if (e2.oppositeEdge != lastEdge && Edge.AreLineSegmentsIntersecting(e2.v.position, e2.prevEdge.v.position, p1, p2))
                {
                    lastEdge = e2;
                }
                else
                {
                    lastEdge = e3;
                }

                // jump to the next triangle by crossing the edge that intersects with the constraint
                t = lastEdge.oppositeEdge.t;

                // save the intersecting edge
                intersectingEdges.Add(lastEdge);
            }

        }

        return intersectingEdges;
    }

    List<HalfEdge> RemoveIntersectingEdges(Vector3 p1, Vector3 p2, List<HalfEdge> intersectingEdges)
    {

        List<HalfEdge> newEdges = new List<HalfEdge>();

        int safety = 0;

        // while some edges still cross the constrained edge, keep going
        while(intersectingEdges.Count > 0)
        {
            safety++;

            if(safety > 10000)
            {
                Debug.Log("Stuck in infinite loop when fixing constrained edges");

                break;
            }

            // remove and edge from the list of edges that intersects the constrained edge
            HalfEdge e = intersectingEdges[0];
            intersectingEdges.RemoveAt(0);

            // the vertices belonging to the two triangles
            Vector2 vK = e.v.GetPos2D_XZ();
            Vector2 vL = e.prevEdge.v.GetPos2D_XZ();
            Vector2 vThirdPos = e.nextEdge.v.GetPos2D_XZ();
            // the vert belonging to the opposite triangle and isn't shared by the current edge
            Vector2 vOppPos = e.oppositeEdge.nextEdge.v.GetPos2D_XZ();

            

            // if the two triangles that share the edge vK and vL do not form a convex quadrilateral,
            // then place the edge back on the list of intersecting edges

            if(!IsQuadrilateralConvex(vK, vL, vThirdPos, vOppPos))
            {
                intersectingEdges.Add(e);
                // add the edge back to the list
                continue;
            }
            else
            {
                // flip the edge like we did when we created the delaunay triangulation so use that code
                FlipEdge(e);

                // the new diagonal is defined by the vertices
                Vector3 vM = e.v.position;
                Vector3 vN = e.prevEdge.v.position;

                // if this new diagonal intersects the constrained edge, add it to the list of intersecting edges
                // vK == e.v.position, vL == e.prevEdge.v.position
                if(Edge.AreLineSegmentsIntersecting(vM, vN, e.v.position, e.prevEdge.v.position))
                {
                    intersectingEdges.Add(e);
                }
                // place it in the list of newly created edges
                else
                {
                    newEdges.Add(e);
                }
            }
        }

        return newEdges;

    }


    // Try to restore the triangulation by flipping the newly created edges
    // this process is similar to when we created the originalf triangulation
    // this step can probably be skipped
    void RestoreDelaunayTriangulation(Vector3 p1, Vector3 p2, List<HalfEdge> newEdges)
    {

    }

    // remove all triangles inside of the contraint
    // this assumes the vertices in the constraint are ordered clockwise
    void RemoveSuperfluousTriangles(List<Triangle> triangulation, List<Vector3> constraints)
    {
        // this assumes we have at least 3 vertices in the constraint because we can't delete triangles inside a line
        if(constraints.Count < 3)
        {
            return;
        }

        // start at a triangle with an edge that shares an edge with the first constraint edge in the list
        // since both are clockwise, we know we are "inside" of the constraint, so we should delete this triangle
        Triangle borderTriangle = null;

        Vector3 constrainedP1 = constraints[0];
        Vector3 constrainedP2 = constraints[1];

        for (int i = 0; i < triangulation.Count; i++)
        {
            HalfEdge e1 = triangulation[i].halfEdge;
            HalfEdge e2 = e1.nextEdge;
            HalfEdge e3 = e2.nextEdge;

            // is any of these edges a constraint?
            if(e1.v.position == constrainedP2 && e1.prevEdge.v.position == constrainedP1)
            {
                borderTriangle = triangulation[i];
                break;
            }
            if(e2.v.position == constrainedP2 && e2.prevEdge.v.position == constrainedP1)
            {
                borderTriangle = triangulation[i];
                break;
            }
            if(e3.v.position == constrainedP2 && e3.prevEdge.v.position == constrainedP1)
            {
                borderTriangle = triangulation[i];
                break;
            }
        }

        if(borderTriangle == null)
        {
            return;
        }

        // find all triangles within the constraint by using a flood fill algorithm
        // these triangles should be deleted
        List<Triangle> trianglesToBeDeleted = new List<Triangle>();
        List<Triangle> neighboursToCheck = new List<Triangle>();

        // start at the triangle we know is within the constraints
        neighboursToCheck.Add(borderTriangle);

        int safety = 0;

        while(true)
        {
            safety++;

            if(safety > 10000)
            {
                Debug.Log("Stuck in infinite loop when deleting extra triangles");

                break;
            }

            // stop if we are out of neighbours
            if(neighboursToCheck.Count == 0)
            {
                break;
            }

            Triangle t = neighboursToCheck[0];
            neighboursToCheck.RemoveAt(0);
            trianglesToBeDeleted.Add(t);

            HalfEdge e1 = t.halfEdge;
            HalfEdge e2 = e1.nextEdge;
            HalfEdge e3 = e2.nextEdge;

            // if the neighbour is not an outer border, meaning no neighbour exists
            // if we have not already visited the neighbour
            // of the edge between the neighbour and this triangle is not a constraint
            // then is's a valid neighbour and we should flood to it
            if(
                e1.oppositeEdge != null &&
                !trianglesToBeDeleted.Contains(e1.oppositeEdge.t) &&
                !neighboursToCheck.Contains(e1.oppositeEdge.t) &&
                !IsAnEdgeAConstraint(e1.v.position, e1.prevEdge.v.position, constraints))
            {
                neighboursToCheck.Add(e1.oppositeEdge.t);
            }
            if (
                e2.oppositeEdge != null &&
                !trianglesToBeDeleted.Contains(e2.oppositeEdge.t) &&
                !neighboursToCheck.Contains(e2.oppositeEdge.t) &&
                !IsAnEdgeAConstraint(e2.v.position, e2.prevEdge.v.position, constraints))
            {
                neighboursToCheck.Add(e2.oppositeEdge.t);
            }
            if (
                e3.oppositeEdge != null &&
                !trianglesToBeDeleted.Contains(e3.oppositeEdge.t) &&
                !neighboursToCheck.Contains(e3.oppositeEdge.t) &&
                !IsAnEdgeAConstraint(e3.v.position, e3.prevEdge.v.position, constraints))
            {
                neighboursToCheck.Add(e3.oppositeEdge.t);
            }

        }

        // delete the triangles
        for (int i = 0; i < trianglesToBeDeleted.Count; i++)
        {
            Triangle t = trianglesToBeDeleted[i];

            // remove from the list of all triangles
            triangulation.Remove(t);

            // in the half edge data structure there's an edge going in the opp direction
            // on the other side of this triangle with a reference to this edge, so remove those
            HalfEdge te1 = t.halfEdge;
            HalfEdge te2 = te1.nextEdge;
            HalfEdge te3 = te2.nextEdge;

            if(te1.oppositeEdge != null)
            {
                te1.oppositeEdge.oppositeEdge = null;
            }
            if (te2.oppositeEdge != null)
            {
                te2.oppositeEdge.oppositeEdge = null;
            }
            if (te3.oppositeEdge != null)
            {
                te3.oppositeEdge.oppositeEdge = null;
            }
        }
    }

    // check if an edge is intersecting with the constraint edge between p1 and p2
    // if so, add it to the list if the edge doesn't exist in the list
    void TryAddEdgeToIntersectingEdges(HalfEdge e, Vector3 p1, Vector3 p2, List<HalfEdge> intersectingEdges)
    {

        // the position the edge is going to
        Vector3 ep1 = e.v.position;
        // the pos the edge is coming from
        Vector3 ep2 = e.prevEdge.v.position;

        // is this edge intersecting with the constraint?
        if(IsEdgeCrossingEdge(ep1, ep2, p1, p2))
        {
            // add it to the list if it isn't already in the list
            for (int i = 0; i < intersectingEdges.Count; i++)
            {
                // in the half edge data structure, there's another edge on the opp side going in the other direction
                // so we have to check both because we want unique edges
                if(intersectingEdges[i] == e || intersectingEdges[i].oppositeEdge == e)
                {
                    // the edge is already in the list
                    return;
                }
            }

            // the edge is not in the list so add it
            intersectingEdges.Add(e);
        }
    }

    bool IsEdgeCrossingEdge(Vector3 a, Vector3 b, Vector3 c, Vector3 d)
    {
        // we will here run into floating point precision issues so we have to be careful
        // to solve that you can first check the end points
        // and modify the line-line intersection algorithm to include a small epsilon

        // first check if the edges are sharing a point, if so, they are not crossing
        if(a==c || a==d ||b==c||b==d)
        {
            return false;
        }

        // then chack if the lines are intersecting
        if(!Edge.AreLineSegmentsIntersecting(a,b,c,d))
        {
            return false;
        }

        return true;
    }

    // is an edge between p1 and p2 a constraint?
    bool IsAnEdgeAConstraint(Vector3 p1, Vector3 p2, List<Vector3> constraints)
    {
        for (int i = 0; i < constraints.Count; i++)
        {
            Vector3 c1 = constraints[i];
            Vector3 c2 = constraints[(((i+1) % constraints.Count) + constraints.Count) % constraints.Count];

            if((p1 == c1 && p2 == c2) || (p2 == c1 && p1 == c2))
            {
                return true;
            }
        }
        return false;
    }

    Node[] CreateNodes(List<Triangle> tris, int size)
    {
        Node[] nodeArray = new Node[size];

        // iterate through the triangles
        // check the vertices for new points
        // if a new point is found, add it to the nodeArray

        // nodes need transforms. Vertices have just positions
        // need to use the index field of the vertices and use that to map it to its original transform

        for (int i = 0; i < tris.Count; i++)
        {
            // find the index of the vertex
            // this index maps to which transform the index points to
            // these indices will also be used to store the nodes. The new nodes will be in the same index as the transform it points to
            int aIndex = tris[i].v1.index;
            int bIndex = tris[i].v2.index;
            int cIndex = tris[i].v3.index;

            //Debug.Log(string.Format("aIndex: {0}, bIndex: {1}, cIndex: {2}", aIndex, bIndex, cIndex));

            // use the already created nodes
            Node a = nodeArray[aIndex];
            Node b = nodeArray[bIndex];
            Node c = nodeArray[cIndex];

            // can check to see if a node has been evaluated
            // if it is in its spot, it has been evaluated, otherwise it hasn't
            //bool aEvaluated = nodeArray[aIndex] != null;
            //bool bEvaluated = nodeArray[bIndex] != null;
            //bool cEvaluated = nodeArray[cIndex] != null;

            // if nodes do not exist yet, create them
            if(a == null)
            {
                a = new Node(points[aIndex]);
                nodeArray[aIndex] = a;
            }
            if(b == null)
            {
                b = new Node(points[bIndex]);
                nodeArray[bIndex] = b;
            }
            if(c == null)
            {
                c = new Node(points[cIndex]);
                nodeArray[cIndex] = c;
            }

            // add all neighbour combinations
            // AddNeighbours() will check for duplicates and not add duplicates

            a.AddNeighbours(b, c);
            b.AddNeighbours(a, c);
            c.AddNeighbours(a, b);

            
        }

        return nodeArray;
    }

    public static List<Triangle> TriangulateConcavePolygon(List<Vector3> points)
    {
        List<Triangle> triangles = new List<Triangle>();

        // if only 3 points, already have triangulated it
        if(points.Count == 3)
        {
            triangles.Add(new Triangle(points[0], points[1], points[2], 0, 1, 2));
            return triangles;
        }

        // Step 1: store the vertices in a list because we need to know the next and prev vertices
        List<Vertex> vertices = new List<Vertex>();

        for (int i = 0; i < points.Count; i++)
        {
            vertices.Add(new Vertex(points[i], i));
        }

        // find the next and previous vertex
        for (int i = 0; i < vertices.Count; i++)
        {
            int nextPos = (((i + 1) % vertices.Count) + vertices.Count) % vertices.Count;
            int prevPos = (((i - 1) % vertices.Count) + vertices.Count) % vertices.Count;

            vertices[i].nextVertex = vertices[nextPos];
            vertices[i].prevVertex = vertices[prevPos];
        }


        // Step 2: Find the concave and convex vertices, and ear vertices
        for (int i = 0; i < vertices.Count; i++)
        {
            vertices[i].CheckIfReflexOrConvex();
        }

        // find ears after we have found if the vertices are reflex or convex
        List<Vertex> earVertices = new List<Vertex>();

        for (int i = 0; i < vertices.Count; i++)
        {
            IsVertexEar(vertices[i], vertices, earVertices);
        }

        // Step 3: Triangulate
        while (true)
        {

            // this means we just have 1 triangle left
            if (vertices.Count == 3)
            {
                // the final triangle
                // shouldn't it be prev, v, next?
                // maybe it doesn't matter
                triangles.Add(new Triangle(vertices[0], vertices[0].prevVertex, vertices[0].nextVertex));
                break;
            }
            // make a triangle of the first ear
            Vertex earVertex = earVertices[0];

            Vertex earVertexPrev = earVertex.prevVertex;
            Vertex earVertexNext = earVertex.nextVertex;

            Triangle newTriangle = new Triangle(earVertex, earVertexPrev, earVertexNext);
            
            triangles.Add(newTriangle);

            // remove the vertex from the list
            earVertices.Remove(earVertex);
            //earVertices.RemoveAt(0);
            vertices.Remove(earVertex);

            // update the previous and next vertex
            earVertexPrev.nextVertex = earVertexNext;
            earVertexNext.prevVertex = earVertexPrev;

            // see if we have found a new ear by investigating the two vertices that was a part of the ear
            earVertexPrev.CheckIfReflexOrConvex();
            earVertexNext.CheckIfReflexOrConvex();

            earVertices.Remove(earVertexPrev);
            earVertices.Remove(earVertexNext);

            IsVertexEar(earVertexPrev, vertices, earVertices);
            IsVertexEar(earVertexNext, vertices, earVertices);
        }

        return triangles;
    }

    public static List<Triangle> TriangulateInner(List<Vector3> interiorPoints, List<Triangle> triangles, int currentIndex)
    {
        // given a list of triangles, "triangles" that determines a concave shape,
        // add all points in interior points to the shape and triangulate the points as they're added.
        for (int i = 0; i < interiorPoints.Count; i++)
        {
            // for each point to be inserted

            // find which triangle it is inside of
            for (int j = 0; j < triangles.Count; j++)
            {
                Triangle t = triangles[j];
                if(t.IsPointInTriangle(t, interiorPoints[i]))
                {
                    // split the triangle into 3 new triangles
                    Triangle t1 = new Triangle(t.v1.position, t.v2.position, interiorPoints[i], t.v1.index, t.v2.index, currentIndex + i);
                    Triangle t2 = new Triangle(t.v2.position, t.v3.position, interiorPoints[i], t.v2.index, t.v3.index, currentIndex + i);
                    Triangle t3 = new Triangle(t.v3.position, t.v1.position, interiorPoints[i], t.v3.index, t.v1.index, currentIndex + i);

                    triangles.Remove(t);

                    triangles.Add(t1);
                    triangles.Add(t2);
                    triangles.Add(t3);

                    // found the triangle this point is inside of
                    // can stop looking now and move on to the next point
                    break;
                }
            }
        }
        return triangles;
    }

    private static void IsVertexEar(Vertex v, List<Vertex> verts, List<Vertex> earVerts)
    {
        // can't be an ear and reflex
        if(v.isReflex)
        {
            return;
        }

        // triangle to check point inside
        Triangle t = new Triangle(v.prevVertex, v, v.nextVertex);


        bool hasPointInside = false;
        Vector3 p = new Vector3();

        for (int i = 0; i < verts.Count; i++)
        {
            if(v.nextVertex == verts[i] || v.prevVertex == verts[i])
            {
                // don't check if the other points of the triangle are inside of it
                continue;
            }

            // we only need to check if a reflex vertex is inside of the triangle
            if(verts[i].isReflex)
            {
                p = verts[i].position;

                if(t.IsPointInTriangle(t, p))
                {
                    hasPointInside = true;
                    break;
                }
            }
        }
        if(!hasPointInside)
        {
            earVerts.Add(v);
        }
    }

    static List<HalfEdge> TransformFromTriangleToHalfEdge(List<Triangle> triangles)
    {
        foreach (Triangle t in triangles)
        {
            t.OrientClockwise();
        }

        // create a list with all possible half edges
        List<HalfEdge> halfEdges = new List<HalfEdge>(triangles.Count * 3);

        for (int i = 0; i < triangles.Count; i++)
        {
            Triangle t = triangles[i];

            HalfEdge he1 = new HalfEdge(t.v1);
            HalfEdge he2 = new HalfEdge(t.v2);
            HalfEdge he3 = new HalfEdge(t.v3);

            he1.nextEdge = he2;
            he2.nextEdge = he3;
            he3.nextEdge = he1;

            he1.prevEdge = he3;
            he2.prevEdge = he1;
            he3.prevEdge = he2;

            // the vertex needs to know of an edge going from it
            he1.v.halfEdge = he2;
            he2.v.halfEdge = he3;
            he3.v.halfEdge = he1;

            // the face the half edge is connected to
            t.halfEdge = he1;

            he1.t = t;
            he2.t = t;
            he3.t = t;

            // add the half edges to the list
            halfEdges.Add(he1);
            halfEdges.Add(he2);
            halfEdges.Add(he3);
        }

        // find the half edges going in the opposite direction
        for (int i = 0; i < halfEdges.Count; i++)
        {
            HalfEdge he = halfEdges[i];

            Vertex goingToVertex = he.v;
            Vertex goingFromVertex = he.prevEdge.v;

            for (int j = 0; j < halfEdges.Count; j++)
            {
                // don't compare with yourself
                if(i == j)
                {
                    continue;
                }

                HalfEdge heOpposite = halfEdges[j];

                // is the edge going between the vertices in the opposite direction?
                if(goingFromVertex.position == heOpposite.v.position && goingToVertex.position == heOpposite.prevEdge.v.position)
                {
                    he.oppositeEdge = heOpposite;
                    break;
                }
            }
        }

        return halfEdges;
    }

    static float IsPointInsideOutsideOrOnCircle(Vector2 aVec, Vector2 bVec, Vector2 cVec, Vector2 pVec)
    {
        // this will simplify how we calculate the determinant
        float a = aVec.x - pVec.x;
        float d = bVec.x - pVec.x;
        float g = cVec.x - pVec.x;

        float b = aVec.y - pVec.y;
        float e = bVec.y - pVec.y;
        float h = cVec.y - pVec.y;

        float c = a * a + b * b;
        float f = d * d + e * e;
        float i = g * g + h * h;

        float determinant = (a * e * i) + (b * f * g) + (c * d * h) - (g * e * c) - (h * f * a) - (i * d * b);

        return determinant;
    }

    // Is a quad convex? Assume no 3 points are colinear and the shape doesn't look like an hourglass
    static bool IsQuadrilateralConvex(Vector2 a, Vector2 b, Vector2 c, Vector2 d)
    {
        bool isConvex = false;

        bool abc = Triangle.IsTriangleClockwise(a, b, c);
        bool abd = Triangle.IsTriangleClockwise(a, b, d);
        bool bcd = Triangle.IsTriangleClockwise(b, c, d);
        bool cad = Triangle.IsTriangleClockwise(c, a, d);

        if(abc && abd && bcd && !cad)
        {
            isConvex = true;
        }
        else if(abc && abd && !bcd && cad)
        {
            isConvex = true;
        }
        else if (abc && !abd && bcd && cad)
        {
            isConvex = true;
        }
        // the opposite sign, which makes everything inverted
        else if (!abc && !abd && !bcd && cad)
        {
            isConvex = true;
        }
        else if (!abc && !abd && bcd && !cad)
        {
            isConvex = true;
        }
        else if (!abc && abd && !bcd && !cad)
        {
            isConvex = true;
        }


        return isConvex;
    }

    static List<Triangle> TriangulateByFlippingEdges(List<Triangle> triangles)
    {

        // step 2: Change the structure from triangles to half edges to make it faster to flip edges
        List<HalfEdge> halfEdges = TransformFromTriangleToHalfEdge(triangles);

        //step 3: flip edges until we have a delaunay triangulation
        int safety = 0;

        int flippedEdges = 0;

        while(true)
        {
            safety += 1;

            if(safety > 100000)
            {
                Debug.Log("Stuck in an endless loop");
                break;
            }

            bool hasFlippedEdge = false;

            // search through all edges to see if we can flip an edge
            for (int i = 0; i < halfEdges.Count; i++)
            {
                HalfEdge thisEdge = halfEdges[i];

                // is this edge sharing an edge, otherwise its a border, adn we can't flip it
                if(thisEdge.oppositeEdge == null)
                {
                    continue;
                }

                // ther vertices belonging to the two triangles
                // c-a are the edge vertices, b belongs to this triangle
                Vertex a = thisEdge.v;
                Vertex b = thisEdge.nextEdge.v;
                Vertex c = thisEdge.prevEdge.v;
                Vertex d = thisEdge.oppositeEdge.nextEdge.v;

                Vector2 aPos = a.GetPos2D_XZ();
                Vector2 bPos = b.GetPos2D_XZ();
                Vector2 cPos = c.GetPos2D_XZ();
                Vector2 dPos = d.GetPos2D_XZ();

                // use th circle test to see if we need to flip this edge
                if(IsPointInsideOutsideOrOnCircle(aPos, bPos, cPos, dPos) < 0f)
                {
                    // are these the two triangles that share this edge forming a convex quad?
                    // otherwise, the edge can't be flipped
                    if(IsQuadrilateralConvex(aPos, bPos, cPos, dPos))
                    {
                        // if the new triangle after a flip is not better, then don't flip it
                        // this will also stop the algorithm from ending in an endless loop
                        if(IsPointInsideOutsideOrOnCircle(bPos, cPos, dPos, aPos) < 0f)
                        {
                            continue;
                        }

                        // flipped the edge
                        flippedEdges += 1;

                        hasFlippedEdge = true;

                        FlipEdge(thisEdge);

                    }
                }
            }
            // we searched through all edges and haven't found an edge to flip, so we have a delaunay triangulation
            if(!hasFlippedEdge)
            {
                Debug.Log("Found a delaunay triangulation");
                break;
            }
        }
        Debug.Log("Flipped " + flippedEdges + " edges");

        // don't have to convert from half edges to triangle because the algorithm will modify the object
        // which belongs to the original triangles, so the triangles have the data we need

        return triangles;
    }

    static void FlipEdge(HalfEdge one)
    {
        // the data we need
        // this edge's triangle
        HalfEdge two = one.nextEdge;
        HalfEdge three = one.prevEdge;
        // the opposite edge's triangle
        HalfEdge four = one.oppositeEdge;
        HalfEdge five = one.oppositeEdge.nextEdge;
        HalfEdge six = one.oppositeEdge.prevEdge;
        // the vertices
        Vertex a = one.v;
        Vertex b = one.nextEdge.v;
        Vertex c = one.prevEdge.v;
        Vertex d = one.oppositeEdge.nextEdge.v;

        // flip

        // change vertex
        a.halfEdge = one.nextEdge;
        c.halfEdge = one.oppositeEdge.nextEdge;

        // change the half edge
        // half edge - half edge connections
        one.nextEdge = three;
        one.prevEdge = five;

        two.nextEdge = four;
        two.prevEdge = six;

        three.nextEdge = five;
        three.prevEdge = one;

        four.nextEdge = six;
        four.prevEdge = two;

        five.nextEdge = one;
        five.prevEdge = three;

        six.nextEdge = two;
        six.prevEdge = four;

        // half edge - vertex connection
        one.v = b;
        two.v = b;
        three.v = c;
        four.v = d;
        five.v = d;
        six.v = a;

        // half edge - triangle connection
        Triangle t1 = one.t;
        Triangle t2 = four.t;

        one.t = t1;
        three.t = t1;
        five.t = t1;

        two.t = t2;
        four.t = t2;
        six.t = t2;

        // opposite edge are not changing

        // triangle connection
        t1.v1 = b;
        t1.v2 = c;
        t1.v3 = d;

        t2.v1 = b;
        t2.v2 = d;
        t2.v3 = a;

        t1.halfEdge = three;
        t2.halfEdge = four;
    }

    Node GetClosestNode(Vector3 pos)
    {
        // not technically closest
        // only returns vertices from the triangle the position is inside of
        // it is possible that the closest node belongs to a different nearby triangle
        // I'm hoping the delaunay triangulation eliminates most or all of those scenarios
        // or, failing that, that it doesn't really matter

        // make sure there are triangles and nodes. Otherwise this is meaningless
        if (listOfTris != null && nodes != null)
        {
            //Vector3 pos = trans.position; // old style of GetClosestNod(trans)
            foreach (Triangle tri in listOfTris)
            {
                // create a new t with updated positions
                // the stored triangles in listOfTris only have their original positions, not updated positions
                // cannot assign to 't' because it is a foreach iteration variable
                // so changed it to tri
                Triangle t = new Triangle(points[tri.v1.index].position, points[tri.v2.index].position, points[tri.v3.index].position, tri.v1.index, tri.v2.index, tri.v3.index);
                if (t.IsPointInTriangle(t, pos))
                {
                    // inside triangle
                    //calculate the distances to the vertices of the triangle
                    float distV1 = Vector3.Distance(t.v1.position, pos);
                    float distV2 = Vector3.Distance(t.v2.position, pos);
                    float distV3 = Vector3.Distance(t.v3.position, pos);

                    if (distV1 <= distV2 && distV1 <= distV3)
                    {
                        // v1 is closest
                        return nodes[t.v1.index];
                    }
                    else if (distV2 <= distV1 && distV2 <= distV3)
                    {
                        // v2 is closest
                        return nodes[t.v2.index];
                    }
                    else
                    {
                        // v3 is closest
                        return nodes[t.v3.index];
                    }
                }
            }
        }
        // position not inside any of the triangles
        return null;
    }

    Node GetClosestNode(Vector3 pos, Triangle t)
    {
        // assume pos is in triangle

        // helper for FindPath for finding the nearest node for the start and end points
        // saves searching every triangle again

        float distV1 = Vector3.Distance(t.v1.position, pos);
        float distV2 = Vector3.Distance(t.v2.position, pos);
        float distV3 = Vector3.Distance(t.v3.position, pos);

        if (distV1 <= distV2 && distV1 <= distV3)
        {
            // v1 is closest
            return nodes[t.v1.index];
        }
        else if (distV2 <= distV1 && distV2 <= distV3)
        {
            // v2 is closest
            return nodes[t.v2.index];
        }
        else
        {
            // v3 is closest
            return nodes[t.v3.index];
        }
    }

    //public Transform[] FindPath(Transform start, Transform end)

    public Transform[] FindPath(Vector3 start, Vector3 end)
    {
        // end point as transform because it will move over time 
        // end point is most likely going to be a specific train car or a player
        // start point can be a vector probably because it won't move before the path is calculated

        // this should probably just be in a generic IsWalkable() method
        bool startOnGraph = false;
        bool endOnGraph = false;

        // the triangles the start and end points are inside of
        Triangle startTri = new Triangle(Vector3.zero, Vector3.zero, Vector3.zero, 0, 0, 0); // initialize them to nothing
        Triangle endTri = startTri;
        // check if start and end points are pathable by this graph
        for (int i = 0; i < listOfTris.Count; i++)
        {
            Triangle t = listOfTris[i];
            t = new Triangle(points[t.v1.index].position, points[t.v2.index].position, points[t.v3.index].position, 0, 0, 0);
            // if you haven't found if the point is on the graph yet, keep checking
            // once you've found it (startOnGraph == true), don't need to check each tri anymore
            if(!startOnGraph && t.IsPointInTriangle(t, start))
            {
                startOnGraph = true;
                startTri = t;
            }
            // finding the start and end are independant. Once you find one, you can quit searching for it
            // but you still need to keep searching for the other
            if (!endOnGraph && t.IsPointInTriangle(t, end))
            {
                endOnGraph = true;
                endTri = t;
            }
            // both points exist on graph
            // can stop checking
            if(startOnGraph && endOnGraph)
            {
                break;
            }

            if(i == listOfTris.Count-1)
            {
                // last item
                Debug.Log(""+startOnGraph + endOnGraph+start+end+" "+listOfTris.Count);
                Debug.Log(""+t.v1.position + t.v2.position + t.v3.position);
            }
        }

        if(!startOnGraph || !endOnGraph)
        {
            Debug.Log("Start or end point not traversable. "+startOnGraph+endOnGraph);
            return null;
        }

        // calculate the path with A*

        // find the node that is closest to the position. We know the triangle it's in from the loop above checking if it exists on the graph.
        Node startNode = GetClosestNode(start, startTri);
        if(startNode == null)
        {
            Debug.Log("Start null");
        }
        // Really want to just end when you reach the triangle the destination is in
        // don't really care about getting to the nearest vertex
        // this still is probably the best way to do it.
        Node goalNode = GetClosestNode(end, endTri);
        if(goalNode == null)
        {
            Debug.Log("Goal null");
        }

        // nodes already evaluated
        // store them here so you know not to evaluate them again
        HashSet<Node> closedSet = new HashSet<Node>();

        // discovered, but not evaluated yet
        // heap structure sorts them beased on g and h scores
        Heap<Node> openSet = new Heap<Node>(listOfTris.Count);

        openSet.Add(startNode);

        while(openSet.Count > 0)
        {
            // current node is the node with the lowest fScore
            // because the openSet heap is sorted, it is the first item
            Node current = openSet.ReturnFirst();

            // add it to the closed set because it is evaluated
            closedSet.Add(current);
            
            if(current == goalNode)
            {
                return ReconstructPath(startNode, current);
            }

            // loop through current's neighbours
            foreach (Node neighbour in current.GetNeighbours())
            {
                //Debug.Log("Neighbour: " + neighbour.transform);
                if(closedSet.Contains(neighbour))
                {
                    // this neighbour has already been evaluated
                    continue;
                }

                int tentative_gScore = current.gCost + CostBetween(current, neighbour);
                //Debug.Log("Tentative: " + tentative_gScore + " currentG: " + current.gCost);
                if(tentative_gScore < neighbour.gCost || !openSet.Contains(neighbour))
                {
                    // if you found a better path for this neighbour
                    // or the neighbour isn't already in the openSet
                    // update all this info for the neighbour
                    neighbour.gCost = tentative_gScore;
                    neighbour.hCost = CostBetween(neighbour, goalNode);
                    //Debug.Log("Neighbour h: " + neighbour.hCost);
                    neighbour.cameFrom = current;

                    // if they weren't in the open set, add them
                    if(!openSet.Contains(neighbour))
                    {
                        openSet.Add(neighbour);
                    }
                    else
                    {
                        // if they were already in the open set
                        // getting here means we found a new gScore that was better
                        // update their position in the openSet
                        openSet.UpdateItem(neighbour);
                    }
                }
            }
            
        }
        Debug.Log("Astar failed. closedset count: "+closedSet.Count);
        //return new Transform[0];
        return null;
    }

    Vector3[] FindLocalPath()
    {
        // return local coordinates for points
        return null;
    }

    int CostBetween(Node start, Node end)
    {
        // guess at what the distance should be
        // ideally, the distance should be Vector3.Distance(start.transform.position, end.transform.position)
        // needs to accomodate nodes that aren't neighbours
        //return (int)start.GetDistBetween(end);

        // maybe should change this all to floats (would need to change g- and hCosts to floats)
        // I was just truncating it before and ran into issues
        // now I'm rounding it
        // difference was g + h = 17 + 0 vs 14 + 2
        // now is 17 + 0 vs 15 + 3
        // that's the difference between truncating vs rounding in long, skinny triangles
        // if the triangle was longer, it could be the difference between 18+0 vs 15+3
        // ties are broken by hCost so in this example it would work out, but is that always true?
        return Mathf.RoundToInt(Vector3.Distance(start.transform.position, end.transform.position));
    }

    Transform[] ReconstructPath(Node start, Node current)
    {
        List<Transform> path = new List<Transform>();

        // ignores the start point
        // don't need the first item of the list of places to go to include where you currently are
        while(current != start)
        {
            path.Add(current.transform);
            current = current.cameFrom;
        }
        path.Reverse();
        
        // convert list to array
        Transform[] pathArray = new Transform[path.Count];
        for (int i = 0; i < path.Count; i++)
        {
            pathArray[i] = path[i];
        }
        return pathArray;
    }

    void OnDrawGizmos()
    {
        if (points != null)
        {
            Gizmos.color = Color.black;
            for (int i = 0; i < points.Length; i++)
            {
                Gizmos.DrawWireSphere(points[i].position, 0.15f);
            }
        }
        if(listOfTris != null)
        {
            Gizmos.color = new Color(0f, 1f, 0.5f);//Color.green;
            for (int i = 0; i < listOfTris.Count; i++)
            {
                // find the position using the points array to get the transforms
                // index is the transform's index in that array
                // getting the transform gives you the current position so the debug moves with the transforms
                Vector3 a = points[listOfTris[i].v1.index].position;
                Vector3 b = points[listOfTris[i].v2.index].position;
                Vector3 c = points[listOfTris[i].v3.index].position;

                Gizmos.DrawWireSphere(a, 0.1f);
                Gizmos.DrawWireSphere(b, 0.1f);
                Gizmos.DrawWireSphere(c, 0.1f);

                Gizmos.DrawLine(a, b);
                Gizmos.DrawLine(b, c);
                Gizmos.DrawLine(c, a);

            }
            if (triNumber >= 0 && triNumber < listOfTris.Count)
            {
                Gizmos.color = Color.blue;
                Vector3 a = listOfTris[triNumber].v1.position;
                Vector3 b = listOfTris[triNumber].v2.position;
                Vector3 c = listOfTris[triNumber].v3.position;

                Gizmos.DrawWireSphere(a, 0.1f);
                Gizmos.DrawWireSphere(b, 0.1f);
                Gizmos.DrawWireSphere(c, 0.1f);

                Gizmos.DrawLine(a, b);
                Gizmos.DrawLine(b, c);
                //Gizmos.DrawLine(c, a);
            }
        }
        if(nodes != null)
        {
            if(nodeNumber >= 0 && nodeNumber < nodes.Length)
            {
                Node n = nodes[nodeNumber];
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(n.transform.position, 0.2f);

                foreach (Node neighbour in n.GetNeighbours())
                {
                    Gizmos.DrawWireSphere(neighbour.transform.position, 0.2f);

                    Gizmos.DrawLine(n.transform.position, neighbour.transform.position);
                }
            }
        }
        if(testPath != null)
        {
            Gizmos.color = Color.blue;
            for (int i = 0; i < testPath.Length-1; i++)
            {
                Gizmos.DrawLine(testPath[i].position, testPath[i + 1].position);
            }
        }
    }

    
}

public class Node : IHeapItem<Node>
{
    Node[] neighbours;
    // keep track of your neighbours and the distance from them
    // also keep track of if the distance is variable and might need to be calculated again
    Dictionary<Node, Edge> neighbourLinks;
    public Transform transform;

    public int gCost;
    public int hCost;
    public Node cameFrom;
    int HeapIndex;

    public Node(Transform trans)
    {
        transform = trans;
        neighbours = new Node[0];
        neighbourLinks = new Dictionary<Node, Edge>();
    }

    public int fCost
    {
        get
        {
            return gCost + hCost;
        }
    }

    public int heapIndex
    {
        get
        {
            return HeapIndex;
        }
        set
        {
            HeapIndex = value;
        }
    }

    public void AddNeighbour(Node n)
    {
        System.Array.Resize<Node>(ref neighbours, neighbours.Length + 1);
        neighbours[neighbours.Length - 1] = n;
        /*
        // make the neighbours array longer and add the new one to it
        Node[] newNeighbours = new Node[neighbours.Length + 1];
        if (neighbours.Length > 0)
        {
            for (int i = 0; i < neighbours.Length; i++)
            {
                newNeighbours[i] = neighbours[i];
            }
        }
        newNeighbours[newNeighbours.Length - 1] = n;
        neighbours = newNeighbours;
        */

        // precalculate the distances between neighbours
        // this may not really be necessary
        // need to also calculate distance between a node and the goal node
        // so precalculating distances doesn't help there
        // could still be useful for ordering neighbours. If you need to find the closest neighbour
        Edge e = new Edge
        {
            distance = Vector3.Distance(transform.position, n.transform.position)
        };
        neighbourLinks.Add(n, e);
    }

    public void AddNeighbours(Node n1, Node n2)
    {
        // commonly adding 2 neighbours at a time
        int newNeighbours = 0;
        bool n1New = false;
        bool n2New = false;
        if (!neighbourLinks.ContainsKey(n1))
        {
            // n1 is not already a neighbour
            newNeighbours++;
            n1New = true;
            neighbourLinks.Add(n1, new Edge
            {
                distance = Vector3.Distance(transform.position, n1.transform.position)
            });
        }
        if (!neighbourLinks.ContainsKey(n2))
        {
            newNeighbours++;
            n2New = true;
            neighbourLinks.Add(n2, new Edge
            {
                distance = Vector3.Distance(transform.position, n2.transform.position)
            });
        }
        if (newNeighbours > 0)
        {
            // instead of tracking the int, could have used
            // n1New?1:0 + n2New?1:0
            System.Array.Resize<Node>(ref neighbours, neighbours.Length + newNeighbours);
        }
        // using these bools so I only have to resize the array once. 
        // seems like doing that twice would be more expensive than having the extra bool flags.
        if(n1New)
        {
            // if newNeighbours == 2, both need to be added. Add this 2 from the end
            // if newNeighbours == 1, only this needs to be added. Add it to the end
            neighbours[neighbours.Length - newNeighbours] = n1;
        }
        if (n2New)
        {
            // this can always be added to the end. If n1 needed to be added, it was added before
            neighbours[neighbours.Length - 1] = n2;
        }
    }

    public void SetNeighbours(Node[] n)
    {
        neighbours = n;
    }

    public Node[] GetNeighbours()
    {
        return neighbours;
    }

    public float GetDistBetween(Node n)
    {
        // find the node in the dict and return the distance
        return neighbourLinks[n].distance;
    }

    struct Edge
    {
        public float distance;
        public bool variableDst;
    }

    public int CompareTo(Node node)
    {
        // first compare fCosts of nodes
        int compare = fCost.CompareTo(node.fCost);

        // if fCosts are equal,
        // compare hCosts
        if(compare == 0)
        {
            compare = hCost.CompareTo(node.hCost);
        }

        // base int.compareTo() is based on larger ints having priority
        // in this case, we want lower costs to have higher priority
        // so negate the comparison
        return -compare;
    }
}
