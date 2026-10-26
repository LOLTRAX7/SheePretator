using UnityEngine;

public class BoidSpawner : MonoBehaviour
{
    [SerializeField] private BoidAgent boidPrefab;
    [SerializeField, Min(6)] private int count = 8;
    private void Start()
    {
        if (boidPrefab == null || Bounds.Instance == null)
        {
            Debug.LogError("Asignar prefab del boid y un Bounds en la escena.", this);
            return;
        }
        for (int i = 0; i < Mathf.Max(6, count); i++)
        {
            if (!Bounds.Instance.TryGetFreePoint(out Vector3 position))
            {
                Debug.LogError("No hay espacio para generar todos los boids.", this);
                break;
            }
            Instantiate(boidPrefab, position, Quaternion.identity);
            Physics.SyncTransforms();
        }
    }
}
