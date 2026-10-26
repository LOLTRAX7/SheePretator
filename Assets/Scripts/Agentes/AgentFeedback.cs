using UnityEngine;

public class AgentFeedback : MonoBehaviour
{
    [SerializeField] private Color flockingColor = Color.white;
    [SerializeField] private Color evadeColor = new Color(1f, 0.55f, 0f);
    [SerializeField] private Color seekColor = Color.cyan;
    [SerializeField] private Color consumeColor = Color.green;
    [SerializeField] private Color deadColor = Color.gray;
    [SerializeField] private Color patrolColor = Color.magenta;
    [SerializeField] private Color huntColor = Color.red;
    [SerializeField] private Color gatherColor = Color.yellow;
    [SerializeField] private Color restColor = Color.blue;
    private Renderer[] _renderers;
    private Collider[] _colliders;
    private MaterialPropertyBlock _properties;

    private void Awake()
    {
        _renderers = GetComponentsInChildren<Renderer>();
        _colliders = GetComponentsInChildren<Collider>();
        _properties = new MaterialPropertyBlock();
    }

    public void ShowState(string state)
    {
        Color color = state switch
        {
            "Evade" => evadeColor, "SeekObject" => seekColor,
            "Consume" => consumeColor, "Dead" => deadColor,
            "Patrol" => patrolColor, "Hunt" => huntColor,
            "Gather" => gatherColor, "Rest" => restColor,
            _ => flockingColor
        };
        // Cambia el cuerpo y conserva el material de los ojos.
        Renderer body = GetComponent<Renderer>();
        if (body == null) return;
        body.GetPropertyBlock(_properties);
        _properties.SetColor("_BaseColor", color);
        _properties.SetColor("_Color", color);
        body.SetPropertyBlock(_properties);
    }

    public void SetVisible(bool visible)
    {
        foreach (Renderer item in _renderers) item.enabled = visible;
        foreach (Collider item in _colliders) item.enabled = visible;
    }
}
