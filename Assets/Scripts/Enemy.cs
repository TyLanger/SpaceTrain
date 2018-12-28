﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Enemy : MonoBehaviour {

    // base enemy class
    // other enemies should extend this for more functionality

    Health health;

    public float moveSpeed = 0.1f;
    public float attackRange = 0.5f;

    public Vector3 target;
    GameObject targetObject;
    bool targetFound = false;
    // list of all the targets the enemy could have (players, train cars)
    public GameObject[] allTargets;
    public Train trainEngine;

    public Transform TargetMarker;

    public GameObject weapon;
    public int weaponDamage = 15;

    // Use this for initialization
    void Start() {
        health = GetComponent<Health>();
        Invoke("InterceptTrain", 0.5f);
    }

    // Update is called once per frame
    void FixedUpdate() {
        if (targetFound)
        {
            TargetMarker.position = targetObject.transform.position;
            transform.position = Vector3.MoveTowards(transform.position, targetObject.transform.position, moveSpeed);
        }
        else
        {
            TargetMarker.position = target;
            transform.position = Vector3.MoveTowards(transform.position, target, moveSpeed);
        }
    }

    void Attack()
    {
        
    }

    IEnumerator MeleeAttack()
    {
        //root

        // start swing
        // turn on weapon collider

        // unroot

        yield return null;
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
            if(currentDst < attackRange)
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
            if(currentDst+attackRange < moveSpeed * (t/Time.fixedDeltaTime))
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
}