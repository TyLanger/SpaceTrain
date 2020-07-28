using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Enemy : MonoBehaviour {

    // base enemy class
    // other enemies should extend this for more functionality

    public event System.Action OnDeath;
    Health health;
    public bool respawn = true;
    Vector3 respawnPoint;
    bool alive = true;

    public bool usingNewStateMachine = true;

    public float moveSpeed = 0.1f;
    // the range where it will find new targets to attack
    public float aggroRange = 4;
    // the range where it will attemp to atack targets
    public float attackRange = 0.5f;
    bool canMove = true;
    public float outerAttackRange = 0.5f;
    public float preferredAttackRange = 0.5f;
    // move until you are inside the preferred attack range and then you can stop
    // don't start moving again until you are outside the outer attack range.
    // these might be redundant for melee attacks. Need to test them.

    public Vector3 moveTarget;
    public GameObject targetObject;
    public Vector3 targetObjectDebugViewer;
    public Vector3 targetObjectDebugViewerLocal;
    // can I use a setter to also change the value of target?
    // Is this even a good idea? I just want them to point to the same thing
    //GameObject betterTargetObject { get; set { target = this.Vector3} }
    //Vector3 betterTarget { get { betterTargetObject.transform.position}; set; }
    bool targetFound = false;
    // list of all the targets the enemy could have (players, train cars)
    public GameObject[] allTargets;

    // Reference to the boarding link you are trying to go to. Used by BoardTrain state
    public BoardingLink TargetBoardingLink;
    public float TrainBoardDist = 1; // distance you can use the link from. 

    public float lootReach = 2; // arbitrary number. How close you need to be to loot to be able to pick it up. Used by PlunderTrain state
    public float timeBetweenPlunders = 4; // arbitrary time. How often you can attempt to plunder a stockpile for loot. Used by PlunderTrain state
    public Stockpile plunderTarget; // the stockpile you're trying to plunder. Used by PlunderTrain state. Set by SearchForTargetsOnTrain state

    public BoardingLink disembarkLink; // the link you're trying to get to in order to leave the train
    public float disembarkDistance = 1;

    public Vector3 trainRZPoint; // point you have to go to to get on the train
    public bool hasTrainRZPoint = false;
    internal float maxTimeToIntercept = 0;
    bool interceptSuccessful;
    int numBoardableTrainsAtIntercept;

    public Train trainEngine;
    public event System.Action OnTrainBoarded;
    public bool onTrain { get; protected set; }
    // how close the train has to be for the enemy to attempt to board it. (calculate an intercept)
    public float boardingRange = 100;

    public Transform TargetMarker;
    public Transform[] path;
    public int pathIndex = 0;
    public bool pathEnded = false;

    public Weapon weapon;

    private StateMachine _stateMachine;
    public float timeOfIntercept;
    //public Transform plunderTarget; // should this be a Stockpile (instead of a transform)?
    public GameObject hostileTarget;
    public bool canSeeHostileTarget = false;

    int currentInventory;
    int maxInventory = 10;

    // Use this for initialization
    void Start() {
        respawnPoint = transform.position;
        health = GetComponent<Health>();
        health.OnDeath += Die;

        if (trainEngine != null)
        {
            moveTarget = transform.position;

            //Invoke("InterceptTrain", 0.5f);
        }
        else
        {
            // temporary
            // enemy stands still if there is no train
            // i.e. when in the testing scene
            moveTarget = transform.position;
            if (allTargets.Length > 0)
            {
                FindTarget();
            }
        }
    }

    // Rename to Awake when it all works
    void Awake()
    {
        _stateMachine = new StateMachine();


        // States
        var search = new SearchForTrainIntercept(this);
        var moveToTrain = new MoveToRZPoint(this);
        var board = new BoardTrain(this);
        var searchOnTrain = new SearchForTargetsOnTrain(this);
        var plunder = new PlunderTrain(this);
        var disembark = new DisembarkTrain(this);
        var attack = new AttackHostile(this);
        var investigate = new InvestigateHostile(this);

        // Transitions
        // at = addTransition
        At(search, moveToTrain, HasTrainRZPoint());
        At(moveToTrain, board, CanBoard());
        At(moveToTrain, search, MissedWindow());
        At(moveToTrain, search, NewTrainsNowBoardable());
        At(board, searchOnTrain, OnTrain());
        At(searchOnTrain, plunder, HasPlunderTarget());
        At(searchOnTrain, attack, HasHostileTarget());
        At(attack, searchOnTrain, HostileNoLongerExists());
        At(attack, investigate, HostileOutOfSight());   // not sure if investigate should exist.
        At(plunder, disembark, InventoryFullOfLoot());

        At(attack, searchOnTrain, LostHostileTarget()); // maybe want this?
        //At(attack, something, NotOnTrain());          // what happens if you're no longer on the train while in the attack state?
        // also, you can't currently attack players while they're off the train

        // set base state
        _stateMachine.SetState(search);

        // Helper Methods
        void At(IState from, IState to, Func<bool> condition) => _stateMachine.AddTransition(from, to, condition);

        // assuming rzPoint being all 0s means it hasn't been set yet
        //TODO: have it expire. After you swap states, it is probably no longer relevent
        Func<bool> HasTrainRZPoint() => () => hasTrainRZPoint;
        // you have a rz point AND you're at it OR you're not at the rz point yet, but you are close enough to the boarding link. Maybe don't need the second compare
        Func<bool> CanBoard() => () => HasTrainRZPoint()() && ((Vector3.Distance(transform.position, trainRZPoint) < TrainBoardDist)); //|| (Vector3.Distance(transform.position, TargetBoardingLink.groundPoint.position) < TrainBoardDist));
        Func<bool> MissedWindow() => () =>
        {
            // time of intercept should be calculated when the intercept is first calculated (but it's not ATM)
            float predictedTimeRemaining = Vector3.Distance(transform.position, trainRZPoint) * moveSpeed;
            if (Time.time + predictedTimeRemaining > timeOfIntercept)
            {
                return false; // return true; // Does not work ATM. Change back to true when it does
            }
            return false;
        };
        Func<bool> NewTrainsNowBoardable() => () =>
        {
            if(!interceptSuccessful)
            {
                if(TrainManager.Instance.indexBoardable > numBoardableTrainsAtIntercept)
                {
                    // try to find a intercept again. There are more available trains this time
                    // should probably also put a check for how many trains you actually want to check.
                    // if you're only checking 1 train back, does it matter if train 5,6,7,8,9 are now boardable?
                    return true;
                }
            }
            return false;
        };
        // which OnTrain is more trustworthy? Both for redundancy?
        //Func<bool> OnTrain() => () => onTrain;
        Func<bool> OnTrain() => () => transform.parent != null && transform.parent.CompareTag("Train");
        Func<bool> HasPlunderTarget() => () => OnTrain()() && plunderTarget != null;
        Func<bool> HasHostileTarget() => () => OnTrain()() && hostileTarget != null;
        // don't necessarily need to be on the train to attack, but this enemy does
        // other types of enemies could extend this and change the requirement
        Func<bool> HostileNoLongerExists() => () => hostileTarget == null;
        //Func<bool> HostileOutOfSight() => () => !CanSee(hostileTarget); // using a different method than this one. Either use that method or retool this one to work
        Func<bool> HostileOutOfSight() => () => !canSeeHostileTarget; // using a different method than this one. Either use that method or retool this one to work
        Func<bool> InventoryFullOfLoot() => () => currentInventory >= maxInventory;

        //unused but may be helpful
        Func<bool> AtEndOfPath() => () => pathEnded;
        Func<bool> LostHostileTarget() => () => AtEndOfPath()() && HostileOutOfSight()();

    }

    void Update()
    {
        _stateMachine.Tick();
    }

    // Update is called once per frame
    void FixedUpdate() {
        if (!alive)
            return;

        if(targetObject != null)
        {
            targetObjectDebugViewer = targetObject.transform.position;
            targetObjectDebugViewerLocal = targetObject.transform.localPosition;

        }
        #region STATEMACHINE
        /*
        // STATE MACHINE STYLE
        // states should handle all logic. This jsut updates values

        Vector3 lookTarget = Vector3.forward;
        Vector3 moveTarget = Vector3.zero;
        // does the enemy need to know what their target is?
        // whether it is a location or the player or the train?
        // currently use a mix of target and targetObject
        // where target is the default and also used for positions (where no object exists) to intercept the train
        // targetObject is used to path to the player or a train car when on board
        // only ever call targetObject.transform.position
        // except FindPath requires transforms.
        // I don't think that's a technical limitation of findPath, but just how it currently is
        // actually, it probably needs to be changed to really work with intercepting the train. That method only returns a V3 and doesn't seem like creating an object would be prudent
        // but even when I do add A* to the ground off the train, won't the agents still want to move to a spot that isn't a node?
        // or will there be frequent enough nodes along the tracks that they can move to the rendezvous point and a node at the same time. i.e. there exists a node that's near enough the real rz point

        // chaseState.MoveTowards(target)
        // moveTarget = target;

        if(moveTarget != null)
        {
            transform.position = Vector3.MoveTowards(transform.position, moveTarget, moveSpeed);
        }

        //except this will use A* so it will have a sequence of locations
        // in chase state:
        // Enemy.MoveTo(target);
        // in here:
        // MoveTo(Vec3 target)
        // path = Astar.GetPath(target, current);
        if (path != null)
        {
            transform.position = Vector3.MoveTowards(transform.position, path[pathIndex].position, moveSpeed);

            // check if you've reached the target position
            // if you have, start moving towards the next node
            if (Vector3.Distance(transform.position, path[pathIndex].position) < 1f)
            {
                // watch for out of bounds
                if (pathIndex < path.Length - 1)
                {
                    pathIndex++;
                }
            }
        }

        // do I need canMove?
        // what happens if this enemy gets rooted?
        // go to rootedState?
        // where it sets speed to 0?
        // should I have:
        // if(moveSpeed > 0)
        // does this even save any performance? Probably not

        // how does the enemy attack?
        // call attack() from AttackState?

        // not necessarily always looking where you are moving
        transform.forward = lookTarget - transform.position;
        if (weapon != null)
        {
            weapon.UpdateAimPos(lookTarget);
        }
        */
        #endregion STATEMACHINE


        if (usingNewStateMachine)
        {
            /// Movement
            /// Always just move towards move target
            /// if you have a path, set move target to the next link on the path
            /// If you don't have a path, the AI will set your move target. So just follow whatever you have
            
            if(path != null && !pathEnded)
            {
                if (pathIndex < path.Length)
                {
                    moveTarget = path[pathIndex].position;
                    // are you close to the node? If so, move on to the next node
                    if (Vector3.Distance(transform.position, path[pathIndex].position) < 1f)
                    {
                        if (pathIndex < path.Length - 1)
                        {
                            pathIndex++;
                        }
                        else
                        {
                            // at end of path
                            // maybe there should be an event?
                            pathEnded = true;
                        }
                    }
                }
            }

            transform.position = Vector3.MoveTowards(transform.position, moveTarget, moveSpeed);

            /// facing
            /// Face where you're moving
            /// or where you're aiming if you have a hostile target
            /// AttackHostile sets the canSeeHostileTarget flag
            if(canSeeHostileTarget)
            {
                //transform.forward = hostileTarget.transform.position - transform.position;
                if (weapon != null)
                {
                    weapon.UpdateAimPos(hostileTarget.transform.position);
                }
            }
            else
            {
                //transform.forward = moveTarget - transform.position;
            }


        }
        else
        {
            // target is found when a target from the list is within aggro range
            if (targetFound)
            {
                if (TargetMarker != null)
                {
                    TargetMarker.position = targetObject.transform.position;
                }

                // Train is on layer "Train"
                // Player is on layer "Damageable"
                // Is there a reason they should be different layers? I don't know if they should or shouldn't be
                // When the enemy is chasing the player and the player puts the train in between them, the enemy fires a ray towards the player and hits the train
                // so it attacks the train even though it's trying to find the player (and it can't path find yet so it just moves straight towards the player)
                RaycastHit hit;
                // if you can see your target and it's within attack range, attack it
                if (Physics.Raycast(transform.position, (targetObject.transform.position - transform.position).normalized, out hit, attackRange, LayerMask.GetMask("Train") | LayerMask.GetMask("Damageable")))
                {

                    if (hit.distance < attackRange)
                    {
                        Attack();
                    }
                }

                if (canMove)
                {
                    if (path == null)
                    {
                        transform.position = Vector3.MoveTowards(transform.position, targetObject.transform.position, moveSpeed);
                    }
                    else
                    {
                        // move via the A* path instead
                        transform.position = Vector3.MoveTowards(transform.position, path[pathIndex].position, moveSpeed);
                        // < 1f is arbitrary
                        // it's just close to the point, but doesn't have to be right on it
                        // instead of distance, being able to see the next point might be a better solution
                        // needs to be larger because the point is on the ground, but measures to the center of the agent
                        if (Vector3.Distance(transform.position, path[pathIndex].position) < 1f)
                        {
                            // watch for oout of bounds
                            if (pathIndex < path.Length - 1)
                            {
                                pathIndex++;
                            }
                            else
                            {
                                // at end of path
                                // maybe there should be an event?
                                pathEnded = true;
                            }
                        }
                    }
                }
                // face where you're going
                // should be towards the direction you're facing while pathing towards the target
                transform.forward = targetObject.transform.position - transform.position;
                if (weapon != null)
                {
                    weapon.UpdateAimPos(targetObject.transform.position);
                }
            }
            else
            {
                if (TargetMarker != null)
                {
                    TargetMarker.position = moveTarget;
                }

                if (canMove)
                {
                    transform.position = Vector3.MoveTowards(transform.position, moveTarget, moveSpeed);
                }
                // face where you're going
                if (moveTarget != transform.position)
                {
                    transform.forward = moveTarget - transform.position;
                }
            }
        }
    }

    public void SetLookTarget(Vector3 lookPos)
    {
        //lookTarget = lookPos;
    }

    public float GetSightDistance(GameObject lookTarget)
    {
        // can you see target and how far away is it?

        Vector3 origin = transform.position + Vector3.up; // +eye pos
        Vector3 direction = lookTarget.transform.position - transform.position;
        RaycastHit hit;
        float maxDist = 100; // max sight distance
        // should only return a hit if it hits a damageable object first
        //LayerMask mask = LayerMask.NameToLayer("Damageable");
        // Should I use the layer of lookTarget? Seems to make sense
        LayerMask targetMask = lookTarget.layer;


        // seeing a target is more than just seeing the center of them. Fire multiple rays at the whole width of the target?
        // maybe you also need to see a minimum amount of them. i.e. not just see one pixel of their shoulder.
        if (Physics.Raycast(origin, direction, out hit, maxDist, targetMask))
        { 
            // does it return 0 if it didn't hit the target? Like if it hit a wall before the player
            return hit.distance;
        }

        // fail state if you can't see it
        return -1;
    }

    public bool CanSee(GameObject lookTarget)
    {
        //if(raycast)



        return true;
    }

    public void StartMovingToRZPoint()
    {
        // move in a straight line to the move target. Doesn't use pathfinding
        moveTarget = trainRZPoint;
    }

    public void HoldPosition()
    {
        Transform[] myTrans = new Transform[1];
        myTrans[0] = transform;
        path = myTrans;
        // just wanted the agent to stand still
    }

    public void SetMoveTarget(Vector3 targetPos)
    {
        // how do I check that I'm not just calculating the same path every frame?
        //if(targetPos != currentTargetPos)
        path = trainEngine.navGraph.FindPath(transform.position, targetPos);
        pathEnded = false;
        pathIndex = 0;
        if(path == null)
        {
            // this means the targetPos is not on the graph
            // so it's on the ground where there is no path.
            // That's a big ASSume
        }
    }

    public void PathToTarget()
    {
        // calculate a path to the current target (targetObject)
        // how can I check if the target is stationary so I don't have to calculate a new path that is the same as the old path?
        // GetComponent<Player> suggests movement
        // GetComponent<ShippingContainer> suggests stationary?
        // GetComponent<IMoveable> ?
        // Maybe keep track of the last time you changed your path. 
        // And then make the time between paths increase as you swap paths. 
        SetMoveTarget(targetObject.transform.position);
    }

    void Respawn()
    {
        health.ResetHealth();
        transform.position = respawnPoint;
        GetComponent<Collider>().enabled = true;
        GetComponent<MeshRenderer>().enabled = true;
        GetComponent<Rigidbody>().useGravity = true;
        alive = true;
    }

    void Die()
    {
        alive = false;
        // if attached to a train, forget about it
        transform.parent = null;
        if(OnDeath != null)
        {
            // tell any listeners that you died
            OnDeath();
        }
        if(respawn)
        {
            GetComponent<Rigidbody>().useGravity = false;
            GetComponent<Collider>().enabled = false;
            GetComponent<MeshRenderer>().enabled = false;
            Invoke("Respawn", 3);
        }
        else
        {
            // if it's not gonna respawn, just destroy it.
            // maybe it should be recycled into a pool?
            // depends how many enemies there gets to be
            Destroy(gameObject);
        }
    }

    public void Attack()
    {
        weapon.Attack();
    }

    internal void BreakOpenPlunderTarget()
    {
        if(plunderTarget != null)
        {
            plunderTarget.remainingHealth -= 10; // arbitrary. should be decremented by a variable like stockpileBreakingStrength
        }
    }

    internal void LootStockpile()
    {
        
        if(plunderTarget != null)
        {
            if(plunderTarget.remainingHealth <= 0)
            {
                // no health means it's open/unlocked
                plunderTarget.remainingHealth -= 10;
                // currentLoot += 10;
            }
        }
    }

    void FindTarget()
    {
        // is there something within attack range to attack?
        // attack that
        // else 
        // find a new target and path to it
        float shortestDst = 0;
        float currentDst = 0;
        for (int i = 0; i < allTargets.Length; i++)
        {
            currentDst = Vector3.Distance(allTargets[i].transform.position, transform.position);
            // this will attack the last target in the list within the range
            // if the first target is inside the aggro range and so is the 8th target, it will attack the 8th target
            if(currentDst < aggroRange)
            {
                targetObject = allTargets[i];
                //attack that
                targetFound = true;
            }
            else if(currentDst < shortestDst || shortestDst == 0)
            {
                // if nothing within attack range, target is the closest thing
                shortestDst = currentDst;
                targetObject = allTargets[i];
            }
        }
    }

    /// <summary>
    /// Intercept the train to try to board it
    /// </summary>
    public void InterceptTrain()
    {
        // try to get to the track ahead of the train

        // check if you can board
        // if not, run this method again when it is ready to board
        if(!trainEngine.CanBoard(InterceptTrain))
        {
            Debug.Log(trainEngine.name + " is not ready to board");
            return;
        }
        
        InterceptInfo info = TrainManager.Instance.TryInterceptTrain(trainEngine, 1, 1, 10, 1, transform.position, moveSpeed);

        if(info.train == null)
        {
            Debug.Log("No train info.");
            //info = TrainManager.Instance.TryInterceptTrain(trainEngine, 1, 5, 25, 5, moveSpeed);
        }

        trainRZPoint = info.position;
        trainEngine = info.train;
        TargetBoardingLink = info.link;
        hasTrainRZPoint = true;
        interceptSuccessful = true;

        if (!info.successful)
        {
            // success being false means you didn't find an intercept and are instead going as close as you can
            // need to try again to find an intercept
            // wait some time.
            if(!info.allTrainsChecked)
            {
                interceptSuccessful = false;
                numBoardableTrainsAtIntercept = TrainManager.Instance.indexBoardable;
            }
        }
    }

    internal void BoardTrain()
    {
        transform.position = TargetBoardingLink.GetOnBoardPosition();
        OnTrainBoarded?.Invoke();
        onTrain = true;
        // finding targets should be handled by SearchForTargetsOnTrain
    }

    internal void Disembark()
    {
        Debug.Log(gameObject + " is trying to jump off train");
        transform.position = disembarkLink.groundPoint.position; // might not be quite this easy.
    }

    internal Vector3[] GetDisembarkPointsAsPositions()
    {
        // should this method be in the train?

        // get all boarding links
        // left links aren't hooked up yet
        var links = trainEngine.rightLinks;
        Vector3[] points = new Vector3[links.Length];

        for (int i = 0; i < links.Length; i++)
        {
            points[i] = links[i].trainPoint.position;
        }

        return points;
    }

    /// <summary>
    ///  Get to within attack range of the train so you can attack from a distance
    /// </summary>
    void ApproachTrain()
    {
        // Warning: outdated
        Vector3 interceptPoint;
        float closestDst = 0;
        float currentDst = 0;

        for (int t = 5; t < 20; t+=5)
        {
            interceptPoint = trainEngine.PositionInTime(t);
            currentDst = Vector3.Distance(transform.position, interceptPoint);
            if(currentDst+aggroRange < moveSpeed * (t/Time.fixedDeltaTime))
            {
                // move towards the intercept point
                // when you get there, you should be in range to attack
                // then the attack logic should kick in
                //moveTarget = interceptPoint;
                Debug.Log("Approach Successful");
                return;
            }
            else
            {
                if(currentDst < closestDst || closestDst == 0)
                {
                    closestDst = currentDst;
                    //moveTarget = interceptPoint;
                }
            }
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, aggroRange);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);

    }

    void OnCollisionEnter(Collision col)
    {
        // when on the train, make it your parent so you move along with it
        // can break if the enemy runs into the side of the train
        // stolen from the Player class. Maybe there should be a parent class that has the functionality moving on the train requires
        if (col.transform.CompareTag("Train") && transform.parent != col.transform)
        {
            transform.parent = col.transform;
            col.gameObject.GetComponent<Train>().BoardedTrain(gameObject);
        }
    }

    /*
    void OnTriggerEnter(Collider col)
    {
        if(col.CompareTag("GroundLink"))
        {
            BoardingLink link = col.GetComponentInParent<BoardingLink>();
            if(link != null)
            {
                // teleport to the other end of the link
                // on the train
                transform.position = link.GetOnBoardPosition();
                // stop moving
                // otherwise the enemy will just try to jump off the train
                //canMove = false;
                OnTrainBoarded?.Invoke();

                onTrain = true;
                targetObject = trainEngine.gameObject;
                targetFound = true;
                FindTarget();

                Debug.Log("On Train");
                //path = trainEngine.GetComponentInChildren<NavGraph>().FindPath(transform, targetObject.transform);
                // bad idea
                // Train class should have a reference to the navGraph
                // like:
                if (trainEngine.navGraph != null)
                {
                    path = trainEngine.navGraph.FindPath(transform.position, targetObject.transform.position);
                    pathIndex = 0;
                }
            }
            else
            {
                Debug.Log("Link is null");
            }
        }
    }
    */
}
