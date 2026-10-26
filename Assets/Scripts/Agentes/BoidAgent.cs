using UnityEngine;

[RequireComponent(typeof(BoidHealth), typeof(AgentFeedback), typeof(ProximitySensor))]
[RequireComponent(typeof(BoidSteering))]
public class BoidAgent : Agent
{
    public enum BoidState { Flocking, Evade, SeekObject, Consume, Dead, Respawn }
    [Header("Percepción")]
    [SerializeField, Min(0.1f)] private float hunterVisionRadius = 8f;
    [SerializeField, Min(0.1f)] private float poiVisionRadius = 10f;
    [Header("Consumo")]
    [SerializeField, Min(0.01f)] private float harvestInterval = 1f;
    [SerializeField, Min(0.1f)] private float harvestDamage = 10f;

    private readonly FiniteStateMachine<BoidState> _fsm = new();
    public BoidHealth Health { get; private set; }
    public BoidSteering Steering { get; private set; }
    public AgentFeedback Feedback { get; private set; }
    public ProximitySensor Sensor { get; private set; }
    public BoidState CurrentState { get; private set; }
    public HunterAgent Threat { get; private set; }
    public InterestPoint Food { get; private set; }
    public bool IsEliminated => Health.IsDead;
    public bool IsPendingGather => IsEliminated && !_gathered;
    public float HarvestInterval => harvestInterval;
    public float HarvestDamage => harvestDamage;
    public float RespawnDelay { get; private set; }
    public bool Gathered => _gathered;
    private bool _gathered;

    private void Awake()
    {
        DisablePhysicsInterference();
        Health = GetComponent<BoidHealth>();
        Steering = GetComponent<BoidSteering>();
        Feedback = GetComponent<AgentFeedback>();
        Sensor = GetComponent<ProximitySensor>();
    }

    private void Start()
    {
        _fsm.AddState(BoidState.Flocking, new BoidFlockingState(this));
        _fsm.AddState(BoidState.Evade, new BoidEvadeState(this));
        _fsm.AddState(BoidState.SeekObject, new BoidSeekObjectState(this));
        _fsm.AddState(BoidState.Consume, new BoidConsumeState(this));
        _fsm.AddState(BoidState.Dead, new BoidDeadState(this));
        _fsm.AddState(BoidState.Respawn, new BoidRespawnState(this));
        _velocity = RandomDirection() * Steering.MaxSpeed;
        _fsm.ChangeState(BoidState.Flocking);
    }

    private void Update() => _fsm.Update();

    public void Sense()
    {
        Threat = Sensor.Nearest<HunterAgent>(hunterVisionRadius);
        if (Food == null || !Food.isActiveAndEnabled
            || PlanarDistance(transform.position, Food.transform.position) > poiVisionRadius)
            Food = Sensor.Nearest<InterestPoint>(poiVisionRadius);
    }

    public void EnterState(BoidState state)
    {
        CurrentState = state;
        Feedback.ShowState(state.ToString());
    }

    public void Move(Vector3 force)
    {
        _velocity = Vector3.ClampMagnitude(_velocity + Vector3.ClampMagnitude(force, Steering.MaxForce)
            * Time.deltaTime, Steering.MaxSpeed);
        if (Bounds.Instance != null)
        {
            Vector3 next = transform.position + _velocity * Time.deltaTime;
            Vector3 inside = Bounds.Instance.ClampInside(next, Steering.BodyRadius);
            if (!Mathf.Approximately(next.x, inside.x)) _velocity.x = -_velocity.x;
            if (!Mathf.Approximately(next.z, inside.z)) _velocity.z = -_velocity.z;
        }
        Steering.MoveWithoutOverlap(_velocity * Time.deltaTime);
        if (_velocity.sqrMagnitude > 0.001f) transform.forward = _velocity.normalized;
    }

    // El ataque modifica la vida; la transición a Dead la evalúa un estado de la FSM.
    public void ReceiveHits(int hits) => Health.ReceiveHits(hits);

    public void OnGathered(float delay)
    {
        if (!IsPendingGather) return;
        _gathered = true;
        RespawnDelay = Mathf.Max(0f, delay);
    }

    public bool TryRespawn()
    {
        if (Bounds.Instance == null || !Bounds.Instance.TryGetFreePoint(out Vector3 position)) return false;
        transform.position = position;
        Physics.SyncTransforms();
        Health.ResetHealth();
        _gathered = false;
        Food = null;
        Threat = null;
        _velocity = RandomDirection() * Steering.MaxSpeed;
        Feedback.SetVisible(true);
        return true;
    }

    private Vector3 RandomDirection()
    {
        Vector2 direction = Random.insideUnitCircle.normalized;
        return new Vector3(direction.x, 0f, direction.y);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.55f, 0f);
        Gizmos.DrawWireSphere(transform.position, hunterVisionRadius);
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, poiVisionRadius);
    }
}
