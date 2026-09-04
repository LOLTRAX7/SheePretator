using System.Collections.Generic;
using UnityEngine;

public class IntermediateAgentSteering : Agent
{
    [Header("Stats")]
    [SerializeField] private float _maxSpeed = 3f;        // Límite de velocidad del agente.
    [SerializeField] private float _maxSteering = 3f;     // Límite de cuánto puede corregir su movimiento por frame.
    [SerializeField] private float _slowingDistance = 3f; // Distancia a partir de la cual Arrive empieza a frenar.
    [SerializeField] private float _minDistance = 0.1f;   // Distancia mínima para considerar que llegó.

    [Header("Flocking")]
    [SerializeField] private float _separationRadius = 5f; // Hasta qué distancia busca vecinos para separarse.
    [SerializeField] private float _cohesionRadius = 5f;  // Hasta qué distancia busca vecinos para mantenerse unido.
    [SerializeField] private float _alignmentRadius = 5f; // Hasta qué distancia busca vecinos para alinear su movimiento.

    [SerializeField, Range(0f, 3f)] private float separationWeight = 3f; // Cuánto influye Separation en Flocking.
    [SerializeField, Range(0f, 3f)] private float cohesionWeight = 3f;   // Cuánto influye Cohesion en Flocking.
    [SerializeField, Range(0f, 3f)] private float alignmentWeight = 3f;  // Cuánto influye Alignment en Flocking.

    // Todos los agentes comparten esta lista para poder encontrarse entre ellos
    // cuando calculan los comportamientos de Flocking.
    private static List<Agent> allAgents = new List<Agent>();

    [Header("References")]
    [SerializeField] private Agent _target; // Objetivo usado por Seek, Flee, Arrive, Pursuit y Evade.

    // Define los comportamientos disponibles y permite elegir uno desde el Inspector.
    // El valor elegido se usa después en SteeringVector() para decidir qué lógica ejecutar.
    public enum SteeringModes { Seek, Flee, Arrive, Pursuit, Evade, Flocking }
    public SteeringModes currentSteering;

    private void Awake()
    {
        // Cada agente se registra en la lista compartida para que Flocking pueda
        // consultar las posiciones y velocidades de los demás agentes.
        allAgents.Add(this);

        // Da una velocidad inicial aleatoria para que los agentes no comiencen todos quietos.
        // El movimiento se mantiene en el plano XZ porque Y vale 0.
        Vector3 randomDirection = new Vector3(Random.Range(-1, 1), 0f, Random.Range(-1, 1));
        _velocity += randomDirection.normalized * _maxSpeed;
    }

    private void Update()
    {
        // Primero calcula cuánto debe cambiar su velocidad y aplica esa corrección.
        // La velocidad no se reemplaza de golpe: se va modificando gradualmente.
        _velocity += SteeringVector();

        // Mueve al agente según su velocidad actual. DeltaTime hace que el movimiento
        // dependa del tiempo real transcurrido y no de cuántos FPS haya.
        transform.position += _velocity * Time.deltaTime;

        // Hace que el agente mire hacia donde se está moviendo.
        // Se evita hacerlo cuando la velocidad es cero porque no existe una dirección.
        if (_velocity != Vector3.zero)
        {
            transform.forward = _velocity;
        }

        // Si sale del área, Bounds lo coloca en el lugar correspondiente del otro lado.
        transform.position = Bounds.Instance.OutOfBounds(transform.position);
    }

    private Vector3 SteeringVector()
    {
        // Según el modo elegido, llama al comportamiento correspondiente.
        // Todos esos métodos terminan devolviendo una corrección de movimiento.
        switch (currentSteering)
        {
            case SteeringModes.Seek:
                return Seek(_target.transform.position);

            case SteeringModes.Flee:
                return Flee(_target.transform.position);

            case SteeringModes.Arrive:
                return Arrive(_target.transform.position);

            case SteeringModes.Pursuit:
                return Pursuit(_target);

            case SteeringModes.Evade:
                return Evade(_target);

            case SteeringModes.Flocking:
                return Flocking();

            default:
                return Vector3.zero;
        }
    }

    private Vector3 DesiredVector(Vector3 target)
    {
        // Primero obtiene la dirección desde el agente hasta el objetivo.
        // Normalizarla deja solo la dirección y luego se le aplica la velocidad máxima.
        Vector3 desired = (target - transform.position).normalized;
        desired *= _maxSpeed;

        return desired;
    }

    private Vector3 CalculateSteering(Vector3 desired)
    {
        // Compara la velocidad que queremos con la velocidad actual.
        // El resultado es la corrección necesaria para pasar de una a la otra.
        Vector3 steering = desired - _velocity;

        // Limita cuánto puede cambiar la velocidad en este frame para que el movimiento
        // no sea instantáneo ni demasiado brusco.
        steering = Vector3.ClampMagnitude(
            steering,
            _maxSteering * Time.deltaTime
        );

        return steering;
    }

    private Vector3 Seek(Vector3 target)
    {
        // Seek hace que el agente busque directamente un punto.
        // Calcula la velocidad ideal hacia el objetivo y luego la convierte en steering.
        var desired = DesiredVector(target);
        return CalculateSteering(desired);
    }

    private Vector3 Flee(Vector3 target)
    {
        // Flee usa la misma lógica de Seek, pero invierte la dirección.
        // Por eso el agente se aleja del objetivo en lugar de acercarse.
        var desired = DesiredVector(target);
        return CalculateSteering(-desired);
    }

    private Vector3 Arrive(Vector3 target)
    {
        // Arrive busca llegar al objetivo pero reduciendo la velocidad al acercarse.
        // Esto permite detenerse cerca del punto sin pasarlo constantemente.
        Vector3 direction = target - transform.position;
        float distance = direction.magnitude;

        // Cuando ya está suficientemente cerca, deja de corregir el movimiento.
        if (distance < _minDistance)
            return Vector3.zero;

        // Cuanto más cerca está del objetivo, menor es la velocidad deseada.
        float targetSpeed = _maxSpeed * (distance / _slowingDistance);
        float desiredSpeed = Mathf.Min(targetSpeed, _maxSpeed);

        // Conserva la dirección hacia el objetivo y le aplica la velocidad calculada.
        Vector3 desired = direction.normalized * desiredSpeed;
        Vector3 steering = CalculateSteering(desired);

        return steering;
    }

    private Vector3 CalculateFuture(Agent target)
    {
        // Se usa en Pursuit y Evade para no reaccionar únicamente a la posición actual.
        // Primero estima cuánto tardaría el agente en llegar hasta la zona del objetivo.
        Vector3 direccion = target.transform.position - transform.position;
        float distance = direccion.magnitude;

        var prediction = distance /
                         (_maxSpeed + target.Velocity.magnitude);

        // Con ese tiempo estima dónde estará el objetivo y devuelve esa posición futura.
        Vector3 futurePosition =
            target.transform.position +
            target.Velocity * prediction;

        return futurePosition;
    }

    private Vector3 Pursuit(Agent target)
    {
        // Pursuit no persigue la posición actual, sino la posición que se estima que
        // tendrá el objetivo. Después usa Seek para dirigirse hacia ese punto.
        var futurePosition = CalculateFuture(target);
        return Seek(futurePosition);
    }

    private Vector3 Evade(Agent target)
    {
        // Evade es la versión predictiva de Flee: calcula hacia dónde va el objetivo
        // y se aleja de ese punto en lugar de alejarse solo de su posición actual.
        var futurePosition = CalculateFuture(target);
        return Flee(futurePosition);
    }

    private Vector3 Flocking()
    {
        // Flocking combina tres reglas: Separation evita choques, Alignment coordina
        // la dirección y Cohesion mantiene unido al grupo. Los pesos deciden cuánto influye cada una.
        return CalculateSeparation(allAgents, _separationRadius) * separationWeight
             + CalculateAlignment(allAgents, _alignmentRadius) * alignmentWeight
             + CalculateCohesion(allAgents, _cohesionRadius) * cohesionWeight;
    }

    private Vector3 CalculateSeparation(List<Agent> list, float radius)
    {
        // Separation hace que el agente se aleje de los vecinos que están cerca.
        // Se suman las posiciones cercanas para encontrar hacia qué lado están agrupados.
        Vector3 dessired = default;
        int count = 0;

        foreach (var item in list)
        {
            if (item == this) continue;

            if (Vector3.Distance(item.transform.position, transform.position) <= radius)
            {
                dessired += (item.transform.position - transform.position);
                count++;
            }
        }

        // Si no hay vecinos cerca, no hay nada de lo que separarse.
        if (count == 0)
        {
            return Vector3.zero;
        }

        // Promedia las posiciones y luego invierte la dirección para obtener el movimiento de alejamiento.
        dessired /= count;
        return CalculateSteering(-dessired.normalized * _maxSpeed);
    }

    private Vector3 CalculateAlignment(List<Agent> list, float radius)
    {
        // Alignment hace que el agente tienda a moverse en la misma dirección que sus vecinos.
        // Por eso aquí se promedian velocidades, no posiciones.
        Vector3 dessired = default;
        int count = 0;

        foreach (var item in list)
        {
            if (item == this) continue;

            if (Vector3.Distance(item.transform.position, transform.position) <= radius)
            {
                dessired += item.Velocity;
                count++;
            }
        }

        // Sin vecinos no existe una dirección grupal hacia la cual alinearse.
        if (count == 0) return Vector3.zero;

        // Usa la velocidad promedio del grupo como dirección deseada del agente.
        dessired /= count;
        return CalculateSteering(dessired.normalized * _maxSpeed);
    }

    private Vector3 CalculateCohesion(List<Agent> list, float radius)
    {
        // Cohesion evita que el grupo se disperse. Busca el centro de los vecinos
        // y hace que el agente tienda a moverse hacia ese punto.
        Vector3 desired = default;
        int count = 0;

        foreach (var item in list)
        {
            if (item == this) continue;

            if (Vector3.Distance(item.transform.position, transform.position) <= radius)
            {
                desired += item.transform.position;
                count++;
            }
        }

        // Sin vecinos no existe un centro de grupo al cual acercarse.
        if (count == 0) return Vector3.zero;

        // El promedio de las posiciones da el centro aproximado de los vecinos.
        desired /= count;
        return Seek(desired);
    }
}
