using UnityEngine;

internal class DisembarkTrain : IState
{
	
	
	private readonly Enemy _enemy;

	public DisembarkTrain(Enemy enemy)
	{
		_enemy = enemy;
	}
	
	public void Tick()
	{
		if(NearDisembarkPoint())
		{
			_enemy.Disembark();
			// teleport to the ground or play an animation or whatever
		}
	}
	
    bool NearDisembarkPoint()
    {
        return Vector3.Distance(_enemy.transform.position, _enemy.disembarkLink.trainPoint.position) < _enemy.disembarkDistance;
    }

	public void OnEnter()
	{
        _enemy.stateMemory += this;
        //Debug.Log("Entered " + this);

        // find nearest point where you can get off
        //Vector3 homeDepot;
        // path to each boarding point
        // path from each boarding point to the home depot (where you drop off loot)

        // Easiest way is just to jump off the train at the nearest spot then run off the screen
        // pick the boarding point that is linearly closest and on the same train car. Probably right most of the time

        Vector3[] boardingPoints =  _enemy.GetDisembarkPointsAsPositions();
        Vector3 closestPoint = _enemy.transform.position; // If I don't find a link, move to current pos
		float shortestDist = 1000000;
		foreach(Vector3 v in boardingPoints)
		{
			float dist = Vector3.Distance(_enemy.transform.position, v);
			if(dist < shortestDist)
			{
				shortestDist = dist;
				closestPoint = v;
			}
		}
		
		// path to that point
		_enemy.SetMoveTarget(closestPoint);

	}
	
	public void OnExit()
	{
		
	}

}