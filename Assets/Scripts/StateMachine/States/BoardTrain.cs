using UnityEngine;

internal class BoardTrain : IState
{
	
	private readonly Enemy _enemy;
	private readonly BoardingLink _trainBoardingLink;
	private readonly float _boardDist;
	
	public BoardTrain(Enemy enemy)
	{
		_enemy = enemy;
		_trainBoardingLink = enemy.TargetBoardingLink;
		_boardDist = enemy.TrainBoardDist;
		
	}
	
	public void Tick()
	{
		if(Vector3.Distance(_enemy.transform.position, _trainBoardingLink.groundPoint.position) < _boardDist)
		{
			_enemy.BoardTrain();
		}
		
		// What if the enemy has a jump?
		// Where it doesn't require a boarding point (like a ladder) to grab onto and climb aboard
	}
	
	public void OnEnter() {}
	public void OnExit() {}
	
}