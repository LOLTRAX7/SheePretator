using System.Collections.Generic;
using UnityEngine;

public class ProximitySensor : MonoBehaviour
{
    [SerializeField, Min(0.02f)] private float interval = 0.15f;

    private float nextScan;

    public List<BoidAgent> Boids { get; } = new List<BoidAgent>();
    public List<HunterAgent> Hunters { get; } = new List<HunterAgent>();
    public List<InterestPoint> Points { get; } = new List<InterestPoint>();


    // Solo consulta objetos cercanos, respetando el intervalo.
    public void Scan(float radius)
    {
        if (Time.time < nextScan) return;
        nextScan = Time.time + interval;

        Boids.Clear();
        Hunters.Clear();
        Points.Clear();

        foreach (Collider hit in Physics.OverlapSphere(transform.position, radius))
        {

            Agent agent = hit.GetComponentInParent<Agent>();

            if (agent != null && agent.gameObject != gameObject)
            {

                if (agent is BoidAgent boid && !Boids.Contains(boid)) Boids.Add(boid);

                if (agent is HunterAgent hunter && !Hunters.Contains(hunter)) Hunters.Add(hunter);

            }

            InterestPoint point = hit.GetComponentInParent<InterestPoint>();
            
            if (point != null && !Points.Contains(point)) Points.Add(point);
        }
    }
    public bool InRange(Component target, float radius)
    {
            return target != null && target.gameObject.activeInHierarchy &&
            (target.transform.position - transform.position).sqrMagnitude <= radius * radius;
    }
}
