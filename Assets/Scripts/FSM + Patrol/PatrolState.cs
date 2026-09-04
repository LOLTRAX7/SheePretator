using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Estado que se encarga de mover al agente entre los waypoints de patrulla.
public class PatrolState<T> : State<T>
{
    private FSMAgent _agent;
    private PatrolData _data;
    private int _currentIndex = 0;

    // Estado al que volverá cuando termine el tiempo de patrulla.
    private T _restState;

    private Coroutine _restCoroutine;

    public PatrolState(PatrolData data, FSMAgent agent, T restState)
    {
        _data = data;
        _agent = agent;
        _restState = restState;
    }

    public override void Enter()
    {
        Debug.Log("Entre a Patrol");

        // Al entrar comienza una corrutina que espera cinco segundos y luego pide
        // volver al estado indicado en _restState.
        _restCoroutine = _agent.StartCoroutine(TimeToRestRoutine());
    }

    public override void Update()
    {
        // Mientras Patrol está activo, cada frame se ejecuta el movimiento entre waypoints.
        Patrol();
    }

    private void Patrol()
    {
        // Toma el waypoint actual y comprueba si el agente ya llegó suficientemente cerca.
        Transform nextWaypoint = _data.waypoints[_currentIndex];
        if (Vector3.Distance(nextWaypoint.position, _data.transform.position) <= _data.waypointCheckDistance)
        {
            // Cuando llega, avanza al siguiente waypoint. Si llega al último, vuelve al primero.
            _currentIndex = _currentIndex + 1 < _data.waypoints.Count ? _currentIndex + 1 : 0;
            nextWaypoint = _data.waypoints[_currentIndex];
        }

        // Calcula la dirección al waypoint y mueve al agente a su velocidad actual.
        Vector3 dir = nextWaypoint.position - _data.transform.position;

        _data.transform.position += _agent.Speed * Time.deltaTime * dir.normalized;
        _data.transform.forward = dir;
    }

    public override void Exit()
    {
        Debug.Log("Sali de Patrol");

        // Si todavía está esperando en la corrutina, se cancela al salir de Patrol.
        // Así evitamos que la corrutina cambie el estado después de haber salido.
        if (_restCoroutine != null)
        {
            _agent.StopCoroutine(_restCoroutine);
            _restCoroutine = null;
        }
    }

    private IEnumerator TimeToRestRoutine()
    {
        // Espera cinco segundos sin bloquear el resto del juego.
        yield return new WaitForSeconds(5f);

        // Cuando termina la espera, vuelve al estado indicado al crear PatrolState.
        _fsm.ChangeState(_restState);
        _restCoroutine = null;
    }
}

// Contiene los datos que Patrol necesita para saber por dónde moverse.
// Se cargan desde el Inspector de Unity.
[System.Serializable]
public class PatrolData
{
    public List<Transform> waypoints;
    public Transform transform;
    public float waypointCheckDistance;
}
