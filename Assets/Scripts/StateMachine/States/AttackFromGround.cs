using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AttackFromGround : IState
{
    private readonly Enemy _enemy;

    public AttackFromGround(Enemy enemy)
    {
        _enemy = enemy;
    }

    public void Tick()
    {
        float dist = _enemy.GetSightDistance(_enemy.hostileTarget);
        if(dist > 0)
        {
            _enemy.canSeeHostileTarget = true;

            if(dist < _enemy.attackRange)
            {
                _enemy.Attack();
            }


            // maybe move closer?
        }
        else
        {
            _enemy.canSeeHostileTarget = false;
        }
    }

    public void OnEnter()
    {
        _enemy.stateMemory += this;
    }

    public void OnExit()
    {

    }

}
