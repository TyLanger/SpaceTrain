using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


// Tutorial from https://youtu.be/YdERlPfwUb0

public class StateMachine : MonoBehaviour {

    private Dictionary<Type, BaseState> availableStates;

    public BaseState currentState { get; private set; }
    public event Action<BaseState> OnStateChanged;

	public void SetStates(Dictionary<Type, BaseState> states)
    {
        availableStates = states;

    }

	// Update is called once per frame
	void Update () {
	    if(currentState == null)
        {
            currentState = availableStates[typeof(BaseState)];
            //currentState = availableStates.Values.First();
        }

        // ?. checks to see if current state is null first
        var nextState = currentState?.Tick();

        if(nextState != null && nextState != currentState?.GetType())
        {
            SwitchToNewState(nextState);
        }
	}

    private void SwitchToNewState(Type nextState)
    {
        currentState = availableStates[nextState];
        OnStateChanged?.Invoke(currentState);
    }
}
