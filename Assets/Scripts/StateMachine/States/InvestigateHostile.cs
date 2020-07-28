using UnityEngine;

internal class InvestigateHostile : IState
{

    private readonly Enemy _enemy;

    public InvestigateHostile(Enemy enemy)
    {
        _enemy = enemy;
    }

	public void Tick()
	{
        /*
         * Pseudocode
         * 
         * 
		if(CanSee(_enemy.hostileTarget))
		{
			_enemy.FoundThem();
			// found your target
			// go back to attack state
		}
		
		if(ReachedEndOfPath())
		{
			_enemy.GiveUpOnHostile();
			// go back to searchForTargetsOnTrain
		}
        */
	}

	public void OnEnter()
	{
        Debug.Log("Entered " + this);

        //_enemy.SetMoveTarget(_enemy.hostileLastPosition);
    }

    public void OnExit() { }
}