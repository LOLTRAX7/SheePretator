using UnityEngine;

public class HunterHuntState : State<HunterAgent.HunterState>
{
    private readonly HunterAgent _hunter;
    private BoidAgent _prey;
    private Vector3 _lastKnownPosition;
    public HunterHuntState(HunterAgent hunter) => _hunter = hunter;
    public override void Enter()
    {
        _prey = _hunter.FindNearestLiveBoid();
        _lastKnownPosition = _prey != null ? _prey.transform.position : _hunter.transform.position;
        _hunter.SetFeedback(HunterAgent.HunterState.Hunt, _prey, "Persiguiendo");
    }
    public override void Update()
    {
        _hunter.Stamina.Drain();
        if (_hunter.Energy <= 0f)
        {
            _fsm.ChangeState(HunterAgent.HunterState.Rest);
            return;
        }
        if (_prey != null && Bounds.Instance != null && !Bounds.Instance.Contains(_prey.transform.position))
        {
            _fsm.ChangeState(HunterAgent.HunterState.Patrol);
            return;
        }
        if (_hunter.FindNearestEliminatedBoid() != null)
        {
            _fsm.ChangeState(HunterAgent.HunterState.Gather);
            return;
        }
        // Solo elige otra presa si la anterior murió o dejó de existir.
        if (_prey == null || _prey.IsEliminated)
            _prey = _hunter.FindNearestLiveBoid();
        _hunter.SetFeedback(HunterAgent.HunterState.Hunt, _prey);
        if (_prey == null)
        {
            _hunter.MoveTowards(_lastKnownPosition);
            return;
        }
        _lastKnownPosition = _prey.transform.position;
        // Sigue moviéndose durante el TBA. No se detiene a preparar un ataque.
        _hunter.Pursue(_prey);
        _hunter.TryAttack(_prey);
    }
    public override void Exit() => _hunter.StopMoving();
}
