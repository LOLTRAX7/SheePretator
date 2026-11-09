using UnityEngine;

public class BoidSteering
{
    private readonly BoidAgent boid;
    public BoidSteering(BoidAgent boid) => this.boid = boid;

    private Vector3 Steering(Vector3 desired)
    {
        return Vector3.ClampMagnitude(desired - boid.Velocity, boid.Force * Time.deltaTime);
    }

    public Vector3 Separation()
    {
        Vector3 away = Vector3.zero;
        foreach (BoidAgent other in boid.Sensor.Boids)
        {
            if (other == null || !boid.Sensor.InRange(other, boid.SeparationRadius)) continue;
            Vector3 offset = boid.transform.position - other.transform.position;
            offset.y = 0f;
            away += offset / Mathf.Max(0.01f, offset.sqrMagnitude);
        }
        if (away.sqrMagnitude < 0.001f) return Vector3.zero;
        return Steering(away.normalized * boid.Speed);
    }

    public Vector3 Alignment()
    {
        Vector3 direction = Vector3.zero;
        int count = 0;
        foreach (BoidAgent other in boid.Sensor.Boids)
        {
            if (other == null || other.IsEliminated || !boid.Sensor.InRange(other, boid.NeighborRadius)) continue;
            direction += other.Velocity;
            count++;
        }
        if (count == 0) return Vector3.zero;
        return Steering(direction.normalized * boid.Speed);
    }

    public Vector3 Cohesion()
    {
        Vector3 center = Vector3.zero;
        int count = 0;
        foreach (BoidAgent other in boid.Sensor.Boids)
        {
            if (other == null || other.IsEliminated || !boid.Sensor.InRange(other, boid.NeighborRadius)) continue;
            center += other.transform.position;
            count++;
        }
        if (count == 0) return Vector3.zero;
        Vector3 direction = center / count - boid.transform.position;
        direction.y = 0f;
        return Steering(direction.normalized * boid.Speed);
    }

    public Vector3 KeepInsideBounds()
    {
        if (Bounds.Instance == null) return Vector3.zero;
        Vector3 future = boid.transform.position + boid.Velocity;
        Vector3 inside = Bounds.Instance.Clamp(future, boid.BodyRadius + 1f);
        if ((future - inside).sqrMagnitude < 0.001f) return Vector3.zero;
        Vector3 direction = inside - boid.transform.position;
        direction.y = 0f;
        return Steering(direction.normalized * boid.Speed);
    }
}
