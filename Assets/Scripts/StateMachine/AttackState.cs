using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AttackState : BaseState {

	private float _attackReadyTimer;
	private Enemy _enemy;
	
	private float _timeOfLastAttack;
	
	public AttackState(Enemy enemy) : base(enemy.gameObject)
	{
		_enemy = enemy;
	}
	
	public override Type Tick()
	{
        /*
		if(_enemy.Target == null)
			return typeof(IdleState);
		
		// does this count down when in another state?
		_attackReadyTimer -= Time.deltaTime;
		
		if(_attackReadyTimer <= 0f)
		{
			Attack();
		}
		
		// timing is part of the weapon
		// so this seems unnecessary
		if(Time.time > _timeOfLastAttack + GameManager.TimeBetweenAttacks)
		{
			_timeOfLastAttack = Time.time;
			Attack();
			
			
			// should this be in ChaseState?
			// if you're in AttackState, you already think you're close enough
			// should just Attack() and if it hits, it hits.
			RaycastHit hit;
            // if you can see your target and it's within attack range, attack it
            if(Physics.Raycast(transform.position, (targetObject.transform.position - transform.position).normalized, out hit, attackRange, LayerMask.GetMask("Train") | LayerMask.GetMask("Damageable")))
            {
                
                if(hit.distance < attackRange)
                {
                    Attack();
                }
            }
		}
		
		// V3
		// attempt attack
		// weapon controls whether it can attack (cooldown, sufficient bullets, etc)
		// how does it work with fully auto or burst guns?
		// timeBetween bullets is different than timeBetweenAttacks
		// how would you make the enemy shoot less than a full clip with a fully auto gun?
		// shoot while the target is in sight - easy
		// shoot until some threshold of accuracy is lost
		// reload when target out of sight or less than 10% left in clip and not in danger
		// when in danger, shoot more liberally
		// depends on how smart the enemy is
		// might need StartAttacking/PullTrigger and StopAttacking/ReleaseTrigger for fully auto guns
        // player just calls Attack() every frame and it works for fully auto
		Attack()
        _enemy.Attack();
        // go back to chase state?
        // stay in attack state?
        if (outOfRange(target)
            return TypeOf(ChaseState);
		*/

        // slightly different style
        Vector3 target = Vector3.zero; // temporary to suppress errors
        float distToTarget = 0;
        float minRange = 10;
        float maxRange = 100;
        //or
        float optimalRange = 70;
        // same, just different names
        // or they could be slightly different. min/maxRange is the hard cap
        // optimal are the optimal ranges
        // there is a range window the enemy wants to stay in
        // if it can shoot 100 units, I don't want the behaviour to be:
        // get to 100 units, stop, shoot. Player moves out of range. Start moving again, etc.
        // would rather have it be:
        // get to 100 units, start shooting, keep moving to get to 70 units (optRange), then you can stop. Player moves. You are still shooting because you are still close enough. But move to try to stay at 70
        // are there a min and max optimal? How do you know if you should get closer or farther?
        // maybe just move until you are within the optimal window and then you can stop moving. Seems unnecessary to keep trying to stay at exactly 70 units from the player.
        // immagine if the only other walkable spot at EXACTLY 70 units was on the other side of a wall. So the player only needs to move 1 unit to force the enemy to path all the way around the train

        // instead of optimalMin and Max, 
        // float optimalBuffer = 10;
        // then just use max-optimalBuffer or min+optimalBuffer

        // I am forgetting to check LOS
        // I feel like I need a target object to raycast to
        // _enemy.targetObject
        // Raycast()

        distToTarget = Vector3.Distance(transform.position, target);
        if(distToTarget > maxRange)
        {
            // too far away to shoot
            // swap to chase state
            // this should be true if I switched to attackState in the first place
            return typeof(ChaseState);
        }
        else if (distToTarget > optimalRange)
        {
            // head towards the target until you are within the optimal range
            _enemy.SetMoveTarget(target);
        }
        else if(distToTarget < minRange)
        {
            // move away from the target
            // this isn't necessarily walkable....
            _enemy.SetMoveTarget((target + transform.position).normalized * optimalRange);
            // find a random walkable location?
            //_enemy.Orbit(target, optimalRange);
            // finds a location at optimal range from target that is walkable
            // would need some more intelligence so that you don't path towards the target to get around an obstacle
            // how would I do that? Do I need a new heuristic in A* that prioritizes not moving towards danger, but allows a little danger for a lot of safety? Would that danger info be stored in the navMesh?
            // Could I pick an arbitrary number of points around the target and then just pathfind to each one and choose the best/shortest?
            // maybe these move away conditions swap you to RetreatState?
            // there's also swapping to melee weapons...
        }
        else if(distToTarget < optimalRange)
        {
            _enemy.SetMoveTarget((target + transform.position).normalized * optimalRange);
        }

        _enemy.SetLookTarget(target);
        

		return null;
	}
}
