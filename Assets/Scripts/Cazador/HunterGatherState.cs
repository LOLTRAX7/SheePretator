using UnityEngine;

public class HunterGatherState : State<HunterAgent.HunterState>
{
    private readonly HunterAgent hunter;
    private BoidAgent target;
    private float timer;
    public HunterGatherState(HunterAgent hunter) => this.hunter = hunter;




    public override void Enter()
    {
        target = hunter.FindNearestEliminatedBoid();
        timer = 0f;
        hunter.SetFeedback(HunterAgent.HunterState.Gather, target, "Yendo a recolectar");
    }

    public override void Update()
    {
        if (target == null || !target.gameObject.activeInHierarchy || !target.IsPendingGather)
        {
            _fsm.ChangeState(HunterAgent.HunterState.Patrol);
            return;
        }
        
        if (Vector3.Distance(hunter.transform.position, target.transform.position) > hunter.GatherInteractionRadius)
        {
            timer = 0f;
            hunter.MoveTowards(target.transform.position);
            return;
        }

        hunter.StopMoving();
        hunter.SetFeedback(HunterAgent.HunterState.Gather, target, "Recolectando");
        timer += Time.deltaTime;

        if (timer < hunter.GatherDuration) return;

        // Al terminar la recoleccion, la oveja puede reaparecer.
        target.OnGathered(hunter.BoidRespawnDelay);
        _fsm.ChangeState(HunterAgent.HunterState.Patrol);
    }

    public override void Exit() => hunter.StopMoving();
}
