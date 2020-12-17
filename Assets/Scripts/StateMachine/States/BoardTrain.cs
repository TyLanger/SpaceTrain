using UnityEngine;

internal class BoardTrain : IState
{
	
	private readonly Enemy _enemy;
	//private readonly BoardingLink _trainBoardingLink;
	private readonly float _boardDist;
	
	public BoardTrain(Enemy enemy)
	{
		_enemy = enemy;
		//_trainBoardingLink = enemy.TargetBoardingLink; // not evaluated until search for train intercept
        // so it is null when this state is created
		_boardDist = enemy.TrainBoardDist;
		
	}
	
	public void Tick()
	{
        // could move towards the link pos instead of just standing still?
        // would still be awkward. Enemy would move towards the train, get to the train, then make a 90 turn and run the opposite direction of the train for 2 steps to board
		if(Vector3.Distance(_enemy.transform.position, _enemy.TargetBoardingLink.groundPoint.position) < _boardDist)
		{
			_enemy.BoardTrain();
		}
		
		// What if the enemy has a jump?
		// Where it doesn't require a boarding point (like a ladder) to grab onto and climb aboard
	}
	
	public void OnEnter() {
        _enemy.stateMemory += this;
        //Debug.Log("Entered "+this);
    }
    public void OnExit() {
    }
	
}