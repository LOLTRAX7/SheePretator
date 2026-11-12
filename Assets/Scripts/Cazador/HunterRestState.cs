public class HunterRestState : State<HunterAgent.HunterState>
{
    private readonly HunterAgent hunter;
    public HunterRestState(HunterAgent hunter) => this.hunter = hunter;
    public override void Enter()
    {
        hunter.StopMoving();
         hunter.SetFeedback(HunterAgent.HunterState.Rest, null, "Recargando energia");

    }
    public override void Update()
    {

        hunter.Energy.Recharge();
        // Vuelve a patrullar cuando recupera toda la energia.
        if (hunter.Energy.IsFull) _fsm.ChangeState(HunterAgent.HunterState.Patrol);
    }
    
    public override void Exit() { }
}
