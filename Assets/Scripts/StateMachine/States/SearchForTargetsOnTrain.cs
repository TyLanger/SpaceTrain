using System.Collections.Generic;
using UnityEngine;

// don't love this class name
public class SearchForTargetsOnTrain : IState
{
	
	private readonly Enemy _enemy;
    Stockpile bestStockpile;
	
	public SearchForTargetsOnTrain(Enemy enemy)
	{
		_enemy = enemy;
	}
	
	
	public void Tick()
	{
		
	}
	
	private Stockpile EvaluateBestStockpileToLoot(Stockpile[] allStockpiles)
	{
        //Stockpile bestStockpile = new Stockpile();
		
		
		float shortestDist = 10000;
		// return the closest stockpile
		foreach(Stockpile s in allStockpiles)
		{
			// check if stockpile has anything in it
			if(s.remainingLoot > 0)
			{
				
			
			    // dist should technically be path distance, not straight line dist, but this is probably close enough for now
			
				float dist = Vector3.Distance(_enemy.transform.position, s.transform.position);
				if(dist < shortestDist)
				{
					shortestDist = dist;
					bestStockpile = s;
				}
			}
		}
		
		return bestStockpile;
		
		
		
		// Advanced tactics for smarter AI
		// this is like version 6.5
		// way into the future
		/*
		// weights
		float distanceWeightMultiplier = 1;
		float stockpileDefenseHealthMultiplier = 1; // how much more damage before it breaks open
		int numAlliesLootingMultiplier = 1;
		float remainingLootMultiplier = 1;
		
		// prbably the best implementation
		// maybe the best way to do this is an expected value function
		// try to maximise how much value you think you'll get out of it per second.
		// something like 
		timeItTakesToGetTheLoot = dist / moveSpeed + healthRemaining / DPS;
		ev = totalLootValue / (timeItTakesToGetTheLoot);
		// I don't really know how to account for competing/cooperative allies.
		// they help with dps, but how much do they get in one anothers' way?
		// probably some number can damage or loot at the same time.
		// so those first x wouldn't have adverse effects.
		// more allies loot it faster. This is only really a problem if the stockpile will be all gone before your turn.
		
		foreach(Stockpile s in ListOfStockpiles)
		{
			// are any of the stockpiles broken into already?
			// how many allies are currently looting a given stockpile?
			// how close is the stockpile?
			
			float expectedValue = 0;
			
			// these aren't right.
			// lose points based on how far away it is
			//expectedValue -= Vector3.Distance(transform.position, s.transform.position) * distanceWeightMultiplier;
			//expectedValue -= s.remainingDefenseHealth * stockpileDefenseHealthMultiplier;
			
			if(expectedValue > bestStockpileExpectedValue)
			{
				bestStockpileExpectedValue = expectedValue;
				bestStockpile = s;
			}
		}
        */
	}
	
	public void OnEnter() {

        _enemy.stateMemory += this;
        //Debug.Log("Entered " + this);

        _enemy.HoldPosition(); // stand still so you don't fall off

        //start with enemies you can see
        // pick the nearest one
        // if you can't see any
        // move on to looting 
        // choose the closest


        HashSet<GameObject> targetSet = _enemy.trainEngine.GetAllFriendlyTargets();

        if(targetSet.Count == 0)
        {
            Debug.Log("No friendly targets");
        }

        float shortestDist = 100000;
        GameObject closestTarget = null;

        foreach (var t in targetSet)
        {
            float dist = _enemy.GetSightDistance(t);
            //Debug.Log("Dist: " + dist);
            if(dist>0)
            {
                if(dist < shortestDist)
                {
                    shortestDist = dist;
                    closestTarget = t;
                }
            }
        }

        if(closestTarget != null)
        {
            // can see at least one target
            _enemy.hostileTarget = closestTarget;
        }
        else
        {
            
            Debug.Log("Didn't see any targets");

            Stockpile stockToLoot = EvaluateBestStockpileToLoot(_enemy.trainEngine?.GetAllStockpiles());
            if(stockToLoot != null)
            {
                _enemy.plunderTarget = stockToLoot;
            }
        }
        //Debug.Break();
        /* Stockpiles don't exactly exist yet
		
		Stockpile stockToLoot = EvaluateBestStockpileToLoot();
		if(stockToLoot != null)
		{
			// found the best stockpile to loot
			_enemy.lootTarget = stockToLoot;
			return;
		}
		
		
		// alt version
		// set all possible targets
		// from there, different agents have different states they can get to.
		// some extensions of the Enemy class will just throw away the best protected stockpile b/c they can't break into it.
		_enemy.SetTrainPointOfInterest(bestViableHostile, bestProtectedStockpile, bestOpenStockpile);
		

        */
    }
	public void OnExit() {}
}

