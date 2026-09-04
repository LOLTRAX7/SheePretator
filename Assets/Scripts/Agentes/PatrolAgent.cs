using UnityEngine;

public class PatrolAgent : Agent
{
    [SerializeField] private Transform pointA;
    [SerializeField] private Transform pointB;
    [SerializeField] private float maxSpeed = 2f;

    private Transform target;

    private void Start()
    {
        // Comienza la patrulla yendo hacia el punto B.
        target = pointB;
    }

    private void Update()
    {
        // Calcula la dirección hacia el punto actual y la convierte en velocidad.
        Vector3 direction = (target.position - transform.position).normalized;
        _velocity = direction * maxSpeed;

        // Mueve al agente usando la velocidad guardada en Agent.
        transform.position += Velocity * Time.deltaTime;

        // Hace que el agente mire hacia donde se está moviendo.
        if (Velocity != Vector3.zero)
            transform.forward = Velocity.normalized;

        // Al llegar al objetivo cambia al otro punto para continuar la patrulla.
        if (Vector3.Distance(transform.position, target.position) < 0.1f)
        {
            target = target == pointA ? pointB : pointA;
        }
    }
}
