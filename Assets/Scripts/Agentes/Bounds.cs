using UnityEngine;

public class Bounds : MonoBehaviour
{
    public static Bounds Instance { get; private set; }
    [SerializeField, Min(2f)] private float height = 30f;
    [SerializeField, Min(2f)] private float width = 60f;
    [SerializeField] private bool drawGizmos = true;

    private void Awake() => Instance = this;
    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public bool Contains(Vector3 position)
    {
        Vector3 local = position - transform.position;
        return Mathf.Abs(local.x) <= width * 0.5f && Mathf.Abs(local.z) <= height * 0.5f;
    }

    public Vector3 Clamp(Vector3 position, float margin)
    {
        Vector3 center = transform.position;
        position.x = Mathf.Clamp(position.x, center.x - width * 0.5f + margin, center.x + width * 0.5f - margin);
        position.z = Mathf.Clamp(position.z, center.z - height * 0.5f + margin, center.z + height * 0.5f - margin);
        return position;
    }

    public Vector3 OutOfBounds(Vector3 position) => Clamp(position, 0.5f);

    public Vector3 GetRandomPointInside()
    {
        return transform.position + new Vector3(Random.Range(-width * 0.5f, width * 0.5f), 0f,
            Random.Range(-height * 0.5f, height * 0.5f));
    }

    public bool TryGetFreePoint(float radius, Agent ignored, float y, out Vector3 position)
    {
        for (int attempt = 0; attempt < 40; attempt++)
        {
            position = Clamp(GetRandomPointInside(), radius + 0.05f);
            position.y = y;
            bool blocked = false;
            foreach (Collider hit in Physics.OverlapSphere(position, radius + 0.6f))
            {
                Agent agent = hit.GetComponentInParent<Agent>();
                InterestPoint point = hit.GetComponentInParent<InterestPoint>();
                if ((agent != null && agent != ignored) || point != null) blocked = true;
            }
            if (!blocked) return true;
        }
        position = default;
        return false;
    }

    private void OnDrawGizmos()
    {
        if (!drawGizmos) return;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(transform.position, new Vector3(width, 0f, height));
    }
}
