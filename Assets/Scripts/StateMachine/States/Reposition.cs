using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Reposition : IState
{
    private readonly Enemy _enemy;

    public Reposition(Enemy enemy)
    {
        _enemy = enemy;
    }

    public void Tick()
    {

    }

    public void OnEnter()
    {
        _enemy.stateMemory += this;
        _enemy.SwapOrbits();
    }

    public void OnExit()
    {

    }
}
