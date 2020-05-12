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


    public float moveSpeed = 0.1f;
    // the range where it will find new targets to attack
    public float aggroRange = 4;
    // the range where it will attemp to atack targets
    public float attackRange = 0.5f;
    bool canMove = true;

    public Vector3 target;
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
    public Train trainEngine;
    public event System.Action OnTrainBoarded;
    public bool onTrain { get; protected set; }
    // how close the train has to be for the enemy to attempt to board it
    public float boardingRange = 100;

    public Transform TargetMarker;
    public Transform[] path;
    public int pathIndex = 0;

    public Weapon weapon;
    public int weaponDamage = 15;

    // Use this for initialization
    void Start() {
        respawnPoint = transform.position;
        health = GetComponent<Health>();
        health.OnDeath += Die;

        if (trainEngine != null)
        {
            target = transform.position;

            Invoke("InterceptTrain", 0.5f);
        }
        else
        {
            // temporary
            // enemy stands still if there is no train
            // i.e. when in the testing scene
            target = transform.position;
            if (allTargets.Length > 0)
            {
                FindTarget();
            }
        }
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


        // target is found when a target from the list is within aggro range
        //TODO Move to ChaseState
        if (targetFound)
        {
            if (TargetMarker != null)
            {
                TargetMarker.position = targetObject.transform.position;
            }
            /* raycasting instead. Works better for rectangular target objects. You hit their hitbox instead checking for their center
            if(Vector3.Distance(transform.position, targetObject.transform.position) < attackRange)
            {
                Attack();
            }
            */
            
            // Train is on layer "Train"
            // Player is on layer "Damageable"
            // Is there a reason they should be different layers? I don't know if they should or shouldn't be
            // When the enemy is chasing the player and the player puts the train in between them, the enemy fires a ray towards the player and hits the train
            // so it attacks the train even though it's trying to find the player (and it can't path find yet so it just moves straight towards the player)
            RaycastHit hit;
            // if you can see your target and it's within attack range, attack it
            if(Physics.Raycast(transform.position, (targetObject.transform.position - transform.position).normalized, out hit, attackRange, LayerMask.GetMask("Train") | LayerMask.GetMask("Damageable")))
            {
                
                if(hit.distance < attackRange)
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
                    // 0.3 is arbitrary
                    // it's just close to the point, but doesn't have to be right on it
                    // instead of distance, being able to see the next point might be a better solution
                    // needs to be larger because the point is on the ground, but measures to the center of the agent
                    if (Vector3.Distance(transform.position, path[pathIndex].position) < 1f)
                    {
                        // watch for oout of bounds
                        if (pathIndex < path.Length-1)
                        {
                            pathIndex++;
                        }
                        else
                        {
                            // end of path reached
                            // what to do? find a new path? destroy this one? Update to follow a potentially moving target?
                            // path = null?
                        }
                        
                    }
                }
            }
            // face where you're going
            // should be towards the direction you're facing while pathing towards the target
            transform.forward = targetObject.transform.position - transform.position;
            if(weapon != null)
            {
                weapon.UpdateAimPos(targetObject.transform.position);
            }
        }
        else
        // TODO Move to IdleState or AggroState
        {
            if (TargetMarker != null)
            {
                TargetMarker.position = target;
            }
            else if (allTargets.Length > 0)
            {
                FindTarget();
            }
            if (canMove)
            {
                transform.position = Vector3.MoveTowards(transform.position, target, moveSpeed);
            }
            // face where you're going
            if (target != transform.position)
            {
                transform.forward = target - transform.position;
            }

        }
    }

    // State Machine
    private void Awake()
    {
        //InitializeStateMachine();
    }

    private void InitializeStateMachine()
    {
        var states = new Dictionary<Type, BaseState>()
        {
            { typeof(IdleState), new IdleState(this) },
            { typeof(ChaseState), new ChaseState(this) },
            { typeof(AttackState), new AttackState(this) }
        };

        GetComponent<StateMachine>().SetStates(states);
    }
    
    public void SetMoveTarget(Vector3 targetPos)
    {
        // how do I check that I'm not just calculating the same path every frame?
        //if(targetPos != currentTargetPos)
        path = trainEngine.navGraph.FindPath(transform.position, targetPos);
    }

    public void SetLookTarget(Vector3 lookPos)
    {
        //lookTarget = lookPos;
    }

    public void PathToTarget()
    {
        // calculate a path to the current target (targetObject)
        // how can I check if the target is stationary so I don't have to calculate a new path that is the same as the old path?
        // GetComponent<Player> suggests movement
        // GetComponent<ShippingContainer> suggests stationary?
        // GetComponent<IMoveable> ?
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

    void Attack()
    {
        weapon.Attack();
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

        Vector3[] boardingPoints;
        float closestDst = 0;
        float currentDst = 0;


        // Finds A close enough boarding point
        // not THE closest boarding point yet
        for (int t = 5; t <= 20; t += 5)
        {
            boardingPoints = trainEngine.GetBoardingLocationsInTime(t);
            for (int i = 0; i < boardingPoints.Length; i++)
            {
                currentDst = Vector3.Distance(transform.position, boardingPoints[i]);
                // x/ time.fixedDeltaTime is the number of fixed updates there are in x seconds
                if (currentDst < moveSpeed * (t/Time.fixedDeltaTime))
                {
                    target = boardingPoints[i];
                    Debug.Log(gameObject.name + ": Successful Boarding at time = " + t);
                    return;
                }
                else
                {
                    if(currentDst < closestDst || closestDst == 0)
                    {
                        closestDst = currentDst;
                        target = boardingPoints[i];
                    }
                }
            }
        }
        Debug.Log(gameObject.name + ": Didn't find boarding; moving to closest");
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
                target = interceptPoint;
                Debug.Log("Approach Successful");
                return;
            }
            else
            {
                if(currentDst < closestDst || closestDst == 0)
                {
                    closestDst = currentDst;
                    target = interceptPoint;
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
        }
    }

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
                if(OnTrainBoarded != null)
                {
                    OnTrainBoarded();
                }

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
}
