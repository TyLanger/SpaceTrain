


internal class SearchForTrainIntercept : IState
{
	private readonly Enemy _enemy;
	
	public SearchForTrainIntercept(Enemy enemy)
	{
        _enemy = enemy;
	}
	
	public void Tick()
	{
		
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
	}
	
	public void OnEnter() {
		ChooseNearestTrain();
	}
	public void OnExit() {}
}