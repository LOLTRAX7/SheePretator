using UnityEngine;

public class InterestPoint : MonoBehaviour
{
    [SerializeField, Min(1f)] private float maxDurability = 30f;
    private float durability;
    private AgentFeedback feedback;
    public static int ActiveCount { get; private set; }
    public bool IsAvailable => isActiveAndEnabled && durability > 0f;
    public float BodyRadius { get; private set; } = 0.5f;

    private void Awake()
    {
        feedback = GetComponent<AgentFeedback>();
        if (feedback == null) feedback = gameObject.AddComponent<AgentFeedback>();
        if (TryGetComponent(out Collider shape))
            BodyRadius = Mathf.Max(shape.bounds.extents.x, shape.bounds.extents.z);
    }

    private void OnEnable()
    {
        ActiveCount++;
        durability = maxDurability;
        UpdateFeedback();
    }
    private void OnDisable() => ActiveCount--;

    public void TakeDamage(float amount)
    {
        if (!IsAvailable) return;
        durability = Mathf.Max(0f, durability - Mathf.Max(0f, amount));
        UpdateFeedback();
        if (durability <= 0f)
        {
            gameObject.SetActive(false);
            Destroy(gameObject);
        }
    }

    private void UpdateFeedback()
    {
        feedback.Set($"Objeto: {durability:0}/{maxDurability:0}",
            Color.Lerp(Color.red, Color.green, durability / maxDurability));
    }
}
