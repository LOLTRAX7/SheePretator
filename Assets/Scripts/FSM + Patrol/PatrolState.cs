using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Agente recorriendo waypoints. Genérico en T para reutilizarse con otros enums.
public class PatrolState<T> : State<T>
{
    private FSMAgent _agent;
    private PatrolData _data;
    private int _currentIndex = 0;

    private T _restState; // a qué estado volver al terminar de patrullar

    private Coroutine _restCoroutine;

    public PatrolState(PatrolData data, FSMAgent agent, T restState)
    {
        _data = data;
        _agent = agent;
        _restState = restState;
    }

    public override void Enter()
    {
        Debug.LogError("Entre a Patrol");
        _restCoroutine = _agent.StartCoroutine(TimeToRestRoutine());
    }

    public override void Update()
    {
        Patrol();
    }

    // Avanza hacia el waypoint actual y pasa al siguiente al llegar (en loop).
    private void Patrol()
    {
        Transform nextWaypoint = _data.waypoints[_currentIndex];
        if (Vector3.Distance(nextWaypoint.position, _data.transform.position) <= _data.waypointCheckDistance)
        {
            _currentIndex = _currentIndex + 1 < _data.waypoints.Count ? _currentIndex + 1 : 0;
            nextWaypoint = _data.waypoints[_currentIndex];
        }

        Vector3 dir = nextWaypoint.position - _data.transform.position;

        _data.transform.position += _agent.Speed * Time.deltaTime * dir.normalized;
        _data.transform.forward = dir;
    }

    public override void Exit()
    {
        Debug.LogError("Sali de Patrol");
        // Se cancela la corrutina para que no dispare un ChangeState después de haber salido.
        if (_restCoroutine != null)
        {
            _agent.StopCoroutine(_restCoroutine);
            _restCoroutine = null;
        }
    }

    private IEnumerator TimeToRestRoutine()
    {
        yield return new WaitForSeconds(5f);
        _fsm.ChangeState(_restState);
        _restCoroutine = null;
    }
}

// Datos de patrullaje, editables desde el Inspector.
[System.Serializable]
public class PatrolData
{
    public List<Transform> waypoints;
    public Transform transform;
    public float waypointCheckDistance;
}