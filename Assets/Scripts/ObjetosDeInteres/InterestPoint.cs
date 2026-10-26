using UnityEngine;

public class InterestPoint : MonoBehaviour
{
    // Solo un contador para el límite de generación; no permite localizar objetos.
    public static int ActiveCount { get; private set; }
    [SerializeField, Min(1f)] private float maxDurability = 30f;
    [SerializeField] private Renderer feedbackRenderer;
    private float _currentDurability;
    private bool _destroyed;
    private MaterialPropertyBlock _properties;
    public float Radius => Mathf.Max(GetComponent<Collider>().bounds.extents.x, GetComponent<Collider>().bounds.extents.z);
    private void OnEnable()
    {
        ActiveCount++;
        _currentDurability = maxDurability;
        _destroyed = false;
        _properties = new MaterialPropertyBlock();
        if (feedbackRenderer == null) feedbackRenderer = GetComponent<Renderer>();
        UpdateFeedback();
    }
    private void OnDisable() => ActiveCount--;
    public void TakeDamage(float amount)
    {
        if (_destroyed) return;
        _currentDurability -= Mathf.Max(0f, amount);
        UpdateFeedback();
        if (_currentDurability > 0f) return;
        _destroyed = true;
        gameObject.SetActive(false);
        Destroy(gameObject);
    }
    private void UpdateFeedback()
    {
        if (feedbackRenderer == null) return;
        Color color = Color.Lerp(Color.red, Color.green, Mathf.Clamp01(_currentDurability / maxDurability));
        _properties.SetColor("_BaseColor", color);
        _properties.SetColor("_Color", color);
        feedbackRenderer.SetPropertyBlock(_properties);
    }
}
