using UnityEngine;

public class BoidSteering : MonoBehaviour
{
    [Header("Movimiento")]
    [SerializeField, Min(0.1f)] private float maxSpeed = 4f;
    [SerializeField, Min(0.1f)] private float maxForce = 6f;
    [Header("Flocking")]
    [SerializeField, Min(0.1f)] private float separationRadius = 2f;
    [SerializeField, Min(0.2f)] private float neighborRadius = 5f;
    [SerializeField, Range(0f, 5f)] private float separationWeight = 1.6f;
    [SerializeField, Range(0f, 5f)] private float alignmentWeight = 1f;
    [SerializeField, Range(0f, 5f)] private float cohesionWeight = 1f;
    [Header("Evade y Arrive")]
    [SerializeField, Range(0f, 5f)] private float evadeWeight = 2.2f;
    [SerializeField, Min(0.1f)] private float arriveSlowingDistance = 3f;
    [SerializeField, Min(0.1f)] private float arriveMinDistance = 1.2f;
    [SerializeField, Range(0f, 5f)] private float arriveWeight = 1.4f;
    private BoidAgent _boid;
    public float MaxSpeed => maxSpeed;
    public float MaxForce => maxForce;
    public float BodyRadius => GetComponent<CapsuleCollider>().radius * Mathf.Max(transform.lossyScale.x, transform.lossyScale.z);
    private void Awake() => _boid = GetComponent<BoidAgent>();

    private Vector3 Steering(Vector3 desired) => desired - _boid.Velocity;
    private Vector3 Direction(Vector3 target)
    {
        Vector3 direction = target - transform.position;
        direction.y = 0f;
        return direction;
    }

    public Vector3 Flocking()
    {
        Vector3 alignment = Vector3.zero;
        Vector3 center = Vector3.zero;
        int count = 0;
        foreach (BoidAgent neighbor in _boid.Sensor.Detect<BoidAgent>(neighborRadius))
        {
            if (neighbor.IsEliminated) continue;
            alignment += neighbor.Velocity;
            center += neighbor.transform.position;
            count++;
        }
        Vector3 force = Separation();
        if (count == 0) return force;
        force += Steering((alignment / count).normalized * maxSpeed) * alignmentWeight;
        force += Steering(Direction(center / count).normalized * maxSpeed) * cohesionWeight;
        return force;
    }

    public Vector3 Separation()
    {
        Vector3 away = Vector3.zero;
        foreach (BoidAgent neighbor in _boid.Sensor.Detect<BoidAgent>(separationRadius))
        {
            Vector3 offset = -Direction(neighbor.transform.position);
            if (offset.sqrMagnitude > 0.0001f) away += offset.normalized / offset.magnitude;
        }
        return away.sqrMagnitude < 0.0001f ? Vector3.zero
            : Steering(away.normalized * maxSpeed) * separationWeight;
    }

    public Vector3 Evade(Agent hunter)
    {
        float prediction = Direction(hunter.transform.position).magnitude / (maxSpeed + hunter.Velocity.magnitude);
        Vector3 future = hunter.transform.position + hunter.Velocity * prediction;
        return Steering(-Direction(future).normalized * maxSpeed) * evadeWeight;
    }

    private float FoodStopDistance(InterestPoint food) => Mathf.Max(arriveMinDistance, BodyRadius + food.Radius + 0.1f);
    public bool AtFood(InterestPoint food) => Agent.PlanarDistance(transform.position, food.transform.position) <= FoodStopDistance(food) + 0.15f;

    public Vector3 Arrive(InterestPoint food)
    {
        Vector3 direction = Direction(food.transform.position);
        float remaining = Mathf.Max(0f, direction.magnitude - FoodStopDistance(food));
        float speed = maxSpeed * Mathf.Clamp01(remaining / arriveSlowingDistance);
        return Steering(direction.normalized * speed) * arriveWeight;
    }

    // Separation es una fuerza, no una garantía física. Estos pasos cortos impiden atravesar cuerpos.
    public void MoveWithoutOverlap(Vector3 displacement)
    {
        int steps = Mathf.Max(1, Mathf.CeilToInt(displacement.magnitude / 0.2f));
        for (int step = 0; step < steps; step++)
        {
            Vector3 previous = transform.position;
            Vector3 next = previous + displacement / steps;
            for (int iteration = 0; iteration < 6; iteration++)
            {
                if (Bounds.Instance != null) next = Bounds.Instance.ClampInside(next, BodyRadius);
                bool blocked = false;
                foreach (Collider hit in Physics.OverlapSphere(next, BodyRadius + 1f))
                {
                    BoidAgent other = hit.GetComponentInParent<BoidAgent>();
                    InterestPoint food = hit.GetComponentInParent<InterestPoint>();
                    if (other == _boid || (other == null && food == null)) continue;
                    Vector3 center = other != null ? other.transform.position : food.transform.position;
                    float radius = other != null ? other.Steering.BodyRadius : food.Radius;
                    Vector3 offset = next - center;
                    offset.y = 0f;
                    float minimum = BodyRadius + radius + 0.02f;
                    if (offset.magnitude >= minimum) continue;
                    if (offset.sqrMagnitude < 0.0001f) offset = transform.right;
                    next = center + offset.normalized * minimum;
                    next.y = previous.y;
                    blocked = true;
                }
                if (!blocked) break;
                if (iteration == 5) next = previous;
            }
            transform.position = next;
            Physics.SyncTransforms();
        }
    }

    private void OnValidate()
    {
        separationRadius = Mathf.Max(0.1f, separationRadius);
        neighborRadius = Mathf.Max(separationRadius + 0.1f, neighborRadius);
    }
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, separationRadius);
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, neighborRadius);
    }
}
