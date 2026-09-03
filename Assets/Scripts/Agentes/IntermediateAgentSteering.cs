using System.Collections.Generic;
using UnityEngine;

public class IntermediateAgentSteering : Agent
{
    [Header("Stats")]
    [SerializeField] private float _maxSpeed = 3f;        // Velocidad máxima a la que puede moverse el agente
    [SerializeField] private float _maxSteering = 3f;     // Fuerza máxima con la que puede cambiar su velocidad/dirección
    [SerializeField] private float _slowingDistance = 3f; // Distancia desde donde empieza a frenar en Arrive
    [SerializeField] private float _minDistance = 0.1f;   // Distancia mínima para considerar que ya llegó

    [Header("Flocking")]
    [SerializeField] private float _separationRadius = 5f; // Radio de vecinos para Separation
    [SerializeField] private float _cohesionRadius = 5f;    // Radio de vecinos para Cohesion
    [SerializeField] private float _alignmentRadius = 5f;   // Radio de vecinos para Alignment

    [SerializeField, Range(0f, 3f)] private float separationWeight = 3f; // Peso de Separation dentro de Flocking
    [SerializeField, Range(0f, 3f)] private float cohesionWeight = 3f;   // Peso de Cohesion dentro de Flocking
    [SerializeField, Range(0f, 3f)] private float alignmentWeight = 3f;  // Peso de Alignment dentro de Flocking

    private static List<Agent> allAgents = new List<Agent>(); // Lista compartida por todos los agentes de la escena

    [Header("References")]
    [SerializeField] private Agent _target; // Agente objetivo: puede ser perseguido, evitado, etc.

    // El enum define los modos posibles; el que se elija en el Inspector
    // determina qué comportamiento se ejecuta en SteeringVector().
    // Sirve para poder cambiar el "tipo de IA" de un agente sin tocar código,
    // solo eligiendo la opción en el Inspector de Unity.
    public enum SteeringModes { Seek, Flee, Arrive, Pursuit, Evade, Flocking }
    public SteeringModes currentSteering;


    private void Awake()
    {
        // Agrega este agente a la lista estática, compartida por todos.
        allAgents.Add(this);

        // Random.Range(-1, 1) con enteros devuelve -1 o 0 (el límite superior es exclusivo),
        // se arma un vector en el plano XZ (Y en 0 = no se mueve verticalmente),
        // se normaliza (longitud 1, conserva solo la dirección) y se escala a _maxSpeed.
        // Esto evita que todos los agentes de Flocking arranquen quietos o superpuestos.
        Vector3 randomDirection = new Vector3(Random.Range(-1, 1), 0f, Random.Range(-1, 1));
        _velocity += randomDirection.normalized * _maxSpeed;
    }

    private void Update()
    {
        // SteeringVector() devuelve cuánto hay que corregir la velocidad actual;
        // sumarlo (en vez de reemplazarlo) es lo que hace que el giro sea gradual
        // y no un cambio instantáneo de dirección.
        _velocity += SteeringVector();

        // Se mueve según la velocidad. Multiplicar por Time.deltaTime (segundos
        // desde el último frame) hace que la velocidad esté en unidades/segundo
        // y no dependa de los FPS del juego.
        transform.position += _velocity * Time.deltaTime;

        // transform.forward = _velocity rota el agente para que "mire" hacia
        // donde se está moviendo. Se chequea != Vector3.zero para no romper
        // la rotación cuando la velocidad es nula.
        if (_velocity != Vector3.zero)
        {
            transform.forward = _velocity;
        }

        // Corrige la posición si el agente se salió del área permitida.
        transform.position = Bounds.Instance.OutOfBounds(transform.position);
    }

    private Vector3 SteeringVector()
    {
        // switch sobre el enum: según currentSteering, delega en el método
        // de comportamiento correspondiente y devuelve el vector de corrección.
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
        // target - transform.position = vector que va desde el agente hacia el target.
        // .normalized deja ese vector con longitud 1 (solo dirección, sin magnitud).
        Vector3 desired = (target - transform.position).normalized;

        // Se escala esa dirección por _maxSpeed: esto da la velocidad "ideal"
        // que tendría el agente si pudiera ir directo al target a máxima velocidad.
        desired *= _maxSpeed;

        return desired;
    }

    private Vector3 CalculateSteering(Vector3 desired)
    {
        // steering = velocidad deseada - velocidad actual = cuánto hay que
        // corregir para pasar de una a otra.
        Vector3 steering = desired - _velocity;

        // Vector3.ClampMagnitude limita la LONGITUD del vector sin cambiar su
        // dirección. Acá evita que la corrección sea más brusca de lo que
        // permite _maxSteering (multiplicado por deltaTime para que el límite
        // también sea independiente del framerate).
        steering = Vector3.ClampMagnitude(
            steering,
            _maxSteering * Time.deltaTime
        );

        return steering;
    }

    private Vector3 Seek(Vector3 target)
    {
        // Seek: ir hacia el target. Se pide la velocidad deseada hacia él
        // y se calcula cuánto corregir la velocidad actual para lograrlo.
        // Sirve para el caso más básico de IA: un agente que persigue una
        // posición fija sin importar velocidad ni predicción (ej: ir a un punto en el mapa).
        var desired = DesiredVector(target);
        return CalculateSteering(desired);
    }

    private Vector3 Flee(Vector3 target)
    {
        // Flee es Seek invertido: se usa la misma velocidad deseada hacia
        // el target, pero con el signo "-" para que apunte en sentido contrario.
        // Sirve para que el agente escape de algo, por ejemplo un enemigo
        // débil que huye del jugador cuando lo tiene cerca.
        var desired = DesiredVector(target);
        return CalculateSteering(-desired);
    }

    private Vector3 Arrive(Vector3 target)
    {
        // Arrive sirve para llegar a un destino y frenar suavemente ahí,
        // en vez de pasarse de largo o quedar rebotando encima del target
        // (útil para waypoints o cuando el agente debe quedarse quieto al llegar).

        // Vector y distancia hacia el target.
        Vector3 direction = target - transform.position;
        float distance = direction.magnitude; // magnitude = longitud del vector

        // Si ya está muy cerca, no se aplica más steering: evita que el
        // agente "tiemble" tratando de corregir una distancia insignificante.
        if (distance < _minDistance)
            return Vector3.zero;

        // La velocidad deseada es proporcional a la distancia: cuanto más
        // lejos, más rápido; a medida que se acerca a _slowingDistance, frena.
        float targetSpeed = _maxSpeed * (distance / _slowingDistance);

        // Mathf.Min evita que, si está muy lejos, la velocidad supere _maxSpeed.
        float desiredSpeed = Mathf.Min(targetSpeed, _maxSpeed);

        // direction.normalized da la dirección hacia el target; se escala
        // por la velocidad deseada (que ya varía según la distancia).
        Vector3 desired = direction.normalized * desiredSpeed;

        Vector3 steering = CalculateSteering(desired);

        return steering;
    }

    private Vector3 CalculateFuture(Agent target)
    {
        // Este método sirve de base para Pursuit y Evade: en vez de apuntar
        // a donde está el target AHORA, calcula a dónde va a llegar,
        // para no ir siempre "atrás" de un objetivo que se mueve.

        // Vector y distancia hacia la posición ACTUAL del target.
        Vector3 direccion = target.transform.position - transform.position;
        float distance = direccion.magnitude;

        // Estima cuánto tiempo tardaría este agente en llegar hasta el target,
        // considerando también qué tan rápido se mueve el target (para no
        // subestimar el tiempo si el target se está alejando).
        var prediction = distance /
                         (_maxSpeed + target.Velocity.magnitude);

        // Posición futura = posición actual + (velocidad del target * tiempo
        // estimado). Es la fórmula básica de movimiento: distancia = v * t.
        Vector3 futurePosition =
            target.transform.position +
            target.Velocity * prediction;

        return futurePosition;
    }

    private Vector3 Pursuit(Agent target)
    {
        // En vez de perseguir dónde está el target ahora, se calcula dónde
        // va a estar, y se hace Seek hacia ese punto futuro.
        // Sirve para que un enemigo "intercepte" al jugador en vez de
        // perseguirlo siempre por detrás sin nunca alcanzarlo.
        var futurePosition = CalculateFuture(target);
        return Seek(futurePosition);
    }

    private Vector3 Evade(Agent target)
    {
        // Igual que Pursuit pero con Flee: se aleja de la posición futura
        // estimada del target, no de su posición actual.
        // Sirve para escapar de forma más inteligente: si solo huyera de la
        // posición actual del perseguidor, podría terminar corriendo derecho
        // hacia donde el perseguidor va a estar un instante después.
        var futurePosition = CalculateFuture(target);
        return Flee(futurePosition);
    }

    private Vector3 Flocking()
    {
        // Flocking = suma ponderada de tres reglas, cada una con su propio
        // radio de detección de vecinos y su propio peso ajustable desde el
        // Inspector (para poder, por ejemplo, priorizar Separation sobre Cohesion).
        // Sirve para simular movimiento grupal tipo cardumen/bandada: cada
        // agente decide su movimiento solo mirando a sus vecinos cercanos,
        // sin que nadie controle al grupo entero desde afuera.
        return CalculateSeparation(allAgents, _separationRadius) * separationWeight
             + CalculateAlignment(allAgents, _alignmentRadius) * alignmentWeight
             + CalculateCohesion(allAgents, _cohesionRadius) * cohesionWeight;
    }

    private Vector3 CalculateSeparation(List<Agent> list, float radius)
    {
        // Separation sirve para que los agentes no se choquen ni se
        // amontonen unos encima de otros: cada uno se aleja un poco de
        // los vecinos que tiene demasiado cerca.

        // Vector acumulador: va a sumar las direcciones hacia cada vecino
        // cercano para después obtener un promedio.
        Vector3 dessired = default;
        int count = 0;

        // Recorre TODOS los agentes de la escena (no solo los cercanos).
        foreach (var item in list)
        {
            // Se salta a sí mismo para no compararse con su propia posición.
            if (item == this) continue;

            // Vector3.Distance da la distancia entre dos puntos; si el
            // vecino está dentro del radio de separación, se lo tiene en cuenta.
            if (Vector3.Distance(item.transform.position, transform.position) <= radius)
            {
                // Se acumula el vector "hacia" el vecino (vecino - yo).
                dessired += (item.transform.position - transform.position);
                count++;
            }
        }

        // Si no hay vecinos cerca, no hace falta separarse de nadie.
        if (count == 0)
        {
            return Vector3.zero;
        }

        // Promedio de las direcciones acumuladas (divide por la cantidad de vecinos).
        dessired /= count;

        // .normalized se queda solo con la dirección promedio.
        // El "-" invierte esa dirección: en vez de ir HACIA el promedio de
        // vecinos, se aleja de ellos. Se escala a _maxSpeed y se pasa por
        // CalculateSteering para obtener la corrección real a aplicar.
        return CalculateSteering(-dessired.normalized * _maxSpeed);
    }

    private Vector3 CalculateAlignment(List<Agent> list, float radius)
    {
        // Alignment sirve para que el grupo se mueva de forma coordinada,
        // como un cardumen que gira todo junto en la misma dirección,
        // en vez de que cada agente vaya para su lado.

        // Acumula las VELOCIDADES (no posiciones) de los vecinos cercanos.
        Vector3 dessired = default;
        int count = 0;

        foreach (var item in list)
        {
            if (item == this) continue;

            if (Vector3.Distance(item.transform.position, transform.position) <= radius)
            {
                // Se suma la velocidad del vecino, para luego promediarla.
                dessired += item.Velocity;
                count++;
            }
        }

        // Sin vecinos, no hay ninguna dirección grupal con la cual alinearse.
        if (count == 0) return Vector3.zero;

        // Velocidad promedio del grupo cercano.
        dessired /= count;

        // Se toma solo la dirección de esa velocidad promedio (.normalized),
        // se escala a _maxSpeed, y se calcula cuánto corregir para acercarse
        // a moverse en esa misma dirección que el grupo.
        return CalculateSteering(dessired.normalized * _maxSpeed);
    }


    private Vector3 CalculateCohesion(List<Agent> list, float radius)
    {
        // Cohesion sirve para que el grupo se mantenga unido: cada agente
        // tiende levemente hacia el centro de sus vecinos, en vez de que
        // el grupo se disperse con el tiempo.

        // Acumula las POSICIONES de los vecinos cercanos, para hallar
        // el centro del grupo.
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

        // Sin vecinos, no hay grupo hacia el cual acercarse.
        if (count == 0) return Vector3.zero;

        // Posición promedio = centro del grupo cercano.
        desired /= count;

        // Se reutiliza Seek (en vez de repetir la lógica) para ir hacia
        // ese punto central, tal como se iría hacia cualquier otro target.
        return Seek(desired);
    }
}