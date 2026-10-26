using UnityEngine;

public class HunterPatrolState : State<HunterAgent.HunterState>
{
    private readonly HunterAgent _hunter;
    private int _index;
    private int _direction = 1;
    public HunterPatrolState(HunterAgent hunter) => _hunter = hunter;
    public override void Enter() => _hunter.SetFeedback(HunterAgent.HunterState.Patrol, null, "Patrullando");
    public override void Update()
    {
        if (_hunter.FindNearestEliminatedBoid() != null)
        {
            _fsm.ChangeState(HunterAgent.HunterState.Gather);
            return;
        }
        if (_hunter.IsAttackReady && _hunter.FindNearestLiveBoid() != null)
        {
            _fsm.ChangeState(HunterAgent.HunterState.Hunt);
            return;
        }
        _hunter.FoodSpawner.TickPatrol();
        var waypoints = _hunter.Waypoints;
        if (waypoints.Count == 0) { _hunter.StopMoving(); return; }
        _index = Mathf.Clamp(_index, 0, waypoints.Count - 1);
        Transform waypoint = waypoints[_index];
        if (waypoint != null)
        {
            _hunter.MoveTowards(waypoint.position);
            if (Agent.PlanarDistance(_hunter.transform.position, waypoint.position) > _hunter.WaypointArrivalDistance) return;
        }
        if (_hunter.PingPongPatrol && waypoints.Count > 1)
        {
            if (_index + _direction >= waypoints.Count || _index + _direction < 0) _direction *= -1;
            _index += _direction;
        }
        else _index = (_index + 1) % waypoints.Count;
    }
    public override void Exit() => _hunter.StopMoving();
}
