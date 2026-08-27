using UnityEngine;

public class Bounds : MonoBehaviour
{
    // Instancia estática accesible desde cualquier parte (patrón Singleton)
    // get público, set privado: solo esta clase puede modificarla
    public static Bounds Instance { get; private set; }

    // SerializeField permite editar estos valores privados desde el Inspector de Unity
    [SerializeField] private float height = 30f;
    [SerializeField] private float width = 60f;
    [SerializeField] private bool drawGizmos;

    // Awake se ejecuta al cargar el objeto, antes que Start
    private void Awake()
    {
        // Si no existe otra instancia, esta pasa a ser la única (Singleton)
        if (Instance == null) Instance = this;
        // Si ya existe una, se destruye este objeto para evitar duplicados
        else Destroy(gameObject);
    }

    // Recibe una posición y la corrige si se sale de los límites del área
    public Vector3 OutOfBounds(Vector3 position)
    {
        Vector3 newPosition = position;

        // Si se pasa del límite derecho, reaparece del lado izquierdo (y viceversa)
        if (position.x > width / 2) newPosition.x = -width / 2;
        if (position.x < -width / 2) newPosition.x = width / 2;
        // Mismo efecto pero en el eje Z (adelante/atrás)
        if (position.z > height / 2) newPosition.z = -height / 2;
        if (position.z < -height / 2) newPosition.z = height / 2;

        return newPosition;
    }

    // Se dibuja solo en el editor de Unity, no afecta el juego en ejecución
    private void OnDrawGizmos()
    {
        // Corta la ejecución si el checkbox drawGizmos está desactivado
        if (!drawGizmos) return;

        Gizmos.color = Color.yellow;
        // Dibuja un cubo sin relleno para visualizar el área de límites
        Gizmos.DrawWireCube(Vector3.zero, new Vector3(width, 0, height));
    }
}