using UnityEngine;

// Estado en el que el agente permanece quieto durante un tiempo antes de volver a patrullar.
public class IdleState : State<FSMAgent.States>
{
    readonly float _timeToChangeToPatrol = 3f;
    float _timer = 0f;

    public override void Enter()
    {
        Debug.Log("Entre a Idle");

        // Cada vez que la FSM entra en Idle, el contador vuelve a cero para empezar
        // nuevamente a contar los tres segundos.
        _timer = 0f;
    }

    public override void Update()
    {
        // El contador avanza mientras este estado está activo.
        _timer += Time.deltaTime;

        // Cuando se cumple el tiempo, Idle le pide a la FSM que cambie a Patrol.
        if(_timer >= _timeToChangeToPatrol)
        {
            _fsm.ChangeState(FSMAgent.States.Patrol);
        }
    }

    public override void Exit()
    {
        // Se ejecuta justo antes de abandonar Idle.
        Debug.Log("Sali de idle");
    }

}
