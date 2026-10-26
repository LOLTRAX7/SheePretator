using UnityEngine;

public class Bounds : MonoBehaviour
{
    public static Bounds Instance { get; private set; }
    [SerializeField, Min(3f)] private float height = 30f;
    [SerializeField, Min(3f)] private float width = 60f;
    [SerializeField] private bool drawGizmos = true;
    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }
    private void OnDestroy() { if (Instance == this) Instance = null; }
    public bool Contains(Vector3 point) => Mathf.Abs(point.x - transform.position.x) <= width / 2f
        && Mathf.Abs(point.z - transform.position.z) <= height / 2f;
    public Vector3 ClampInside(Vector3 point, float margin = 0f)
    {
        point.x = Mathf.Clamp(point.x, transform.position.x - width / 2f + margin, transform.position.x + width / 2f - margin);
        point.z = Mathf.Clamp(point.z, transform.position.z - height / 2f + margin, transform.position.z + height / 2f - margin);
        return point;
    }
    public Vector3 GetRandomPointInside() => new Vector3(
        transform.position.x + Random.Range(-width / 2f + 1f, width / 2f - 1f), 0f,
        transform.position.z + Random.Range(-height / 2f + 1f, height / 2f - 1f));

    public bool TryGetFreePoint(out Vector3 point)
    {
        for (int attempt = 0; attempt < 100; attempt++)
        {
            point = GetRandomPointInside();
            bool occupied = false;
            foreach (Collider hit in Physics.OverlapSphere(point, 1.2f))
                if (hit.GetComponentInParent<BoidAgent>() != null || hit.GetComponentInParent<InterestPoint>() != null)
                { occupied = true; break; }
            if (!occupied) return true;
        }
        point = Vector3.zero;
        return false;
    }
    private void OnDrawGizmos()
    {
        if (!drawGizmos) return;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(transform.position, new Vector3(width, 0, height));
    }
}
