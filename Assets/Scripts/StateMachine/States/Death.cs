using UnityEngine;

public class Death : IState
{
    public Death()
    {

    }

    public void Tick()
    {

    }

    public void OnEnter()
    {
        Debug.Log("Entered " + this);
    }

    public void OnExit() { }


}
