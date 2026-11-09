using UnityEngine;

public class Agent : MonoBehaviour
{
    protected Vector3 _velocity;
    protected ProximitySensor sensor;
    protected AgentFeedback feedback;
    public Vector3 Velocity => _velocity;
    public float BodyRadius { get; private set; } = 0.5f;

    protected void InitializeAgent()
    {
        if (TryGetComponent(out Rigidbody body))
        {
            body.isKinematic = true;
            body.useGravity = false;
        }
        if (TryGetComponent(out Collider shape))
            BodyRadius = Mathf.Max(shape.bounds.extents.x, shape.bounds.extents.z);
        sensor = GetComponent<ProximitySensor>();
        if (sensor == null) sensor = gameObject.AddComponent<ProximitySensor>();
        feedback = GetComponent<AgentFeedback>();
        if (feedback == null) feedback = gameObject.AddComponent<AgentFeedback>();
    }

    protected Vector3 Steering(Vector3 desired, float force)
    {
        return Vector3.ClampMagnitude(desired - _velocity, force * Time.deltaTime);
    }

    protected Vector3 DesiredVelocity(Vector3 target, float speed)
    {
        Vector3 direction = target - transform.position;
        direction.y = 0f;
        return direction.normalized * speed;
    }

    public Vector3 PredictPosition(Agent target, float speed)
    {
        float distance = Vector3.Distance(transform.position, target.transform.position);
        float prediction = distance / Mathf.Max(0.01f, speed + target.Velocity.magnitude);
        return target.transform.position + target.Velocity * prediction;
    }

    protected void Move(Vector3 velocity, bool avoidOverlap)
    {
        Vector3 position = transform.position;
        Vector3 nextPosition = position + velocity * Time.deltaTime;
        if (Bounds.Instance != null)
            nextPosition = Bounds.Instance.Clamp(nextPosition, BodyRadius);

        // Solo corrige contactos cercanos, sin consultar agentes de toda la escena.
        if (avoidOverlap)
        {
            for (int pass = 0; pass < 2; pass++)
            {
                foreach (Collider hit in Physics.OverlapSphere(nextPosition, BodyRadius * 2f + 1f))
                {
                    Agent other = hit.GetComponentInParent<Agent>();
                    InterestPoint point = hit.GetComponentInParent<InterestPoint>();
                    if (other == this || (other == null && point == null)) continue;
                    Vector3 center = other != null ? other.transform.position : point.transform.position;
                    float radius = other != null ? other.BodyRadius : point.BodyRadius;
                    Vector3 offset = nextPosition - center;
                    offset.y = 0f;
                    float distance = offset.magnitude;
                    float minimum = BodyRadius + radius + 0.02f;
                    if (distance >= minimum) continue;
                    if (distance < 0.001f)
                    {
                        offset = position - center;
                        offset.y = 0f;
                        if (offset.sqrMagnitude < 0.0001f) offset = transform.right;
                    }
                    nextPosition = center + offset.normalized * minimum;
                    nextPosition.y = position.y;
                }
                if (Bounds.Instance != null)
                    nextPosition = Bounds.Instance.Clamp(nextPosition, BodyRadius);
            }
        }
        transform.position = nextPosition;
        _velocity = (nextPosition - position) / Mathf.Max(Time.deltaTime, 0.0001f);
        if (_velocity.sqrMagnitude > 0.001f) transform.forward = _velocity.normalized;
    }

    public void StopMoving() => _velocity = Vector3.zero;
}
