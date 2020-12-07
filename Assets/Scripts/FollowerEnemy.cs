using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FollowerEnemy : Enemy
{
    

    protected override void StateMachineSetup()
    {

        // States
        // What states are different?
        // Search for a target point
        // move to target point
        // find targets to shoot at

        // Maybe it should have a number of distinct positions
        // When it's close, shoot small arms fire
        // when far away, shoot mortar fire
        // Or support the other units

        // Can I reuse any of the state classes I already have?
        // Should I be able to?
        // If I can't reuse some, does that mean I should rework them so that I can
        // Or make new ones?

        if(trainEngine == null)
        {
            Debug.Log("Follower StateMachineSetup. No Train");
        }

        _stateMachine = new StateMachine();

        var search = new SearchForTrainIntercept(this);

        _stateMachine.SetState(search);
    }



    public override void InterceptTrain()
    {
        //Debug.Log("Horse");
        ApproachTrain();
    }

    public void ApproachTrain()
    {
        //Debug.Log("Approach Horse");


        Vector3 trainPos = trainEngine.transform.position;

        float difference = transform.position.x - trainPos.x;
        // if this is positive, you're on the right side

        TargetMarker.position = trainPos + new Vector3(10*Mathf.Sign(difference), 0, 0);
        TargetMarker.parent = trainEngine.transform;
    }

}
