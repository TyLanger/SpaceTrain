using System.Collections.Generic;
using UnityEngine;

// don't love this class name
public class SearchForTargetsOnTrain : IState
{
	
	private readonly Enemy _enemy;
	
	public SearchForTargetsOnTrain(Enemy enemy)
	{
		_enemy = enemy;
	}
	
	
	public void Tick()
	{
		
	}
	
	private Stockpile EvaluateBestStockpileToLoot(List<Stockpile> allStockpiles)
	{
        Stockpile bestStockpile = new Stockpile();
		
		
		float shortestDist = 10000;
		// return the closest stockpile
		foreach(Stockpile s in allStockpiles)
		{
			// check if stockpile has anything in it
			if(s.remainingLoot > 0)
			{
				
			
			
			
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
        /*
         * 
         * This is just all pseudocode
         * TODO: real variable names
         * 
         * 
		ListOfTargets = _enemy.trainEngine.GetAllTargets;
		// returns all players aboard as well as the stockpiles on the trains that you can rob
		
		ListOfHostiles = _enemy.trainEngine.GetHostiles;
		ListOfStockpiles = _enemy.trainEngine.GetStockpiles;
		
		Hostile closestHostileThatCanBeSeen;
		float shortestDistanceToHostile = 10000;
		
		foreach(Hostile h in ListOfHostiles)
		{
			if(canSee(h))
			{
				// attack the hostile you can see.
				if(dist = Vector3.Distance(transform.position, h.transform.position) < shortestDistanceToHostile)
				{
					shortestDistanceToHostile = dist;
					closestHostileThatCanBeSeen = h;
				}
			}
		}
		
		if(closestHostileThatCanBeSeen != null)
		{
			// can see at least 1 hostile
			// probably attack that one.
			_enemy.targetHostile = closestHostileThatCanBeSeen;
			return;
		}
		
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

