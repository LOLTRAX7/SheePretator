using UnityEngine;

// Todos los cambios de estado del boid se solicitan desde estos estados.
public abstract class BoidLivingState : State<BoidAgent.BoidState>
{
    protected readonly BoidAgent boid;
    private readonly BoidAgent.BoidState _id;
    protected BoidLivingState(BoidAgent agent, BoidAgent.BoidState id) { boid = agent; _id = id; }
    public override void Enter() => boid.EnterState(_id);
    public override void Exit() { }

    public override void Update()
    {
        if (boid.IsEliminated)
        {
            _fsm.ChangeState(BoidAgent.BoidState.Dead);
            return;
        }
        boid.Sense();
        // Prioridad: Dead > Evade > Consume > SeekObject > Flocking.
        BoidAgent.BoidState next = boid.Threat != null ? BoidAgent.BoidState.Evade
            : boid.Food == null ? BoidAgent.BoidState.Flocking
            : boid.Steering.AtFood(boid.Food) ? BoidAgent.BoidState.Consume
            : BoidAgent.BoidState.SeekObject;
        if (next != _id) { _fsm.ChangeState(next); return; }
        Act();
    }
    protected abstract void Act();
}

public class BoidFlockingState : BoidLivingState
{
    public BoidFlockingState(BoidAgent boid) : base(boid, BoidAgent.BoidState.Flocking) { }
    protected override void Act() => boid.Move(boid.Steering.Flocking());
}

public class BoidEvadeState : BoidLivingState
{
    public BoidEvadeState(BoidAgent boid) : base(boid, BoidAgent.BoidState.Evade) { }
    protected override void Act() => boid.Move(boid.Steering.Evade(boid.Threat) + boid.Steering.Separation());
}

public class BoidSeekObjectState : BoidLivingState
{
    public BoidSeekObjectState(BoidAgent boid) : base(boid, BoidAgent.BoidState.SeekObject) { }
    protected override void Act() => boid.Move(boid.Steering.Arrive(boid.Food) + boid.Steering.Separation());
}

public class BoidConsumeState : BoidLivingState
{
    private float _timer;
    public BoidConsumeState(BoidAgent boid) : base(boid, BoidAgent.BoidState.Consume) { }
    public override void Enter() { base.Enter(); boid.StopMoving(); _timer = 0f; }
    protected override void Act()
    {
        boid.StopMoving();
        _timer += Time.deltaTime;
        if (_timer < boid.HarvestInterval) return;
        _timer = 0f;
        boid.Food.TakeDamage(boid.HarvestDamage);
    }
}

public class BoidDeadState : State<BoidAgent.BoidState>
{
    private readonly BoidAgent _boid;
    public BoidDeadState(BoidAgent boid) => _boid = boid;
    public override void Enter() { _boid.EnterState(BoidAgent.BoidState.Dead); _boid.StopMoving(); }
    public override void Update()
    {
        if (_boid.Gathered) _fsm.ChangeState(BoidAgent.BoidState.Respawn);
    }
    public override void Exit() { }
}

public class BoidRespawnState : State<BoidAgent.BoidState>
{
    private readonly BoidAgent _boid;
    private float _timer;
    public BoidRespawnState(BoidAgent boid) => _boid = boid;
    public override void Enter()
    {
        _boid.EnterState(BoidAgent.BoidState.Respawn);
        _boid.Feedback.SetVisible(false);
        _timer = 0f;
    }
    public override void Update()
    {
        _timer += Time.deltaTime;
        if (_timer >= _boid.RespawnDelay && _boid.TryRespawn())
            _fsm.ChangeState(BoidAgent.BoidState.Flocking);
    }
    public override void Exit() { }
}
