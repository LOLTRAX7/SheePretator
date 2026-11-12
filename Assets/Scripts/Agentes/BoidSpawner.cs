using UnityEngine;

public class BoidSpawner : MonoBehaviour
{
    [SerializeField] private BoidAgent boidPrefab;
    [SerializeField, Min(6)] private int count = 8;
    private void Start()
    {
        if (boidPrefab == null || Bounds.Instance == null) return;
        
        Collider shape = boidPrefab.GetComponent<Collider>();
        float radius = shape != null ? Mathf.Max(shape.bounds.extents.x, shape.bounds.extents.z, 0.5f) : 0.5f;

        for (int i = 0; i < Mathf.Max(6, count); i++)
        {

            if (!Bounds.Instance.TryGetFreePoint(radius, null, transform.position.y, out Vector3 position)) break;

            BoidAgent boid = Instantiate(boidPrefab, position, Quaternion.identity);

            boid.name = $"Oveja {i + 1}";
            Physics.SyncTransforms();
        }
    }
}
