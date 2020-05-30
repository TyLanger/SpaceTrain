


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
			_enemy.GiveUpOnHstile();
			// go back to searchForTargetsOnTrain
		}
        */
	}

	public void OnEnter()
	{
		//_enemy.SetMoveTarget(_enemy.hostileLastPosition);
	}

    public void OnExit() { }
}