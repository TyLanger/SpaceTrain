

using UnityEngine;

internal class BoardTrain : IState
{
	
	private readonly Enemy _enemy;
	private readonly BoardingLink _trainBoardingPoint;
	private readonly float _boardDist;
	
	public BoardTrain(Enemy enemy)
	{
		_enemy = enemy;
		_trainBoardingPoint = enemy.TargetBoardingPoint;
		_boardDist = enemy.TrainBoardDist;
		
	}
	
	public void Tick()
	{
		if(Vector3.Distance(_enemy.transform.position, _trainBoardingPoint.transform.position) < _boardDist)
		{
			// doesn't exist
			// currently, the train just has trigger bodies that the enemy collides with
			// then it teleports onto the train.
			_enemy.BoardTrain();
		}
		
		// What if the enemy has a jump?
		// Where it doesn't require a boarding point (like a ladder) to grab onto and climb aboard
	}
	
	public void OnEnter() {}
	public void OnExit() {}
	
}