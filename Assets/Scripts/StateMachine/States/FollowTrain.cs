
using UnityEngine;

public class FollowTrain : IState
{
    private readonly Enemy _enemy;

    public FollowTrain(Enemy enemy)
    {
        _enemy = enemy;
    }

    public void Tick()
    {

    }

    public void OnEnter()
    {
        // this is just the same as SearchForTrainIntercept....
        _enemy.FollowTrain();
        _enemy.stateMemory += this;
    }

    public void OnExit()
    {

    }
}
