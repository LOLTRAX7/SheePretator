using UnityEngine;

public class InterestPointSpawner : MonoBehaviour
{
    [SerializeField] private InterestPoint interestPointPrefab;
    [SerializeField, Min(0.1f)] private float poiSpawnInterval = 4f;
    private float _timer;

    // Lo llama únicamente Patrol. No toma decisiones por los boids.
    public void TickPatrol()
    {
        _timer += Time.deltaTime;
        if (_timer < poiSpawnInterval || interestPointPrefab == null || InterestPoint.ActiveCount >= 5) return;
        Vector3 position = transform.position;
        position.y = 0f;
        foreach (Collider hit in Physics.OverlapSphere(position, 1.2f))
            if (hit.GetComponentInParent<BoidAgent>() != null || hit.GetComponentInParent<InterestPoint>() != null) return;
        Instantiate(interestPointPrefab, position, Quaternion.identity);
        Physics.SyncTransforms();
        _timer = 0f;
    }
}
