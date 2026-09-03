using System.Collections.Generic;
using UnityEngine;

// Arma y hace correr la FSM de este agente.
public class FSMAgent : MonoBehaviour
{
    [SerializeField]
    private float _speed = 3f;
    public float Speed => _speed;

    [SerializeField]
    private PatrolData _patrolData;

    // La FSM, tipada con el enum States de acá abajo.
    private readonly FiniteStateMachine<States> _fsm = new();
    public enum States { Idle, Patrol, Death }

    public int HealthPoints;

    //private Animator _animator;
    //private readonly int _walkAnimBool = Animator.StringToHash("Walk");

    private void Start()
    {
        // Crea e instancia los estados...
        var idle = new IdleState();
        var patrol = new PatrolState<States>(_patrolData, this, States.Idle);

        // ...y los registra en la FSM.
        _fsm.AddState(States.Idle, idle);
        _fsm.AddState(States.Patrol, patrol);

        _fsm.ChangeState(States.Idle); // estado inicial
    }
    private void Update()
    {
        _fsm.Update(); // le da el "tick" a la FSM cada frame
    }

    //private void ChangeToWalkAnimation() => _animator.SetBool(_walkAnimBool, true);

    public void TakeDamage(int damage)
    {
        HealthPoints -= damage;
        // Cambio de estado disparado desde afuera (no por Idle ni Patrol).
        // Ojo: States.Death todavía no tiene clase creada ni registrada.
        if (HealthPoints <= 0) _fsm.ChangeState(States.Death);
    }

}

/*public enum States
{
    Idle, 
    Patrol,
    Attack,
    Death,
    Revive,
    Stun,

}*/
/*
public enum GameStates
{
    Menu,
    Game,
    Pause
}*/