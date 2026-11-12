using UnityEngine;

public abstract class BoidLivingState : State<BoidState>
{


    protected readonly BoidAgent boid;
    protected BoidLivingState(BoidAgent boid) => this.boid = boid;

    public override void Enter() { }
    public override void Exit(){ }

    // Morir y huir tienen prioridad sobre buscar comida.
    protected bool ChangeIfNeeded()
    {
        BoidState next = BoidState.Flocking;

        if (boid.IsEliminated) next = BoidState.Dead;
        else if (boid.Threat != null) next = BoidState.Evade;
        else if (boid.AtPoint) next = BoidState.Consume;
        else if (boid.HasPoint) next = BoidState.SeekObject;
        if (next == boid.CurrentState) return false;

        _fsm.ChangeState(next);

        return true;
    }
}
public class BoidFlockingState : BoidLivingState
{
    public BoidFlockingState(BoidAgent boid) : base(boid) { }

    public override void Update()
    {
        if (ChangeIfNeeded()) return;
        boid.Flock();
    }

}

public class BoidEvadeState : BoidLivingState
{
   
    public BoidEvadeState(BoidAgent boid) : base(boid) { }
    
    public override void Update()
    {
        if (ChangeIfNeeded()) return;
        boid.EvadeThreat();
    }
}

public class BoidSeekObjectState : BoidLivingState
{
    public BoidSeekObjectState(BoidAgent boid) : base(boid) { }
    public override void Update()
    {
        if (ChangeIfNeeded()) return;
        boid.ArriveAtPoint();
    }
}


public class BoidConsumeState : BoidLivingState
{
    private float timer;
    public BoidConsumeState(BoidAgent boid) : base(boid) { }

    public override void Enter()
    {
        timer = 0f;
        boid.StopMoving();
    }


    public override void Update()
    {
        if (ChangeIfNeeded()) return;
        boid.StopMoving();
        timer += Time.deltaTime;

        // Consume el objeto por intervalos.
        if (timer < boid.HarvestInterval) return;
        timer = 0f;
        boid.ConsumePoint();
    }
}

public class BoidDeadState : State<BoidState>
{
    private readonly BoidAgent boid;
    public BoidDeadState(BoidAgent boid) => this.boid = boid;
    public override void Enter() => boid.StopMoving();

    public override void Update()
    {

        // Espera inmovil hasta que el cazador la recolecta.
        if (boid.WasGathered) _fsm.ChangeState(BoidState.Respawn);

    }

    public override void Exit() { }
}



public class BoidRespawnState : State<BoidState>
{
    private readonly BoidAgent boid;
    private float timer;
    public BoidRespawnState(BoidAgent boid) => this.boid = boid;

    public override void Enter()
    {
        timer = boid.RespawnDelay;
        // Oculta la oveja mientras espera para reaparecer.
        boid.Hide();

    }

    public override void Update()
    {
        timer -= Time.deltaTime;
        if (timer <= 0f && boid.TryRespawn()) _fsm.ChangeState(BoidState.Flocking);
    }

    public override void Exit() { }
}
