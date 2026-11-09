using UnityEngine;

public class HunterPatrolState : State<HunterAgent.HunterState>
{
    private readonly HunterAgent hunter;
    private int index;
    private int direction = 1;
    private float spawnTimer;
    public HunterPatrolState(HunterAgent hunter) => this.hunter = hunter;
    public override void Enter() => hunter.SetFeedback(HunterAgent.HunterState.Patrol);

    public override void Update()
    {
        if (hunter.FindNearestEliminatedBoid() != null)
        {
            _fsm.ChangeState(HunterAgent.HunterState.Gather);
            return;
        }
        if (hunter.IsAttackReady && hunter.FindNearestLiveBoid() != null)
        {
            _fsm.ChangeState(HunterAgent.HunterState.Hunt);
            return;
        }
        Patrol();
        spawnTimer += Time.deltaTime;
        if (spawnTimer < hunter.PoiSpawnInterval) return;
        spawnTimer = 0f;
        hunter.SpawnInterestPointAtCurrentPosition();
    }

    private void Patrol()
    {
        if (hunter.Waypoints.Count == 0) return;
        index = Mathf.Clamp(index, 0, hunter.Waypoints.Count - 1);
        Transform waypoint = hunter.Waypoints[index];
        if (waypoint != null)
        {
            hunter.MoveTowards(waypoint.position);
            if (Vector3.Distance(hunter.transform.position, waypoint.position) > hunter.WaypointArrivalDistance) return;
        }
        if (!hunter.PingPongPatrol) index = (index + 1) % hunter.Waypoints.Count;
        else
        {
            if (index + direction < 0 || index + direction >= hunter.Waypoints.Count) direction *= -1;
            index = Mathf.Clamp(index + direction, 0, hunter.Waypoints.Count - 1);
        }
    }

    public override void Exit() => hunter.StopMoving();
}
