using System.Collections.Generic;
using UnityEngine;

public class HunterAgent : Agent
{
    public enum HunterState { Patrol, Hunt, Gather, Rest }

    [SerializeField, Min(0.1f)] private float moveSpeed = 8f;
    [SerializeField] private List<Transform> waypoints = new List<Transform>();
    [SerializeField, Min(0.05f)] private float waypointArrivalDistance = 0.4f;
    [SerializeField] private bool pingPongPatrol = true;
    [SerializeField] private InterestPoint interestPointPrefab;
    [SerializeField, Min(0.1f)] private float poiSpawnInterval = 4f;
    [SerializeField, Range(1, 5)] private int maxActiveInterestPoints = 5;
    [SerializeField, Min(0.1f)] private float perceptionRadius = 8f;
    [field: SerializeField, Min(0.01f)] public float TBA { get; private set; } = 2f;
    [field: SerializeField, Min(0.2f)] public float RangeAttackRadius { get; private set; } = 5f;
    [field: SerializeField, Min(0.1f)] public float MeleeAttackRadius { get; private set; } = 1.5f;
    [SerializeField, Min(0.1f)] private float gatherInteractionRadius = 1.2f;
    [SerializeField, Min(0.01f)] private float gatherDuration = 2f;
    [SerializeField, Min(0f)] private float boidRespawnDelay = 5f;

    private readonly FiniteStateMachine<HunterState> fsm = new FiniteStateMachine<HunterState>();

    private float attackCooldown;
    private string action = "";
    private BoidAgent displayedTarget;
    public HunterState CurrentState => fsm.CurrentState;
    public HunterEnergy Energy { get; private set; }
    public List<Transform> Waypoints => waypoints;

    public float WaypointArrivalDistance => waypointArrivalDistance;
    public bool PingPongPatrol => pingPongPatrol;
    public float VisionRadius => perceptionRadius;
    public float PoiSpawnInterval => poiSpawnInterval;
    public float GatherInteractionRadius => gatherInteractionRadius;
    public float GatherDuration => gatherDuration;
    public float BoidRespawnDelay => boidRespawnDelay;
    public bool IsAttackReady => attackCooldown <= 0f;


    private void Awake()
    {

        InitializeAgent();

        Energy = GetComponent<HunterEnergy>();

        if (Energy == null) Energy = gameObject.AddComponent<HunterEnergy>();
        fsm.AddState(HunterState.Patrol, new HunterPatrolState(this));
        fsm.AddState(HunterState.Hunt, new HunterHuntState(this));
        fsm.AddState(HunterState.Gather, new HunterGatherState(this));
        fsm.AddState(HunterState.Rest, new HunterRestState(this));

        fsm.ChangeState(HunterState.Patrol);
    }
    private void Update()
    {
        // El tiempo entre ataques sigue pasando en todos los estados.
        attackCooldown = Mathf.Max(0f, attackCooldown - Time.deltaTime);

        sensor.Scan(perceptionRadius);

        fsm.Update();
        UpdateFeedback();
    }
    public void MoveTowards(Vector3 targetPosition)
    {

        float distance = Vector3.Distance(transform.position, targetPosition);
        float speed = Mathf.Min(moveSpeed, distance / Mathf.Max(Time.deltaTime, 0.0001f));

        Move(DesiredVelocity(targetPosition, speed), false);
    }



    public void Pursue(BoidAgent target) => MoveTowards(PredictPosition(target, moveSpeed));
    public BoidAgent FindNearestLiveBoid() => FindNearestBoid(false);
    public BoidAgent FindNearestEliminatedBoid() => FindNearestBoid(true);


    private BoidAgent FindNearestBoid(bool eliminated)
    {
        BoidAgent nearest = null;
        float distance = perceptionRadius;

        foreach (BoidAgent boid in sensor.Boids)
        {

            if (!sensor.InRange(boid, distance)) continue;
            if (eliminated ? !boid.IsPendingGather : boid.IsEliminated) continue;

            nearest = boid;
            distance = Vector3.Distance(transform.position, boid.transform.position);

        }
        return nearest;
    }


    // Cuerpo a cuerpo cuenta como dos golpes; a distancia, uno.
    public void PerformMeleeAttack(BoidAgent target) => Attack(target, 2, MeleeAttackRadius);
    public void PerformRangedAttack(BoidAgent target) => Attack(target, 1, RangeAttackRadius);


    private void Attack(BoidAgent target, int hits, float radius)
    {
        if (!IsAttackReady || target == null || target.IsEliminated || !sensor.InRange(target, radius)) return;
        target.TakeDamage(hits);

        attackCooldown = TBA;
        action = hits == 2 ? "Ataque cuerpo a cuerpo" : "Ataque a distancia";

    }

    public bool HasRoomForInterestPoint()
    {
        return interestPointPrefab != null && InterestPoint.ActiveCount < Mathf.Min(5, maxActiveInterestPoints);
    }

    public void SpawnInterestPointAtCurrentPosition()
    {
        if (!HasRoomForInterestPoint()) return;
        Vector3 position = transform.position;

        foreach (Collider hit in Physics.OverlapSphere(position, 1.1f))
        {
            
            if (hit.GetComponentInParent<BoidAgent>() != null || hit.GetComponentInParent<InterestPoint>() != null) return;
        }

        Instantiate(interestPointPrefab, position, Quaternion.identity);
        Physics.SyncTransforms();
    }


    public void SetFeedback(HunterState state, BoidAgent target = null, string action = "")
    {

        displayedTarget = target;
        this.action = action;
    }
    private void UpdateFeedback()
    {
        string state = "Patrullando";
        Color color = Color.white;

        switch (CurrentState)
        {
            case HunterState.Hunt: state = "Cazando"; color = Color.red; break;

            case HunterState.Gather: state = "Recolectando"; color = Color.yellow; break;
            case HunterState.Rest: state = "Descansando"; color = Color.cyan; break;
        }

        string target = displayedTarget != null ? displayedTarget.name : "-";
        feedback.Set($"Cazador: {state} | Energia: {Energy.Current:0}/{Energy.Maximum:0}\nObjetivo: {target} | {action}", color);
    }


    private void OnValidate()
    {

        RangeAttackRadius = Mathf.Max(0.2f, RangeAttackRadius);
        MeleeAttackRadius = Mathf.Clamp(MeleeAttackRadius, 0.1f, RangeAttackRadius - 0.05f);
    }
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, perceptionRadius);

        Gizmos.color = new Color(1f, 0.55f, 0f);
        Gizmos.DrawWireSphere(transform.position, RangeAttackRadius);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, MeleeAttackRadius);
    }
}
