using UnityEngine;

public enum BoidState { Flocking, Evade, SeekObject, Consume, Dead, Respawn }

public class BoidAgent : Agent
{
    [SerializeField, Min(0.1f)] private float maxSpeed = 4f;
    [SerializeField, Min(0.1f)] private float maxForce = 6f;
    [SerializeField, Min(0.1f)] private float separationRadius = 2f;
    [SerializeField, Min(0.2f)] private float neighborRadius = 5f;
    [SerializeField, Range(0f, 5f)] private float separationWeight = 1.6f;
    [SerializeField, Range(0f, 5f)] private float alignmentWeight = 1f;
    [SerializeField, Range(0f, 5f)] private float cohesionWeight = 1f;
    [SerializeField, Min(0.1f)] private float hunterVisionRadius = 6f;
    [SerializeField, Range(0f, 5f)] private float evadeWeight = 2.2f;
    [SerializeField, Range(0f, 1f)] private float separationWeightWhileEvading = 0.35f;
    [SerializeField, Min(0.1f)] private float poiVisionRadius = 10f;
    [SerializeField, Min(0.1f)] private float arriveSlowingDistance = 3f;
    [SerializeField, Min(0.1f)] private float arriveMinDistance = 1.2f;
    [SerializeField, Min(0.05f)] private float harvestInterval = 1f;
    [SerializeField, Min(0.1f)] private float harvestDamage = 10f;
    [SerializeField, Min(0f)] private float respawnDelay = 5f;

    private readonly FiniteStateMachine<BoidState> fsm = new FiniteStateMachine<BoidState>();
    private BoidHealth health;
    private BoidSteering steering;
    private bool gathered;
    public HunterAgent Threat { get; private set; }
    public InterestPoint TargetPoint { get; private set; }
    public BoidState CurrentState => fsm.CurrentState;
    public bool IsEliminated => health.IsDead;
    public bool IsPendingGather => health.IsDead && !gathered;
    public bool WasGathered => gathered;
    public float RespawnDelay => respawnDelay;
    public float HarvestInterval => harvestInterval;
    public bool HasPoint => TargetPoint != null && TargetPoint.IsAvailable && sensor.InRange(TargetPoint, poiVisionRadius);
    public float ObjectStopDistance => Mathf.Max(arriveMinDistance, BodyRadius + TargetPoint.BodyRadius + 0.08f);
    public bool AtPoint => HasPoint && Vector3.Distance(transform.position, TargetPoint.transform.position) <= ObjectStopDistance + 0.08f;
    public ProximitySensor Sensor => sensor;
    public float Speed => maxSpeed;
    public float Force => maxForce;
    public float SeparationRadius => separationRadius;
    public float NeighborRadius => neighborRadius;

    private void Awake()
    {
        InitializeAgent();
        health = GetComponent<BoidHealth>();
        if (health == null) health = gameObject.AddComponent<BoidHealth>();
        steering = new BoidSteering(this);
        fsm.AddState(BoidState.Flocking, new BoidFlockingState(this));
        fsm.AddState(BoidState.Evade, new BoidEvadeState(this));
        fsm.AddState(BoidState.SeekObject, new BoidSeekObjectState(this));
        fsm.AddState(BoidState.Consume, new BoidConsumeState(this));
        fsm.AddState(BoidState.Dead, new BoidDeadState(this));
        fsm.AddState(BoidState.Respawn, new BoidRespawnState(this));
        _velocity = RandomDirection() * maxSpeed;
        fsm.ChangeState(BoidState.Flocking);
    }

    private void Update()
    {
        if (!IsEliminated)
        {
            sensor.Scan(Mathf.Max(neighborRadius, hunterVisionRadius, poiVisionRadius));
            UpdatePerception();
        }
        fsm.Update();
        UpdateFeedback();
    }

    private void UpdatePerception()
    {
        Threat = null;
        float nearest = hunterVisionRadius;
        foreach (HunterAgent hunter in sensor.Hunters)
        {
            if (!sensor.InRange(hunter, nearest)) continue;
            Threat = hunter;
            nearest = Vector3.Distance(transform.position, hunter.transform.position);
        }
        if (HasPoint) return;
        TargetPoint = null;
        nearest = poiVisionRadius;
        foreach (InterestPoint point in sensor.Points)
        {
            if (point == null || !point.IsAvailable || !sensor.InRange(point, nearest)) continue;
            TargetPoint = point;
            nearest = Vector3.Distance(transform.position, point.transform.position);
        }
    }

    public void Flock()
    {
        Vector3 force = steering.Separation() * separationWeight + steering.Alignment() * alignmentWeight +
            steering.Cohesion() * cohesionWeight + steering.KeepInsideBounds();
        ApplySteering(force);
    }

    public void EvadeThreat()
    {
        Vector3 future = PredictPosition(Threat, maxSpeed);
        Vector3 evade = Steering(-DesiredVelocity(future, maxSpeed), maxForce);
        ApplySteering(evade * evadeWeight + steering.Separation() * separationWeight * separationWeightWhileEvading +
            steering.KeepInsideBounds());
    }

    public void ArriveAtPoint()
    {
        float distance = Vector3.Distance(transform.position, TargetPoint.transform.position);
        float speed = maxSpeed * Mathf.Clamp01((distance - ObjectStopDistance) / arriveSlowingDistance);
        Vector3 desired = DesiredVelocity(TargetPoint.transform.position, speed);
        ApplySteering(Steering(desired, maxForce) + steering.Separation() * separationWeight);
    }

    private void ApplySteering(Vector3 force)
    {
        Vector3 velocity = Vector3.ClampMagnitude(_velocity + force, maxSpeed);
        Move(velocity, true);
    }

    public void ConsumePoint() => TargetPoint.TakeDamage(harvestDamage);
    public void TakeDamage(int hits) => health.TakeHits(hits);

    public void OnGathered(float delay)
    {
        if (!IsPendingGather) return;
        respawnDelay = Mathf.Max(0f, delay);
        gathered = true;
    }

    public void Hide() => feedback.SetVisible(false);

    public bool TryRespawn()
    {
        if (Bounds.Instance == null || !Bounds.Instance.TryGetFreePoint(BodyRadius, this, transform.position.y, out Vector3 position))
            return false;
        transform.position = position;
        health.Restore();
        gathered = false;
        TargetPoint = null;
        Threat = null;
        _velocity = RandomDirection() * maxSpeed;
        feedback.SetVisible(true);
        Physics.SyncTransforms();
        return true;
    }

    private Vector3 RandomDirection()
    {
        Vector3 direction = new Vector3(Random.Range(-1f, 1f), 0f, Random.Range(-1f, 1f));
        return direction.sqrMagnitude < 0.001f ? Vector3.forward : direction.normalized;
    }

    private void UpdateFeedback()
    {
        string state;
        Color color;
        switch (CurrentState)
        {
            case BoidState.Evade: state = "Huyendo"; color = new Color(1f, 0.55f, 0f); break;
            case BoidState.SeekObject: state = "Yendo al objeto"; color = Color.cyan; break;
            case BoidState.Consume: state = "Consumiendo"; color = Color.green; break;
            case BoidState.Dead: state = "Eliminada"; color = Color.gray; break;
            case BoidState.Respawn: state = "Reapareciendo"; color = Color.gray; break;
            default: state = "En grupo"; color = Color.white; break;
        }
        feedback.Set($"Oveja: {state}\nVida: {health.Current}/2", color);
    }

    private void OnValidate()
    {
        neighborRadius = Mathf.Max(neighborRadius, 0.2f);
        separationRadius = Mathf.Clamp(separationRadius, 0.1f, neighborRadius - 0.05f);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, separationRadius);
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, neighborRadius);
        Gizmos.color = new Color(1f, 0.55f, 0f);
        Gizmos.DrawWireSphere(transform.position, hunterVisionRadius);
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, poiVisionRadius);
    }
}
