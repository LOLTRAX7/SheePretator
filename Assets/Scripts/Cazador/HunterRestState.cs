using UnityEngine;

public class HunterRestState : State<HunterAgent.HunterState>
{
    private readonly HunterAgent _hunter;
    public HunterRestState(HunterAgent hunter) => _hunter = hunter;
    public override void Enter()
    {
        _hunter.StopMoving();
        _hunter.SetFeedback(HunterAgent.HunterState.Rest, null, "Recargando energía");
    }
    public override void Update()
    {
        _hunter.Stamina.Recharge();
        if (_hunter.Energy >= _hunter.MaxEnergy) _fsm.ChangeState(HunterAgent.HunterState.Patrol);
    }
    public override void Exit() { }
}
