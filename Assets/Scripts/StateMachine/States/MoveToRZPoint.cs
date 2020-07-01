

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
		//_enemy.SetMoveTarget(_enemy.trainRZPoint);
        // would maybe work if there was a navmesh on the ground
        // besides, the enemy already knows its own trainRZPoint
        // should this be:
        _enemy.StartMovingToRZPoint();
		
	}
	
	public void OnExit() {
        // if you leave this, the rz point is dirty and no longer good
        // will need to find a new rz point in order to swap back here
        _enemy.hasTrainRZPoint = false;
    }
}