


using UnityEngine;

internal class PlunderTrain : IState
{

	private readonly Enemy _enemy;

    private float _nextPlunderTime;

	public PlunderTrain(Enemy enemy)
	{
		_enemy = enemy;
	}
	
	public void Tick()
	{
		if(Vector3.Distance(_enemy.transform.position, _enemy.plunderTarget.transform.position) > _enemy.lootReach)
		{
			if(_nextPlunderTime >= Time.time)
			{
				_nextPlunderTime = Time.time + _enemy.timeBetweenPlunders;
				// loot
				
				
				// this is kind of doing 2 things:
				// breaking open loot stockpiles
				// and looting the stockpiles
				// It's not hard to imagine a demolishion expert that blows open the stockpiles
				// and some scavenger enemies that pick up the scraps.
				// ergo, these should be different states
				if(_enemy.plunderTarget.remainingHealth > 0)
				{
					// break open stockpile
					_enemy.BreakOpenPlunderTarget();
				}
				else
				{
					_enemy.LootStockpile();
				}
			}
		}
	}
	
	public void OnEnter()
	{
		_enemy.SetMoveTarget(_enemy.plunderTarget.transform.position);
	}
	
	public void OnExit()
	{
		// stop moving?
	}
}