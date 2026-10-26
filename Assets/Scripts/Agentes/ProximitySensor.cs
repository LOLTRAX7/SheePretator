using System.Collections.Generic;
using UnityEngine;

public class ProximitySensor : MonoBehaviour
{
    [SerializeField] private LayerMask perceptionLayers = ~0;

    public List<T> Detect<T>(float radius) where T : Component
    {
        var detected = new List<T>();
        // Consulta espacial local, sin registros globales de agentes.
        foreach (Collider hit in Physics.OverlapSphere(transform.position, radius,
                     perceptionLayers, QueryTriggerInteraction.Ignore))
        {
            T item = hit.GetComponentInParent<T>();
            if (item == null || item.gameObject == gameObject || detected.Contains(item)) continue;
            if (Agent.PlanarDistance(transform.position, item.transform.position) <= radius)
                detected.Add(item);
        }
        return detected;
    }

    public T Nearest<T>(float radius) where T : Component
    {
        T nearest = null;
        foreach (T item in Detect<T>(radius))
        {
            float distance = Agent.PlanarDistance(transform.position, item.transform.position);
            if (distance > radius) continue;
            radius = distance;
            nearest = item;
        }
        return nearest;
    }
}
