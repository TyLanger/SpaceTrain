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
		if(Vector3.Distance(_enemy.transform.position, _enemy.TargetBoardingLink.groundPoint.position) < _boardDist)
		{
			_enemy.BoardTrain();
		}
		
		// What if the enemy has a jump?
		// Where it doesn't require a boarding point (like a ladder) to grab onto and climb aboard
	}
	
	public void OnEnter() {
        Debug.Log("Entered "+this);
    }
    public void OnExit() {}
	
}