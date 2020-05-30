

internal class MoveToRZPoint : IState
{
	
	private readonly Enemy _enemy;
	
	public MoveToRZPoint(Enemy enemy)
	{
		_enemy = enemy;
	}
	
	public void Tick()
	{
		// check if stuck
	}
	
	public void OnEnter()
	{
		_enemy.SetMoveTarget(_enemy.trainRZPoint);
		
	}
	
	public void OnExit() {}
}