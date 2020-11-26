using UnityEngine;

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
            // try to move to the preferred range
            // once you're there, you can stop moving
            // until you are outside the out range
            // then start moving to the preferred range again

            // if too close
            // should be based on radius
            // radius of capsules is 0.5 so 2*0.5 is 1
            if (dist > 1f)
            {

                if (madeItInsidePreferredRange)
                {
                    if (dist > _enemy.outerAttackRange) // bigger
                    {
                        madeItInsidePreferredRange = false;
                        _enemy.SetMoveTarget(_enemy.hostileTarget.transform.position);
                    }
                    else
                    {
                        // stop moving
                        _enemy.HoldPosition();
                        //Debug.Log("inside of outer");
                    }
                }
                else
                {
                    if (dist > _enemy.preferredAttackRange) // smaller
                    {
                        // too far away. keep trying to move closer
                        _enemy.SetMoveTarget(_enemy.hostileTarget.transform.position);
                    }
                    else
                    {
                        // inside preferred dist
                        //Debug.Log("Inside Preferred");
                        madeItInsidePreferredRange = true;
                    }
                }
            }
            else
            {
                // too close
                _enemy.HoldPosition();
            }

            //else if(dist )
		
		}
		else
		{
            // can't see your target
            _enemy.canSeeHostileTarget = false;
			// if you still had a path, you'll keep following the path
            // maybe you'll find your target again
            if(_enemy.pathEnded)
            {
                // give up on this state?
            }
            else
            {
                // enemy may be stuck not moving.
                // if the enemy gets within preferred range and outer range
                // it stops moving
                // if it's in that sweet spot when the player leaves view
                // it won't move to the player's last spot and will just soft lock itself until it sees the player again.
                // This isn't the 10/10 fix for this.
                _enemy.WantToMove();
                //_enemy.SetMoveTarget(lastKnownPosition);
            }
        }
	}

    public void OnEnter()
	{
        Debug.Log("Entered " + this);
        //Debug.Break();

        // pathfind to get near the target
        // Do I path immediately or handle that in tick?

        // might be a good idea to recache some variables.
        // stuff like aggro range and attack range.
        // it's not unheard of to have them change. Maybe the enemy has swapped weapons since last time it was in this state
        // maybe caching the enemy is enough

    }

    public void OnExit() {
        _enemy.canSeeHostileTarget = false; // not necessarily true, but probably safer
        // if you're moving into another state where you want to look at your target, it will handle it. Default is look where you're moving
    }
	
}