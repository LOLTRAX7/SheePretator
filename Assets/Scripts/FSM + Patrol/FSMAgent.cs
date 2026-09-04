using System.Collections.Generic;
using UnityEngine;

// Es el agente que utiliza la FSM. Crea los estados y luego deja que la FSM controle
// cuál está activo en cada momento.
public class FSMAgent : MonoBehaviour
{
    [SerializeField]
    private float _speed = 3f;
    public float Speed => _speed;

    [SerializeField]
    private PatrolData _patrolData;

    // Esta FSM usa States como identificadores para saber qué estado debe activar.
    private readonly FiniteStateMachine<States> _fsm = new();
    public enum States { Idle, Patrol, Death }

    public int HealthPoints;

    //private Animator _animator;
    //private readonly int _walkAnimBool = Animator.StringToHash("Walk");

    private void Start()
    {
        // Crea las instancias de los estados que va a utilizar este agente.
        // Patrol recibe el estado al que debe volver cuando termine su tiempo de patrulla.
        var idle = new IdleState();
        var patrol = new PatrolState<States>(_patrolData, this, States.Idle);

        // Registra cada estado dentro de la FSM usando su identificador.
        _fsm.AddState(States.Idle, idle);
        _fsm.AddState(States.Patrol, patrol);

        // Define Idle como el primer estado activo del agente.
        _fsm.ChangeState(States.Idle);
    }

    private void Update()
    {
        // La FSM recibe una actualización por frame y se encarga de actualizar
        // solamente al estado que está activo.
        _fsm.Update();
    }

    //private void ChangeToWalkAnimation() => _animator.SetBool(_walkAnimBool, true);

    public void TakeDamage(int damage)
    {
        // Reduce la vida y, cuando llega a cero, intenta pasar al estado Death.
        HealthPoints -= damage;
        // Death existe en el enum, pero en este código todavía no se registró un DeathState.
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
