using UnityEngine;

// Define los límites del área de movimiento y permite que los agentes reaparezcan
// por el lado opuesto cuando salen de la zona.
public class Bounds : MonoBehaviour
{
    public static Bounds Instance { get; private set; }

    [SerializeField] private float height = 30f;
    [SerializeField] private float width = 60f;
    [SerializeField] private bool drawGizmos;

    private void Awake()
    {
        // Guarda una única instancia para que los agentes puedan acceder a Bounds
        // sin tener que buscar el objeto cada vez que se mueven.
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public Vector3 OutOfBounds(Vector3 position)
    {
        // Comprueba cada límite y, si el agente lo cruza, lo pasa al lado contrario.
        // Así el área funciona como un espacio cerrado que conecta sus bordes.
        Vector3 newPosition = position;

        if (position.x > width / 2) newPosition.x = -width / 2;
        if (position.x < -width / 2) newPosition.x = width / 2;
        if (position.z > height / 2) newPosition.z = -height / 2;
        if (position.z < -height / 2) newPosition.z = height / 2;

        return newPosition;
    }

    private void OnDrawGizmos()
    {
        // Dibuja el área en la escena de Unity cuando la opción está activada.
        if (!drawGizmos) return;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(Vector3.zero, new Vector3(width, 0, height));
    }
}
