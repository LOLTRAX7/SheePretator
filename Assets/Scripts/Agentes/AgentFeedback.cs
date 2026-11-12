using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class AgentFeedback : MonoBehaviour
{
    [SerializeField] private Renderer feedbackRenderer;
    [SerializeField] private bool showText = true;
    private MaterialPropertyBlock properties;
    private string message = "";
    private Color originalColor = Color.white;

    private void Awake()
    {
        if (feedbackRenderer == null) feedbackRenderer = GetComponentInChildren<Renderer>();
        properties = new MaterialPropertyBlock();
        if (feedbackRenderer != null && feedbackRenderer.sharedMaterial != null)
            originalColor = feedbackRenderer.sharedMaterial.color;
    }

    public void Set(string text, Color color)
    {
        message = text;
        
        if (feedbackRenderer == null) return;
        if (properties == null) properties = new MaterialPropertyBlock();

        feedbackRenderer.GetPropertyBlock(properties);
        properties.SetColor("_BaseColor", color);
        properties.SetColor("_Color", color);
        feedbackRenderer.SetPropertyBlock(properties);
    }

    public void ResetColor(string text) => Set(text, originalColor);

    public void SetVisible(bool visible)
    {
        foreach (Renderer child in GetComponentsInChildren<Renderer>()) child.enabled = visible;
        foreach (Collider child in GetComponentsInChildren<Collider>()) child.enabled = visible;

        showText = visible;
    }

    private void OnGUI()
    {
        if (!showText || Camera.main == null || message.Length == 0) return;

        Vector3 screen = Camera.main.WorldToScreenPoint(transform.position + Vector3.up * 1.8f);

        if (screen.z <= 0f) return;

        Vector2 size = GUI.skin.box.CalcSize(new GUIContent(message)) + new Vector2(12f, 8f);

        float x = Mathf.Clamp(screen.x - size.x * 0.5f, 0f, Mathf.Max(0f, Screen.width - size.x));
        float y = Mathf.Clamp(Screen.height - screen.y - size.y, 0f, Mathf.Max(0f, Screen.height - size.y));

        GUI.Box(new Rect(x, y, size.x, size.y), message);
    }

    #if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (showText && message.Length > 0)
            Handles.Label(transform.position + Vector3.up * 1.8f, message);
    }
    #endif
}
