

internal class AttackHostile : IState
{
	
	private readonly Enemy _enemy;
	
	public AttackHostile(Enemy enemy)
	{
		_enemy = enemy;
	}

	public void Tick()
	{
        
        /*
         * Pseudocode
         * 
         * 

		// stuff I care about
		CanSeeHostile();
		WithinRangeToAttack();
		// which order do they go in? CanSee, then range. How would you check range on something you can't see?
		
		if(CanSeeHostile())
		{
			_enemy.AimAtHostile();
		
		
			if(WithinRangeToAttack())
			{
				_enemy.Shoot();
			}
			else
			{
				// move closer
			}
		
		}
		else
		{
			// move where you can see
		}
        */
		
	}
	
	public void OnEnter()
	{
		// pathfind to get near the target
		
	}

    public void OnExit() { }
	
}