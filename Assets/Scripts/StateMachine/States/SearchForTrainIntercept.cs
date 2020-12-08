using UnityEngine;

internal class SearchForTrainIntercept : IState
{
	private readonly Enemy _enemy;

    private float maxWaitTime = 25; // only wait for the train to start moving for 25s
    private float timeStartedWaiting = 100000;
	
	public SearchForTrainIntercept(Enemy enemy)
	{
        _enemy = enemy;
        //maxWaitTime = enemy.maxTimeToWaitForTrainIntercept; // doesn't exist yet. Might not need to
	}
	
	public void Tick()
	{
		if(timeStartedWaiting + maxWaitTime < Time.time)
        {
            Debug.Log("Waited for train to move too long. Stuck in SearchForTrainIntercept state.");
            // waited too long
            // call a method to get this AI to change states
            //_enemy.WaitedForTrainTooLong();
        }

        //_enemy.InterceptTrain();
	}
	
	private void ChooseNearestTrain()
	{
        /*
         * Few problems:
         * Get InterceptTrain to return a time
         * But how accurate is this time supposed to be?
         * The time is used for when an enemy should give up trying to path to the train because they won't get there in time
         * 
         * Problem 2:
         * There's the callback function that waits for the train to be ready then retries to run InterceptTrain
         * That callback may be obsolete with the AI
         * Or I may need to retool it to work with returning a float instead of void
         * 
         * 
		float maxTimeToIntercept = _enemy.InterceptTrain();
		// returns when the train will intercept that point.
		_enemy.maxTimeToIntercept = maxTimeToIntercept;
        */

        // can I do it like this?
        /*
        float maxTimeToIntercept = _enemy.InterceptTrain();
        if(maxTimeToIntercept < 0)
        {
            // returns a -1 when not ready
            while(_enemy.maxTimeToIntercept < 0)
            {
                // do nothing
            }
            // once the train is ready to go, it updates the time
            // Do I need to give a time here?
            // seems like the enemy class is capable of doing that.
            // this class might just need to call _enemy.InterceptTrain()
            // without even returning anything and just let it handle it.
            // some potential problems:
            // what if the train never moves? then the AI never does anything
        }
        */

        // Easy Way
        _enemy.InterceptTrain();
        // let it handle it
        timeStartedWaiting = Time.time;
	}
	
	public void OnEnter() {
		ChooseNearestTrain();
        _enemy.stateMemory += this;
        //Debug.Log("Entered Search for Train Intercept");
    }
	public void OnExit() {}
}