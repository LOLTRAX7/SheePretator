using UnityEngine;

public class HunterGatherState : State<HunterAgent.HunterState>
{
    private readonly HunterAgent _hunter;
    private BoidAgent _target;
    private float _timer;
    public HunterGatherState(HunterAgent hunter) => _hunter = hunter;
    public override void Enter()
    {
        _target = _hunter.FindNearestEliminatedBoid();
        _timer = 0f;
        _hunter.SetFeedback(HunterAgent.HunterState.Gather, _target, "Yendo a recolectar");
    }
    public override void Update()
    {
        if (_target == null || !_target.IsPendingGather || !_target.isActiveAndEnabled)
        {
            _fsm.ChangeState(HunterAgent.HunterState.Patrol);
            return;
        }
        if (Agent.PlanarDistance(_hunter.transform.position, _target.transform.position) > _hunter.GatherInteractionRadius)
        {
            _timer = 0f;
            _hunter.MoveTowards(_target.transform.position);
            return;
        }
        _hunter.StopMoving();
        _timer += Time.deltaTime;
        _hunter.SetFeedback(HunterAgent.HunterState.Gather, _target, "Recolectando");
        if (_timer < _hunter.GatherDuration) return;
        _target.OnGathered(_hunter.BoidRespawnDelay);
        _hunter.RecordGather();
        _fsm.ChangeState(HunterAgent.HunterState.Patrol);
    }
    public override void Exit() => _hunter.StopMoving();
}
