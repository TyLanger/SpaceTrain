

internal class AttackHostile : IState
{
	
	private readonly Enemy _enemy;

    private bool madeItInsidePreferredRange = false;
	
	public AttackHostile(Enemy enemy)
	{
		_enemy = enemy;
	}

	public void Tick()
	{
        
        // Pathfind returns a node point
        // So I need to also know when to switch off the path and just go straight at the target
        // When on the last node? Second last node?
		// stuff I care about
		//CanSeeHostile();
		//WithinRangeToAttack();
		// which order do they go in? CanSee, then range. How would you check range on something you can't see?
		
        float dist = _enemy.GetSightDistance(_enemy.hostileTarget);
		if(dist > 0)
		{
            // should be true the first time this ticks. (b/c that's how it swapped to this state)
            // May not be true as the players move
            _enemy.canSeeHostileTarget = true;
            

            // if in attack range, try to attack
            if(dist < _enemy.attackRange)
            {
                _enemy.Attack();
            }


            // when not in range to attack,
            // try to move tot he preferred range
            // once you're there, you can stop moving
            // until you are outside the out range
            // then start moving to the preferred range again

            if (madeItInsidePreferredRange)
            {
                if (dist > _enemy.outerAttackRange)
                {
                    madeItInsidePreferredRange = false;
                    _enemy.SetMoveTarget(_enemy.hostileTarget.transform.position);
                }
                else
                {
                    // stop moving
                    // set move target to current pos?
                    _enemy.HoldPosition();
                }
            }
            else
            {
                if (dist > _enemy.preferredAttackRange)
                {
                    // too far away. keep trying to move closer
                    _enemy.SetMoveTarget(_enemy.hostileTarget.transform.position);
                }
                else
                {
                    // inside preferred dist
                    madeItInsidePreferredRange = true;
                }
            }

            //else if(dist )
		
		}
		else
		{
            // can't see your target
            _enemy.canSeeHostileTarget = false;
			// if you still had a path, you'll keep following the path
            // maybe you'll find your target again
            // How do I check if you've reached the end of the path?
            // _enemy.EndOfPathReached += someCallback()
            if(_enemy.pathEnded)
            {
                // give up on this state?
            }
		}
        
		
	}
	
	public void OnEnter()
	{
		// pathfind to get near the target
        // Do I path immediately or handle that in tick?

        // might be a good idea to recache some variables.
        // stuff like aggro range and attack range.
        // it's not unheard of to have them change. Maybe the enemy has swapped weapons since last time it was in this state
        // maybe caching the enemy is enough
		
	}

    public void OnExit() { }
	
}