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
    public Transform[] innerPoints;

    Node[] nodes;
    List<Triangle> listOfTris;
    int[] trianglesByPointIndex;

    public int triNumber = 0;
    public int nodeNumber = 0;

    public int pathStartNode = 0;
    public int pathEndNode = 1;
    public Transform[] testPath;

    void Start()
    {
        
        CreateGraph();
        if((pathStartNode >= 0 && pathStartNode < nodes.Length) && (pathEndNode >= 0 && pathEndNode < nodes.Length))
        {
            testPath = FindPath(nodes[pathStartNode].transform, nodes[pathEndNode].transform);
        }
    }

    void CreateGraph()
    {
        // concave triangulation
        List<Vector3> pointList = new List<Vector3>();
        nodes = new Node[points.Length];
        List<Vector3> innerPointsList = new List<Vector3>();
        /*
        for (int i = 0; i < nodes.Length; i++)
        {
            nodes[i] = new Node(points[i]);
        }*/

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
        for (int i = 0; i < innerPoints.Length; i++)
        {
            innerPointsList.Add(innerPoints[i].position);
        }

        listOfTris = TriangulateConcavePolygon(pointList);
        if (innerPoints.Length > 0)
        {
            listOfTris = TriangulateInner(innerPointsList, listOfTris, pointList.Count);
            // combine the array of outer points and inner points
            // nodes use the index to find the corresponding transform
            // so neeed it in one array
            //points += innerPoints
            Transform[] allPoints = new Transform[points.Length + innerPoints.Length];
            for (int i = 0; i < points.Length; i++)
            {
                allPoints[i] = points[i];
            }
            for (int i = 0; i < innerPoints.Length; i++)
            {
                allPoints[points.Length + i] = innerPoints[i];
            }

            points = allPoints;
        }
        listOfTris = TriangulateByFlippingEdges(listOfTris);

        // turn the triangles into nodes that keep track of their neighbours
        nodes = CreateNodes(listOfTris, points.Length);
       
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

    Node GetClosestNode(Transform trans)
    {
        // not technically closest
        // only returns vertices from the triangle the position is inside of
        // it is possible that the closest node belongs to a different nearby triangle
        // I'm hoping the delaunay triangulation eliminates most or all of those scenarios
        // or, failing that, that it doesn't really matter

        // make sure there are triangles and nodes. Otherwise this is meaningless
        if (listOfTris != null && nodes != null)
        {
            Vector3 pos = trans.position;
            foreach (Triangle t in listOfTris)
            {
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

    public Transform[] FindPath(Transform start, Transform end)
    {
        // end point as transform because it will move over time 
        // end point is most likely going to be a specific train car or a player
        // start point can be a vector probably because it won't move before the path is calculated

        // this should probably just be in a generic IsWalkable() method
        bool startOnGraph = false;
        bool endOnGraph = false;
        // check if start and end points are pathable by this graph
        for (int i = 0; i < listOfTris.Count; i++)
        {
            Triangle t = listOfTris[i];
            // if you haven't found if the point is on the graph yet, keep checking
            // once you've found it (startOnGraph == true), don't need to check each tri anymore
            if(!startOnGraph && t.IsPointInTriangle(t, start.position))
            {
                startOnGraph = true;
            }
            // finding the start and end are independant. Once you find one, you can quit searching for it
            // but you still need to keep searching for the other
            if (!endOnGraph && t.IsPointInTriangle(t, end.position))
            {
                endOnGraph = true;
            }
            // both points exist on graph
            // can stop checking
            if(startOnGraph && endOnGraph)
            {
                break;
            }
        }

        if(!startOnGraph || !endOnGraph)
        {
            Debug.Log("Start or end point not traversable. "+startOnGraph+endOnGraph);
            return null;
        }

        // calculate the path with A*

        // find the node that is closest to the position
        Node startNode = GetClosestNode(start);
        if(startNode == null)
        {
            Debug.Log("Start null");
        }
        // Really want to just end when you reach the triangle the destination is in
        // don't really care about getting to the nearest vertex
        // this still is probably the best way to do it.
        Node goalNode = GetClosestNode(end);
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
                if(tentative_gScore < neighbour.gCost || !openSet.Contains(neighbour))
                {
                    // if you found a better path for this neighbour
                    // or the neighbour isn't already in the openSet
                    // update all this info for the neighbour
                    neighbour.gCost = tentative_gScore;
                    neighbour.hCost = CostBetween(neighbour, goalNode);
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

        return (int)Vector3.Distance(start.transform.position, end.transform.position);
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
            Gizmos.color = Color.green;
            for (int i = 0; i < listOfTris.Count; i++)
            {
                Vector3 a = listOfTris[i].v1.position;
                Vector3 b = listOfTris[i].v2.position;
                Vector3 c = listOfTris[i].v3.position;

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
