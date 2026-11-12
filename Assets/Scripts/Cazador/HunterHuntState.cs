using UnityEngine;

public class HunterHuntState : State<HunterAgent.HunterState>
{
    private readonly HunterAgent hunter;
    private BoidAgent target;
    private Vector3 lastPosition;
    public HunterHuntState(HunterAgent hunter) => this.hunter = hunter;
    public override void Enter()
    {
        target = hunter.FindNearestLiveBoid();
        lastPosition = target != null ? target.transform.position : hunter.transform.position;
        hunter.SetFeedback(HunterAgent.HunterState.Hunt, target, "Persiguiendo");
    }
    public override void Update()
    {
        hunter.Energy.Spend();
        if (hunter.Energy.IsEmpty)
        {
            _fsm.ChangeState(HunterAgent.HunterState.Rest);
            return;
        }

        if (target != null && Bounds.Instance != null && !Bounds.Instance.Contains(target.transform.position))
        {
            _fsm.ChangeState(HunterAgent.HunterState.Patrol);
            return;
        }

        if (hunter.FindNearestEliminatedBoid() != null)
        {
            _fsm.ChangeState(HunterAgent.HunterState.Gather);
            return;
        }

        // La presa se conserva aunque salga del radio de vision.
        if (target == null || target.IsEliminated || !target.gameObject.activeInHierarchy)
            target = hunter.FindNearestLiveBoid();
        if (target == null)
        {
            hunter.MoveTowards(lastPosition);
            hunter.SetFeedback(HunterAgent.HunterState.Hunt, null, "Buscando en la ultima posicion");
            return;
        }

        lastPosition = target.transform.position;
        float distance = Vector3.Distance(hunter.transform.position, target.transform.position);
        hunter.SetFeedback(HunterAgent.HunterState.Hunt, target, "Persiguiendo");

        if (hunter.IsAttackReady)
        {
            if (distance <= hunter.MeleeAttackRadius) hunter.PerformMeleeAttack(target);
            else if (distance <= hunter.RangeAttackRadius) hunter.PerformRangedAttack(target);
        }
        
        if (distance > hunter.MeleeAttackRadius) hunter.Pursue(target);
        else hunter.StopMoving();
    }
    public override void Exit() => hunter.StopMoving();
}
