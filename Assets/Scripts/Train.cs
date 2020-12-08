using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class Train : MonoBehaviour {


    public float baseSpeed = 1;
    static float currentSpeed;
    public float maxSpeed = 2;
    public float acceleration = 1;


    public Path path;
    Vector3 pathTarget;

    // 2 wheel approach
    public Transform frontWheels;
    public Transform rearWheels;
    Vector3 rearPathTarget;
    int frontWheelIndex = 0;
    int rearWheelIndex = 0;
    //Vector3 frontWheelOffset;
    float frontWheelDistanceOffset;
    float frontWheelHeightOffset;

    int trainIndex; // which car you are on the train

    // Boarding
    public BoardingLink[] leftLinks;
    public BoardingLink[] rightLinks;
    public event Action OnBoardable;
    public event Action OnBoardableEarly; 
    //public Delegate Reminder();
    public NavGraph navGraph;

    // Not sure if I need the local sets or not
    //HashSet<GameObject> localFriendlySet;              // set of friendlies (players) on this train car
    static HashSet<GameObject> trainWideFriendlySet;     // all players on the train(over multiple cars)
    //HashSet<GameObject> localHostileSet;               // Same for hostiles(AI-controlled enemies)
    static HashSet<GameObject> trainWideHostileSet;

    public Stockpile[] Stockpiles;

    //public GameObject floorCollider;
    //public GameObject marker;

    // Boarding Testing
    float testTime = 4;

    // Gizmos
    Vector3 newTrainPos;
    Vector3 newFrontPos;
    Vector3 newEndPos;

    void Awake()
    {
        if (trainWideFriendlySet == null)
        {
            trainWideFriendlySet = new HashSet<GameObject>();
        }
        if (trainWideHostileSet == null)
        {
            trainWideHostileSet = new HashSet<GameObject>();
        }

        /*
         * This can maybe be used to populate the nav mesh automatically
         * Without me needing to place each node and move them to the right spot
        // what are extents?
        if (floorCollider != null && marker != null)
        {
            Debug.Log("Entents: " + floorCollider.GetComponent<BoxCollider>().bounds.extents);

            Vector3 center = floorCollider.GetComponent<BoxCollider>().center;
            Vector3 extents = floorCollider.GetComponent<BoxCollider>().bounds.extents;
            

            // spawn something on each corner
            // spawn on top front right corner
            GameObject copy = Instantiate(marker, transform.position + center + extents, Quaternion.identity);
            copy.transform.parent = this.transform;

            // spawn on bottom back left corner (underneath the box)
            copy = Instantiate(marker, transform.position + (center - extents), Quaternion.identity);
            copy.transform.parent = this.transform;
        }
        */
    }

    // Use this for initialization
    void Start () {
        currentSpeed = baseSpeed;
        pathTarget = path.GetFirstPoint();
        rearPathTarget = pathTarget;
        //frontWheelOffset = transform.position - frontWheels.position;
        // check the distance between the front wheel and the train's transform
        // check vertical distance (y) and horizontal distance (x and z)
        frontWheelHeightOffset = transform.position.y - frontWheels.position.y;
        frontWheelDistanceOffset = Vector2.Distance(new Vector2(transform.position.x, transform.position.z), new Vector2(frontWheels.position.x, frontWheels.position.z));

        // ~15 seconds for all 3 cars to be on the track
        //Invoke("TestBoarding", 15);
    }

    void TestBoarding()
    {
        //testing
        (Vector3[] testPoints, _) = GetBoardingLocationsInTime(testTime);
        
        foreach (var item in testPoints)
        {
            Debug.Log("Forward Position: " + item);
        }
        Invoke("Pause", testTime);
        
    }

    void Pause()
    {
        Debug.Break();
    }
	
	// Update is called once per frame
	void FixedUpdate () {
        //transform.position = Vector3.MoveTowards(transform.position, pathTarget, currentSpeed);
        frontWheels.transform.position = Vector3.MoveTowards(frontWheels.transform.position, pathTarget, currentSpeed);
        rearWheels.transform.position = Vector3.MoveTowards(rearWheels.transform.position, rearPathTarget, currentSpeed);
        transform.position = (frontWheels.position + rearWheels.position) * 0.5f;
        transform.forward = frontWheels.position - rearWheels.position;


        if (Vector3.Distance(frontWheels.position, pathTarget) < 0.1f)
        {
            //oldPathTarget = pathTarget;
            frontWheelIndex++;
            pathTarget = path.GetPoint(frontWheelIndex);
            if (frontWheelIndex > path.Count)
            {
                ReachedEndOfLine();
            }
        }
        
        if (Vector3.Distance(rearWheels.position, rearPathTarget) < 0.1f)
        {
            //oldPathTarget = pathTarget;
            rearWheelIndex++;
            if (rearWheelIndex == 1)
            {
                // can't be boarded until the reare wheels are on the track
                // now they are so tell things that are waiting that you can be boarded
                if (OnBoardableEarly != null)
                {
                    OnBoardableEarly();
                }
                if (OnBoardable != null)
                {
                    OnBoardable();
                    //Debug.Log(gameObject + " is boardable", gameObject);
                }
            }
            rearPathTarget = path.GetPoint(rearWheelIndex);
        }

        //transform.forward = Vector3.LerpUnclamped(transform.forward, pathTarget - transform.position, currentSpeed * rotationMultiplier);
    }

    // Set which car this is in the whole train
    public void SetTrainIndex(int index)
    {
        trainIndex = index;
    }

    // Where the train is along the path
    public int GetTrainPathIndex()
    {
        return frontWheelIndex;
    }

    public Vector3 PositionInTime(float t)
    {
        // calculate where on the tracks the train will be in t seconds
        float totalDist = currentSpeed * (t / Time.fixedDeltaTime);

        // new implementation
        return path.GetPointInDistance(frontWheelIndex, totalDist);


        // original implementation
        /*
        int i = frontWheelIndex;
        Vector3 lastPoint = transform.position;
        while(totalDist > 0)
        {
            Vector3 nextPoint = path.GetPoint(i);
            totalDist -= Vector3.Distance(lastPoint, nextPoint);
            lastPoint = nextPoint;
            i++;
        }
        // doesn't return the exact point at that time
        // returns the next point on the tracks that the train would be going to.
        // the point returned will not be passed by that time if the train stays at the same speed
        Debug.Log("Train Pos in " + t + ": " + lastPoint);
        return lastPoint;
        */
    }

    public bool CanBoard(Action reminderMethod, bool needEarly = false)
    {
        // only allow things to try to board if the rear wheel is on the track
        if (rearWheelIndex > 0)
        {
            return true;
        }
        else
        {
            // if not boardable, add the reminderMethod to be called when it is boardable
            // Early
            // was having trouble with the train manager and the AI beoth needing to know when the train was boardable.
            // They both called CanBoard and used the callback
            // but then the AI uses a method in trainManager that needed to know when the train was boardable
            // so the info wasn't updated when the ai needed it, causing errors.
            // this is my stupid workaround
            if (needEarly)
            {
                OnBoardableEarly -= reminderMethod;
                OnBoardableEarly += reminderMethod;
            }
            else
            {
                if (reminderMethod != null) // Do I need this?
                {
                    OnBoardable -= reminderMethod;
                    OnBoardable += reminderMethod;
                }
            }
            return false;
        }
    }

    /// <summary>
    ///  Call this when you board a train car. Self is your gameobject.
    ///  
    /// </summary>
    /// <param name="self"></param>
    public void BoardedTrain(GameObject self)
    {
        // when something enters the train, add it to the collection of things on the train
        // check if the thing is friend or enemy

        // should friendliness be a parameter?

        // if it has an enemy component, assume it's a hostile
        if(self.GetComponent<Enemy>() != null)
        {
            if(!trainWideHostileSet.Contains(self))
            {   
                trainWideHostileSet.Add(self);
            }
            /*
            if(!localHostileSet.Contains(self))
            {
                localHostileSet.Add(self);
            }*/
        }
        // if it has a player component, assume it's a friendly
        else if(self.GetComponent<Player>() != null)
        {
            if (!trainWideFriendlySet.Contains(self))
            {
                trainWideFriendlySet.Add(self);
                Debug.Log("Added a friendly");
            }
            /*
            if (!localFriendlySet.Contains(self))
            {
                localFriendlySet.Add(self);
            }*/
        }
    }

    /// <summary>
    /// Call when you get off the train (onto the ground). Self is your gameObject
    /// </summary>
    /// <param name="self"></param>
    public void LeaveTrain(GameObject self)
    {
        // maybe this isn't a method to call, because it may be forgotten to be called.
        // How can I make it forced? attach this to where the agents swap their parents?

        //this method is for disembarking the train (onto the ground)

        // if it has an enemy component, assume it's a hostile
        if (self.GetComponent<Enemy>() != null)
        {
            if (!trainWideHostileSet.Contains(self))
            {
                trainWideHostileSet.Remove(self);
            }
        }
        // if it has a player component, assume it's a friendly
        else if (self.GetComponent<Player>() != null)
        { 
            if (!trainWideFriendlySet.Contains(self))
            {
                trainWideFriendlySet.Remove(self);
            }
        }
    }

    public HashSet<GameObject> GetAllFriendlyTargets()
    {
        return trainWideFriendlySet;
    }

    public Stockpile[] GetAllStockpiles()
    {
        return Stockpiles;
    }

    public (Vector3[], BoardingLink[]) GetBoardingLocationsInTime(float t, bool rightSide = true)
    {
        // train will have some spots on them for jumping aboard
        Vector3[] boardingSpots = new Vector3[rightSide? rightLinks.Length: leftLinks.Length];

        float totalDist = currentSpeed * (t / Time.fixedDeltaTime);
        // can't use Train.PositionInTime() because it only uses the frontWheelIndex
        // this method should largely replace that one anyway
        Vector3 frontPt = path.GetPointInDistance(frontWheelIndex, totalDist);
        Vector3 rearPt = path.GetPointInDistance(rearWheelIndex, totalDist);
        // this ^ will fail in the first few seconds of the game. Both the front and rear wheels start by moving towards the first point in the path (index 0)
        // index is incremented when they reach a point. The front wheel will reach points and start incrementing soon after starting
        // the rear wheels take some time to reach the first point. 
        // As such, they won't be at their "real" index difference (about 15) for a little while. front wheel will be at 0,1,2,3,4,5,6,7... while rear is still at 0
        // until about front 16, where rear will finally get to 1
        // this messes up the lookahead because you will be looking for 2 points 6 indicies apart instead of the proper 15
        // the problem is even worse for additional train cars. They take even longer to get on the track (~15 seconds with the current 3 cars)

        Vector3 trainForward = frontPt - rearPt;
        float angle = Vector3.SignedAngle(Vector3.forward, trainForward, Vector3.up) * Mathf.Deg2Rad * -1;
        // due to rounding in path.GetPointInDistance()
        // all these positions will be approximate
        // path.GetPointInDistance() returns a location exactly on one of its points. It won't return a location between points.
        // front and rear wheels of the train also don't sit exactly on a point at the same time because they aren't a whole number of points apart.
        // Even if they were, that wouldn't hold true around corners.


        // I thought the bug was in this rotating the train too much
        // but the bug was the rearpt being in the worng spot.
        // so rotating the the front wheel offset might work
        // rotate offset
        // x' = xcos() - ysin()
        // y' = xSin() + yCos()
        //Vector3 rotatedFrontWheelOffset = new Vector3(frontWheelOffset.x * Mathf.Cos(angle) - frontWheelOffset.z * Mathf.Sin(angle), frontWheelOffset.y, frontWheelOffset.x * Mathf.Sin(angle) + frontWheelOffset.z * Mathf.Cos(angle));
        //Vector3 trainPos = frontPt + rotatedFrontWheelOffset;

        // train position will be along the line from the back wheels to the front wheels. 
        // the train position is a distance from the front wheels and a certain height above them
        // these offsets are stored in frontWheelDistanceOffset and frontWheelHeightOffset
        Vector3 trainPos = frontPt - trainForward.normalized * frontWheelDistanceOffset + new Vector3(0, frontWheelHeightOffset, 0);

        /*
        //testing
        newTrainPos = trainPos;
        newFrontPos = frontPt;
        newEndPos = rearPt;
        //end
        */

        // check what the orientation of all the boarding points will be at the given time.
        // each link knows its position in relation to the train. Giving them the train's location and rotation at the given time,
        // they can calculate where they will be.
        if (rightSide)
        {
            for (int i = 0; i < rightLinks.Length; i++)
            {
                boardingSpots[i] = rightLinks[i].GroundPointAtPosition(trainPos, angle);
            }
        }
        else
        {
            for (int i = 0; i < leftLinks.Length; i++)
            {
                boardingSpots[i] = leftLinks[i].GroundPointAtPosition(trainPos, angle);
            }
        }
        // order the points front to back left side, then front to back rights side?
        // or have a separate method of computing left and right side?
        // just a parameter?
        // enemies should know which side they're on to cut down on some calculations
        if(rightSide)
            return (boardingSpots, rightLinks);
        else
            return (boardingSpots, leftLinks);

    }

    void ReachedEndOfLine()
    {
        currentSpeed = 0;
    }

    void OnDrawGizmos()
    {
        /*
        if (rightLinks.Length > 0)
        {
            Gizmos.color = Color.magenta;
            if (newTrainPos != Vector3.zero)
            {
                Gizmos.DrawWireSphere(newTrainPos, 0.5f);
                
            }

            if (newFrontPos != Vector3.zero)
            {
                Gizmos.DrawWireSphere(newFrontPos, 0.3f);
                Gizmos.DrawWireSphere(newEndPos, 0.3f);
                Gizmos.DrawLine(newFrontPos, newEndPos);
            }
        }
        */
    }
}
