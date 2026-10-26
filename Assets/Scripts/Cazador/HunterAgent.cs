using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(ProximitySensor), typeof(AgentFeedback), typeof(HunterEnergy))]
[RequireComponent(typeof(InterestPointSpawner), typeof(HunterDebugDisplay))]
public class HunterAgent : Agent
{
    public enum HunterState { Patrol, Hunt, Gather, Rest }
    [Header("Movimiento")]
    [SerializeField, Min(0.1f)] private float moveSpeed = 8f;
    [SerializeField, Min(0.1f)] private float maxForce = 20f;
    [Header("Patrulla")]
    [SerializeField] private List<Transform> waypoints = new();
    [SerializeField, Min(0.1f)] private float waypointArrivalDistance = 0.4f;
    [SerializeField] private bool pingPongPatrol = true;
    [Header("Percepción y ataque")]
    [SerializeField, Min(0.1f)] private float perceptionRadius = 8f;
    [field: SerializeField, Min(0.01f)] public float TBA { get; private set; } = 2f;
    [field: SerializeField, Min(0.2f)] public float RangeAttackRadius { get; private set; } = 5f;
    [field: SerializeField, Min(0.1f)] public float MeleeAttackRadius { get; private set; } = 1.5f;
    [Header("Recolección")]
    [SerializeField, Min(0.1f)] private float gatherInteractionRadius = 1.2f;
    [SerializeField, Min(0.01f)] private float gatherDuration = 2f;
    [SerializeField, Min(0f)] private float boidRespawnDelay = 5f;
    private readonly FiniteStateMachine<HunterState> _fsm = new();
    private float _attackCooldown;
    private AgentFeedback _feedback;
    public ProximitySensor Sensor { get; private set; }
    public HunterEnergy Stamina { get; private set; }
    public InterestPointSpawner FoodSpawner { get; private set; }
    public HunterState CurrentState { get; private set; }
    public BoidAgent Target { get; private set; }
    public string LastAction { get; private set; } = "-";
    public int Attacks { get; private set; }
    public int Kills { get; private set; }
    public int GatheredCount { get; private set; }
    public List<Transform> Waypoints => waypoints;
    public float WaypointArrivalDistance => waypointArrivalDistance;
    public bool PingPongPatrol => pingPongPatrol;
    public float VisionRadius => perceptionRadius;
    public float Energy => Stamina.Energy;
    public float MaxEnergy => Stamina.MaxEnergy;
    public float GatherInteractionRadius => gatherInteractionRadius;
    public float GatherDuration => gatherDuration;
    public float BoidRespawnDelay => boidRespawnDelay;
    public bool IsAttackReady => _attackCooldown <= 0f;
    public float Cooldown => Mathf.Max(0f, _attackCooldown);

    private void Awake()
    {
        DisablePhysicsInterference();
        Sensor = GetComponent<ProximitySensor>();
        _feedback = GetComponent<AgentFeedback>();
        Stamina = GetComponent<HunterEnergy>();
        FoodSpawner = GetComponent<InterestPointSpawner>();
    }
    private void Start()
    {
        _fsm.AddState(HunterState.Patrol, new HunterPatrolState(this));
        _fsm.AddState(HunterState.Hunt, new HunterHuntState(this));
        _fsm.AddState(HunterState.Gather, new HunterGatherState(this));
        _fsm.AddState(HunterState.Rest, new HunterRestState(this));
        _fsm.ChangeState(HunterState.Patrol);
    }
    private void Update()
    {
        // TBA sigue corriendo también en Patrol, Gather y Rest.
        _attackCooldown -= Time.deltaTime;
        _fsm.Update();
    }
    public void MoveTowards(Vector3 targetPosition)
    {
        Vector3 direction = targetPosition - transform.position;
        direction.y = 0f;
        if (direction.sqrMagnitude < 0.001f) { StopMoving(); return; }
        Vector3 desired = direction.normalized * moveSpeed;
        _velocity += Vector3.ClampMagnitude(desired - _velocity, maxForce * Time.deltaTime);
        _velocity = Vector3.ClampMagnitude(_velocity, moveSpeed);
        Vector3 displacement = Vector3.ClampMagnitude(_velocity * Time.deltaTime, direction.magnitude);
        Vector3 next = transform.position + displacement;
        transform.position = Bounds.Instance != null ? Bounds.Instance.ClampInside(next, 0.5f) : next;
        if (_velocity.sqrMagnitude > 0.001f) transform.forward = _velocity.normalized;
        Physics.SyncTransforms();
    }
    public void Pursue(BoidAgent target)
    {
        float prediction = PlanarDistance(transform.position, target.transform.position) / (moveSpeed + target.Velocity.magnitude);
        MoveTowards(target.transform.position + target.Velocity * prediction);
    }
    public BoidAgent FindNearestLiveBoid() => FindNearestBoid(false);
    public BoidAgent FindNearestEliminatedBoid() => FindNearestBoid(true);
    private BoidAgent FindNearestBoid(bool dead)
    {
        BoidAgent nearest = null;
        float closest = VisionRadius;
        foreach (BoidAgent boid in Sensor.Detect<BoidAgent>(VisionRadius))
        {
            if (dead ? !boid.IsPendingGather : boid.IsEliminated) continue;
            float distance = PlanarDistance(transform.position, boid.transform.position);
            if (distance > closest) continue;
            nearest = boid;
            closest = distance;
        }
        return nearest;
    }
    public void TryAttack(BoidAgent target)
    {
        if (!IsAttackReady || target == null || target.IsEliminated) return;
        float distance = PlanarDistance(transform.position, target.transform.position);
        if (distance > RangeAttackRadius) return;
        bool melee = distance <= MeleeAttackRadius;
        target.ReceiveHits(melee ? 2 : 1);
        _attackCooldown = TBA;
        Attacks++;
        if (target.IsEliminated) Kills++;
        LastAction = melee ? "Cuerpo a cuerpo: 2 golpes" : "A distancia: 1 golpe";
        Debug.Log($"[Cazador] {LastAction}. Vida de {target.name}: {target.Health.HitsRemaining}/2");
    }
    public void SetFeedback(HunterState state, BoidAgent target = null, string action = null)
    {
        CurrentState = state;
        Target = target;
        if (action != null) LastAction = action;
        _feedback.ShowState(state.ToString());
    }
    public void RecordGather() => GatheredCount++;
    private void OnValidate()
    {
        MeleeAttackRadius = Mathf.Max(0.1f, MeleeAttackRadius);
        RangeAttackRadius = Mathf.Max(MeleeAttackRadius + 0.1f, RangeAttackRadius);
    }
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, VisionRadius);
        Gizmos.color = new Color(1f, 0.55f, 0f);
        Gizmos.DrawWireSphere(transform.position, RangeAttackRadius);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, MeleeAttackRadius);
    }
}
