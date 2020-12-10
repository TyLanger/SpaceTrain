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
    //bool canMove = true;
    public float outerAttackRange = 0.5f;
    public float preferredAttackRange = 0.5f;
    // move until you are inside the preferred attack range and then you can stop
    // don't start moving again until you are outside the outer attack range.
    // these might be redundant for melee attacks. Need to test them.
    bool wantToMove = true;

    public Vector3 moveTarget;

    // Reference to the boarding link you are trying to go to. Used by BoardTrain state
    public BoardingLink TargetBoardingLink;
    public float TrainBoardDist = 1; // distance you can use the link from. 

    public float lootReach = 2; // arbitrary number. How close you need to be to loot to be able to pick it up. Used by PlunderTrain state
    public float timeBetweenPlunders = 4; // arbitrary time. How often you can attempt to plunder a stockpile for loot. Used by PlunderTrain state
    //public float timeOfNextPlunder = 0;
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

    public Transform TargetMarker;
    public Transform[] path;
    public int pathIndex = 0;
    public bool pathEnded = false;
    public float distToNextNode = 0;

    public Weapon weapon;

    protected StateMachine _stateMachine;
    public float timeOfIntercept;
    //public Transform plunderTarget; // should this be a Stockpile (instead of a transform)?
    public GameObject hostileTarget;
    public bool canSeeHostileTarget = false;
    [TextArea(1,10)]
    public string stateMemory;

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
            Debug.Log("No Train set");
        }
    }

    public void Initialize()
    {
        StateMachineSetup();
    }

    protected virtual void StateMachineSetup()
    {
        if(trainEngine == null)
        {
            Debug.Log("StateMachineSetup. No train");
        }

        _stateMachine = new StateMachine();


        // States
        var search = new SearchForTrainIntercept(this);
        var moveToTrain = new MoveToRZPoint(this);
        var board = new BoardTrain(this);
        var searchOnTrain = new SearchForTargetsOnTrain(this);
        var plunder = new PlunderTrain(this);
        var disembark = new DisembarkTrain(this);
        var attack = new AttackHostile(this);
        var death = new Death();
        //var investigate = new InvestigateHostile(this);

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
        //At(attack, investigate, HostileOutOfSight());   // not sure if investigate should exist.
        At(plunder, disembark, InventoryFullOfLoot());

        At(attack, searchOnTrain, LostHostileTarget()); // maybe want this?
        //At(attack, something, NotOnTrain());          // what happens if you're no longer on the train while in the attack state?
        // also, you can't currently attack players while they're off the train
        At(death, search, Revived());

        // Should I have a dead state?
        // would probably depend how enemy death works. Ideally, would probably want to add the enemy back to an object pool.
        // maybe go to a dead state until you respawn, where you then go to back to search
        _stateMachine.AddAnyTransition(death, Dead());

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
        Func<bool> Dead() => () => !alive; // respawn takes some time so this shouldn't have an issue firing. But it triggers to quickly. Enemy is already searching for an intercept before it's back alive
        Func<bool> Revived() => () => alive;

        //unused but may be helpful
        Func<bool> AtEndOfPath() => () => pathEnded;
        Func<bool> LostHostileTarget() => () => AtEndOfPath()() && HostileOutOfSight()();

    }

    void Update()
    {

        _stateMachine?.Tick();
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        if (!alive)
            return;


        moveSpeed = StatsManager.Instance().BaseMoveSpeed * StatsManager.Instance().EnemyMoveMultiplier;


        /// Movement
        /// Always just move towards move target
        /// if you have a path, set move target to the next link on the path
        /// If you don't have a path, the AI will set your move target. So just follow whatever you have

        if (path != null && !pathEnded)
        {
            //Debug.Log("Path not null");
            if (pathIndex < path.Length)
            {
                moveTarget = path[pathIndex].position;
                // are you close to the node? If so, move on to the next node
                distToNextNode = Vector3.Distance(transform.position, path[pathIndex].position);
                if (distToNextNode < 1f)
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
                        //Debug.Log("Path Ended");
                    }
                }
            }
        }
        else
        {
            // once the path has ended, move towards the targetMarker
            // it is at the position of the end of the path
            // so once you reach the end of the path, you stand still (but don't fall off the train)
            if (TargetMarker != null)
            {
                moveTarget = TargetMarker.position;
            }
        }

        // wantToMove stops you from falling off the train
        // otherwise, you keep trying to move to moveTarget (a v3) and that pos is no longer on the train
        if (wantToMove)
        {
            transform.position = Vector3.MoveTowards(transform.position, moveTarget, moveSpeed);
        }

        /// facing
        /// Face where you're moving
        /// or where you're aiming if you have a hostile target
        /// AttackHostile sets the canSeeHostileTarget flag
        if (canSeeHostileTarget)
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

    public void SetLookTarget(Vector3 lookPos)
    {
        //lookTarget = lookPos;
    }

    public float GetSightDistance(GameObject lookTarget)
    {
        // can you see target and how far away is it?

        Vector3 origin = transform.position + Vector3.up * 0.8f; // +eye pos
        Vector3 direction = lookTarget.transform.position - transform.position;
        float maxDist = 100; // max sight distance


        // seeing a target is more than just seeing the center of them. Fire multiple rays at the whole width of the target?
        // maybe you also need to see a minimum amount of them. i.e. not just see one pixel of their shoulder.
        //Debug.DrawRay(origin, direction, Color.blue, 5f);

        if (Physics.Raycast(origin, direction, out RaycastHit hit, maxDist))
        {
            // does it return 0 if it didn't hit the target? Like if it hit a wall before the player
            if (hit.collider.gameObject.Equals(lookTarget))
            {
                // Only return if you hit your target
                // if you hit a wall first, you can't see your target
                //Debug.Log("Hit " + hit.collider.gameObject);
                return hit.distance;
            }
            /*
            else
            {
                Debug.Log("Target: "+lookTarget+". Hit " + hit.collider.gameObject + " instead.");
            }
            */
        }


        // fail state if you can't see it
        return -1;
    }

    public void StartMovingToRZPoint()
    {
        // move in a straight line to the move target. Doesn't use pathfinding
        moveTarget = trainRZPoint;
        wantToMove = true;
    }

    public void HoldPosition()
    {
        wantToMove = false;
    }

    public void WantToMove()
    {
        wantToMove = true;
    }

    public void SetMoveTarget(Vector3 targetPos, bool debug = false)
    {
        // how do I check that I'm not just calculating the same path every frame?
        //if(targetPos != currentTargetPos)
        if(debug)
        {
            Debug.Log(String.Format("Start: {0}. End: {1}", transform.position, targetPos));
        }
        wantToMove = true;
        path = trainEngine.navGraph.FindPath(transform.position, targetPos);
        pathEnded = false;
        pathIndex = 0;

        if(TargetMarker != null)
            TargetMarker.position = targetPos;
        
        

        if(path == null)
        {
            // this means the targetPos is not on the graph
            // so it's on the ground where there is no path.
            // That's a big ASSume
            // path being null means either the start or end is not on the navgraph
            Debug.Log("Why is path null?");
        }
        else if (path.Length == 0)
        {
            // probably in the same tri
            //Debug.Log("Probably in the same tri");
            pathEnded = true;
            // this is unnecessary
            // The path isn't null, but pathEnded is true
            // When that's the case, it defaults to setting moveTarget to the target marker position
            // and the target marker position is targetPos as seen ^
            //moveTarget = targetPos;
            // basically, I can't set moveTarget. In fixedUpdate, it either gets set to the path or the targetMarker
        }
        else
        {
            // path exists

            // the nodes follow the train around
            // what difference does it make if the target marker follows the train or follows the node that follows the train?
            // This doesn't quite work on the inbetween trains area, but neither does anything else. Players and agents move in that area too.
            if (TargetMarker != null)
            {
                TargetMarker.parent = path[path.Length - 1];
                path[path.Length - 1] = TargetMarker;
            }
        }

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
                //plunderTarget.remainingHealth -= 10;
                plunderTarget.remainingLoot -= 10;
                currentInventory += 10;
                // currentLoot += 10;
            }
        }
    }

    /// <summary>
    /// Intercept the train to try to board it
    /// </summary>
    public virtual void InterceptTrain()
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
        //Debug.Log(gameObject + " is trying to jump off train");
        transform.position = disembarkLink.groundPoint.position; // might not be quite this easy.
        transform.parent = null; // make this more dynamic some day. (Whenever you leave the train, lose it as a parent. No just when you leave voluntarily)
        wantToMove = false; // change when I setup a run away/escape state
        trainEngine.LeaveTrain(gameObject);
    }

    internal BoardingLink[] GetDisembarkLinks()
    {
        // should probably figure out what train it is on, not just use trainEngine
        // trainEngine is the train it is given when it spawns then it doesn't really change (even if you board the train and path to a new train)
        BoardingLink[] allLinks = new BoardingLink[trainEngine.leftLinks.Length + trainEngine.rightLinks.Length];

        // Might have fixed this when you collide with the train in OnCollisionEnter
        /*
        Train currentTrain = GetComponentInParent<Train>();

        if(currentTrain != null)
        {
            trainEngine = currentTrain;
        }
        */

        for (int i = 0; i < trainEngine.leftLinks.Length; i++)
        {
            allLinks[i] = trainEngine.leftLinks[i];
        }
        for (int i = 0; i < trainEngine.rightLinks.Length; i++)
        {
            allLinks[trainEngine.leftLinks.Length + i] = trainEngine.rightLinks[i];
        }

        return allLinks;
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

    void OnDrawGizmosSelected()
    {
        /*
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, aggroRange);
        */

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, outerAttackRange);

        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, preferredAttackRange);

    }

    void OnCollisionEnter(Collision col)
    {
        // when on the train, make it your parent so you move along with it
        // can break if the enemy runs into the side of the train
        // stolen from the Player class. Maybe there should be a parent class that has the functionality moving on the train requires
        if (col.transform.CompareTag("Train") && transform.parent != col.transform)
        {
            transform.parent = col.transform;
            trainEngine = col.gameObject.GetComponent<Train>();
            trainEngine.BoardedTrain(gameObject);
            //col.gameObject.GetComponent<Train>().BoardedTrain(gameObject);
            
        }
    }

    void OnCollisionExit(Collision col)
    {
        // remove the train as your parent?
        // WOuld one of them need to be delayed to not mess it up?
    }
}
