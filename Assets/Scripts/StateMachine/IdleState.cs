using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IdleState : BaseState {

    private Enemy _enemy;
    private Transform _transform;

    bool attemptedIntercept = false;

    public IdleState(Enemy enemy) : base(enemy.gameObject)
    {
        _enemy = enemy;
    }

    public override Type Tick()
    {

        // if a target is within aggro range
        // swap to chase state

        // currently tries to path to a train
        // then chooses a target once on board
        // using an aggro radius approach when the enemy needs to board a train seems odd.
        // by the time they are within aggro range, the train is too far away to board?
        // unless the aggro range is huge
        // or there is a trainAggroRange and targetAggroRange

        if (_enemy.onTrain)
        {
            float shortestDist = _enemy.aggroRange + 10; // arbitrarily larger than aggroRange
            float distance;
            GameObject shortestTarget = null;
            foreach (GameObject target in _enemy.allTargets)
            {
                // set currentTarget to the highest threat or closest target of this list
                // then swap to chaseState
                distance = Vector3.Distance(target.transform.position, _transform.position);
                if (distance < _enemy.aggroRange)
                {
                    // close enough to attack
                    // but may as well find the closest
                    if(distance < shortestDist)
                    {
                        shortestDist = distance;
                        shortestTarget = target;
                    }
                }
            }
            if(shortestTarget != null)
            {
                _enemy.targetObject = shortestTarget;
                return typeof(ChaseState);
            }
        }
        else
        {
            // try to board the train?
            if(_enemy.trainEngine != null && !attemptedIntercept)
            {
                if (Vector3.Distance(_transform.position, _enemy.trainEngine.transform.position) < _enemy.boardingRange)
                {
                    //interceptTrain
                    // only want to call it once
                    // it won't let you board if the car isn't on the tracks
                    // but it has callback functionality to remedy that

                    _enemy.InterceptTrain();
                    attemptedIntercept = true;

                }
            }
        }

        // otherwise just stand still or something

        // return null to stay in same state
        return null;
    }

}
