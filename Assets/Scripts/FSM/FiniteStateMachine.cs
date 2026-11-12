using System.Collections.Generic;
using UnityEngine;

public class FiniteStateMachine<T>
{
    private State<T> _currentState;
    public T CurrentState { get; private set; }
    private readonly Dictionary<T, State<T>> _allStates = new();
    public void Update()
        {
            if (_currentState != null) _currentState.Update();
        }
    public void AddState(T ID, State<T> state)
    {

        state.SetFSM(this);
        if (!_allStates.ContainsKey(ID))
            _allStates.Add(ID, state);
        else
            _allStates[ID] = state;

    }
    public void ChangeState(T ID)
    {

        if (!_allStates.ContainsKey(ID))
        {
            Debug.LogError("Missing State!");
            return;
        }

        // Sale del estado anterior y entra al nuevo.
        _currentState?.Exit();
        _currentState = _allStates[ID];
        CurrentState = ID;
        _currentState.Enter();

    }
}
