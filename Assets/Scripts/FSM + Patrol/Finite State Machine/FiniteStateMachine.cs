using System.Collections.Generic;
using UnityEngine;

// Motor genérico de FSM. No sabe nada del juego en sí, solo maneja estados.
public class FiniteStateMachine<T>
{
    private State<T> _currentState;

    // Asocia cada ID con su estado.
    private readonly Dictionary<T, State<T>> _allStates = new();

    // Registra un estado bajo un ID.
    public void AddState(T ID, State<T> state)
    {
        state.SetFSM(this); // le pasa la referencia a esta FSM

        if (!_allStates.ContainsKey(ID))
            _allStates.Add(ID, state);
        else
            _allStates[ID] = state; // reemplaza si el ID ya existía
    }

    // Cambia el estado activo, llamando Exit()/Enter() en orden.
    public void ChangeState(T ID)
    {
        if (!_allStates.ContainsKey(ID))
        {
            Debug.LogError("Missing State!");
            return;
        }

        _currentState?.Exit(); // "?." evita error en el primer cambio (no hay estado previo)
        _currentState = _allStates[ID];
        _currentState.Enter();
    }

    // Se llama una vez por frame y delega en el estado activo.
    public void Update()
    {
        if (_currentState != null) _currentState.Update();
    }

    //public void FixedUpdate() => _currentState.FixedUpdate();

}