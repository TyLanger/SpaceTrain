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
    GameObject targetObject;
    bool targetFound = false;
    // list of all the targets the enemy could have (players, train cars)
    public GameObject[] allTargets;
    public Train trainEngine;

    public Transform TargetMarker;

    public Weapon weapon;
    public int weaponDamage = 15;

    // Use this for initialization
    void Start() {
        respawnPoint = transform.position;
        health = GetComponent<Health>();
        health.OnDeath += Die;
        
        if (trainEngine != null)
        {
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

        // target is found when a target from the list is within aggro range
        if (targetFound)
        {
            if (TargetMarker != null)
            {
                TargetMarker.position = targetObject.transform.position;
            }
            if(Vector3.Distance(transform.position, targetObject.transform.position) < attackRange)
            {
                Attack();
            }

            if (canMove)
            {
                transform.position = Vector3.MoveTowards(transform.position, targetObject.transform.position, moveSpeed);
            }
            // face where you're going
            transform.forward = targetObject.transform.position - transform.position;
            if(weapon != null)
            {
                weapon.UpdateAimPos(targetObject.transform.position);
            }
        }
        else
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
    void InterceptTrain()
    {
        // try to get to the track ahead of the train

        Vector3 interceptPoint;
        float closestDst = 0;
        float currentDst = 0;

        for (int t = 5; t <= 20; t += 5)
        {

            interceptPoint = trainEngine.PositionInTime(t);
            // x/ time.fixedDeltaTime is the number of fixed updates there are in x seconds
            currentDst = Vector3.Distance(transform.position, interceptPoint);
            //Debug.Log(currentDst);
            if (currentDst < moveSpeed * (t / Time.fixedDeltaTime))
            {
                // can make it to that point in time
                target = interceptPoint;
                Debug.Log("Success");
                return;
            }
            else
            {
                if(currentDst < closestDst || closestDst == 0)
                {
                    // this one is the closest so use this if you don't find one better
                    closestDst = currentDst;
                    target = interceptPoint;
                }
            }
        }
    }

    /// <summary>
    ///  Get to within attack range of the train so you can attack from a distance
    /// </summary>
    void ApproachTrain()
    {
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
}
