using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChaseState : BaseState {

    private Enemy _enemy;
    private Vector3 targetLastPosition;

    public ChaseState(Enemy enemy) : base(enemy.gameObject)
    {
        _enemy = enemy;
    }

    public override Type Tick()
    {
        if(_enemy.targetObject == null)
        {
            // no target, go back to idle state
            return typeof(IdleState);
        }

        if(targetLastPosition == null)
        {
            if (_enemy.targetObject.transform.position != targetLastPosition)
            {
                // only try to path if the target has moved.
                // this may not work because the target is on the train and the train is moving
                _enemy.SetMoveTarget(_enemy.targetObject.transform.position);
                targetLastPosition = _enemy.targetObject.transform.position;
            }
        }

        // look towards where you're moving

        if (Vector3.Distance(_enemy.transform.position, _enemy.targetObject.transform.position) < _enemy.attackRange)
        {
            // do I check if I can attack?
            // weapon loaded, LOS, etc.
            return typeof(AttackState);
        }

        /*
        if (_enemy.Target == null)
            return typeof(WanderState); // go back to the default state. Probable IdleState in mine

        // use A* path instead of moving to an enemy
        var target = path[index].transform;
        transform.position = Vector3.MoveTowards(transform.position, path[pathIndex].position, moveSpeed);


        // face your target
        transform.LookAt(_enemy.Target);
        // move towards your target
        transform.Translate(Vector3.forward * Time.deltaTime * GameSettings.EnemySpeed);

        var distance = Vector3.Distance(transform.position, _enemy.Target.transform.position);
        if (distance <= GameSettings.AttackRange)
        {
            // should I check if the weapon is ready?
            // Do I check here or in AttackState?
            // What if the weapon isn't ready, but I'm in range?
            // inRange -> AttackState -> NotReady -> ChaseState -> inRange -> ....
            // where does reloading happen?
            // ReloadState?
            // Kinda makes sense as you can't do anything while reloading
            // Maybe you can still move while reloading?
            // Need a multi state system
            // Leg states and arm states.
            // can move around while doing other things?
            if (weapon.CanAttack())
            {
                return typeof(AttackState);
            }
            else
            {
                // keep chasing until wepon is ready to fire
                return null;
            }
        }

        // keep going on the same state; don't state change
        */
        return null;
    }
}
