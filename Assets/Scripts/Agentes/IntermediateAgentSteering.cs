using UnityEngine;

public class IntermediateAgentSteering : Agent
{
    [Header("Stats")]
    [SerializeField] private float _maxSpeed = 3f;        // Velocidad máxima a la que puede moverse el agente
    [SerializeField] private float _maxSteering = 3f;     // Fuerza máxima con la que puede cambiar su velocidad/dirección
    [SerializeField] private float _slowingDistance = 3f; // Distancia desde donde empieza a reducir la velocidad en Arrive
    [SerializeField] private float _minDistance = 0.1f;   // Distancia mínima para considerar que el agente ya llegó

    [Header("References")]
    [SerializeField] private Agent _target; // Agente objetivo: puede ser perseguido, evitado, etc.

    // Modos de movimiento disponibles.
    // El modo elegido en el Inspector determina qué comportamiento utiliza el agente.
    public enum SteeringModes { Seek, Flee, Arrive, Pursuit, Evade }
    public SteeringModes currentSteering;

    private void Update()
    {
        // Calcula cuánto debe cambiar la velocidad actual y suma esa corrección.
        // Esto hace que el movimiento sea gradual en vez de cambiar de dirección instantáneamente.
        _velocity += SteeringVector();

        // Mueve al agente usando su velocidad.
        // Time.deltaTime hace que el movimiento sea independiente del framerate.
        transform.position += _velocity * Time.deltaTime;

        // Hace que el agente mire hacia la dirección en la que se está moviendo.
        if (_velocity != Vector3.zero)
        {
            transform.forward = _velocity;
        }
    }

    private Vector3 SteeringVector()
    {
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

            default:
                return Vector3.zero;
        }
    }
    private Vector3 DesiredVector(Vector3 target)
    {
        // Calcula la dirección desde el agente hacia el objetivo.
        // target - transform.position = posición del objetivo - posición del agente.
        Vector3 desired = (target - transform.position).normalized;

        // normalized deja el vector con longitud 1, conservando solamente la dirección.
        // Después se multiplica por la velocidad máxima para obtener la velocidad ideal.
        desired *= _maxSpeed;

        return desired;
    }

    private Vector3 CalculateSteering(Vector3 desired)
    {
        // "desired" es la velocidad que queremos tener.
        // "_velocity" es la velocidad que tenemos actualmente.
        // La diferencia indica cuánto debemos corregir nuestro movimiento.
        Vector3 steering = desired - _velocity;

        // Limita la longitud del steering para evitar cambios demasiado bruscos
        // de velocidad o dirección.
        steering = Vector3.ClampMagnitude(
            steering,
            _maxSteering * Time.deltaTime
        );

        return steering;
    }

    private Vector3 Seek(Vector3 target)
    {
        // Seek hace que el agente intente ir hacia el objetivo.
        var desired = DesiredVector(target);

        // Calcula cuánto debe corregir su velocidad actual para alcanzar la velocidad deseada.
        return CalculateSteering(desired);
    }

    private Vector3 Flee(Vector3 target)
    {
        // Flee es lo contrario de Seek: hace que el agente se aleje del objetivo.
        var desired = DesiredVector(target);

        // Se invierte la dirección para que la velocidad deseada apunte lejos del objetivo.
        return CalculateSteering(-desired);
    }

    private Vector3 Arrive(Vector3 target)
    {
        // Calcula el vector desde el agente hasta el objetivo.
        Vector3 direction = target - transform.position;

        // magnitude obtiene la distancia entre el agente y el objetivo.
        float distance = direction.magnitude;

        // Si ya está suficientemente cerca, deja de aplicar steering.
        // Esto evita que el agente tiemble u oscile alrededor del objetivo.
        if (distance < _minDistance)
            return Vector3.zero;

        // Calcula una velocidad proporcional a la distancia.
        // Lejos = más velocidad.
        // Cerca = menos velocidad.
        float targetSpeed = _maxSpeed * (distance / _slowingDistance);

        // Evita que la velocidad supere _maxSpeed.
        float desiredSpeed = Mathf.Min(targetSpeed, _maxSpeed);

        // Obtiene la dirección hacia el objetivo y la multiplica
        // por la velocidad que queremos tener.
        Vector3 desired = direction.normalized * desiredSpeed;

        // Calcula la corrección necesaria para alcanzar esa velocidad.
        Vector3 steering = CalculateSteering(desired);

        return steering;
    }

    private Vector3 CalculateFuture(Agent target)
    {
        // Calcula la dirección desde este agente hasta el objetivo.
        // Posición del objetivo - posición del agente = dirección hacia el objetivo.
        Vector3 direccion = target.transform.position - transform.position;

        // magnitude convierte el vector de dirección en un número
        // que representa la distancia entre los dos agentes.
        float distance = direccion.magnitude;

        // Estima cuánto tiempo hacia el futuro conviene predecir.
        // Tiene en cuenta la velocidad máxima de este agente
        // y la velocidad actual del objetivo.
        var prediction = distance /
                         (_maxSpeed + target.Velocity.magnitude);

        // Calcula dónde estará el objetivo en el futuro.
        // Posición futura = posición actual + velocidad × tiempo
        // Velocity indica hacia dónde y qué tan rápido se mueve el objetivo.
        // Al multiplicarla por prediction obtenemos cuánto avanzará durante ese tiempo.
        Vector3 futurePosition =
            target.transform.position +
            target.Velocity * prediction;

        // Devuelve la posición futura estimada del objetivo.
        return futurePosition;
    }

    private Vector3 Pursuit(Agent target)
    {
        // En vez de perseguir la posición actual del objetivo,
        // calcula primero dónde probablemente estará.
        var futurePosition = CalculateFuture(target);

        // Después utiliza Seek para ir hacia esa posición futura.
        return Seek(futurePosition);
    }

    private Vector3 Evade(Agent target)
    {
        // Primero calcula dónde probablemente estará el objetivo en el futuro.
        var futurePosition = CalculateFuture(target);

        // Después utiliza Flee para alejarse de esa posición futura.
        return Flee(futurePosition);
    }
}