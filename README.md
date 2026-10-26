# Recuperatorio del primer parcial

Abrir el proyecto con **Unity 6000.3.17f1**, cargar `Assets/Scenes/Lvl1.unity` y presionar Play. La escena genera ocho boids. El panel superior izquierdo muestra el estado, presa, energía, TBA, ataques, kills y recolecciones del cazador.

## Qué cambió y por qué

El cazador antes se detenía durante la preparación del ataque y volvía a Patrol después de atacar. La presa podía escapar, y el siguiente ataque podía apuntar a otro boid. Además, el prefab tenía MeleeAttackRadius = 4 y RangeAttackRadius = 1.5, al revés de lo pedido.

Ahora Hunt mantiene la presa aunque salga de visión, continúa Pursuit mientras espera el TBA y ataca sin una pausa de preparación. Un ataque a distancia quita un golpe; uno cuerpo a cuerpo quita dos. Cada boid tiene dos golpes de vida. Los radios del prefab son 1.5 para melee y 5 para distancia.

Se conservó la base `FiniteStateMachine<T>` y `State<T>` del ejemplo del profesor. Las transiciones se evalúan en los estados. `Update` de los agentes solo actualiza el temporizador general y la FSM.

## Organización para estudiar el código

- `BoidAgent`: conecta componentes y registra sus seis estados.
- `BoidStates`: Flocking, Evade, SeekObject, Consume, Dead y Respawn. Los estados vivos comparten la evaluación de prioridades para no repetirla en cuatro lugares.
- `BoidHealth`: vida de dos golpes, sin lógica de movimiento ni transiciones.
- `BoidSteering`: Separation, Alignment, Cohesion, Evade, Arrive y movimiento sin superponer los cuerpos.
- `ProximitySensor`: detecta componentes mediante `Physics.OverlapSphere`. Descarta el propio agente, filtra por radio y evita contar dos veces un objeto que tiene varios colliders.
- `AgentFeedback`: colores por estado y visibilidad de todos los renderers y colliders al reaparecer.
- `HunterAgent`: registra sus estados y ofrece movimiento, percepción y ataque.
- `HunterPatrolState`, `HunterHuntState`, `HunterGatherState`, `HunterRestState`: decisiones del cazador.
- `HunterEnergy`: consume energía cuando Hunt lo pide y recarga cuando Rest lo pide.
- `HunterDebugDisplay`: panel de información durante Play.
- `InterestPointSpawner`: temporizador de generación durante Patrol y límite de cinco objetos.
- `InterestPoint`: durabilidad y color del objeto al recibir daño.
- `Bounds` y `BoidSpawner`: límites y posiciones iniciales libres. No deciden el comportamiento de los agentes.

## Cómo funcionan las decisiones

**Boids:** Dead tiene prioridad absoluta. Si están vivos y ven al cazador, pasan a Evade. Sin amenaza, buscan comida cercana: si llegaron pasan a Consume y, si falta distancia, a SeekObject. Sin comida percibida, Flocking. Al recolectarlos pasan de Dead a Respawn, se ocultan y esperan antes de aparecer con dos golpes de vida.

**Cazador:** Patrol prioriza un cadáver visible; si no hay ninguno, solo entra en Hunt cuando hay presa visible y terminó el TBA. Hunt consume energía. Sus salidas son Rest por agotamiento, Gather por un cadáver visible, o Patrol si la presa sale del mapa. Si la presa muere y no hay cadáver visible, busca otra presa percibida; si no la encuentra, sigue a la última posición conocida. Gather tarda un tiempo en recolectar y vuelve a Patrol. Rest ignora presas y cadáveres hasta completar la energía.

**Steering:** se calcula una velocidad deseada y se resta la velocidad actual para obtener la fuerza de dirección. Separation aleja de vecinos cercanos; Alignment busca la dirección promedio; Cohesion busca su centro. Pursuit y Evade predicen la posición futura usando posición y velocidad del objetivo. Arrive reduce la velocidad según la distancia libre hasta el objeto.

**Sin superposiciones:** Separation ayuda a mantener distancia, pero una fuerza por sí sola no garantiza evitar el contacto. El movimiento del boid se divide en pasos cortos y comprueba cuerpos cercanos para impedir atravesarlos. La generación y el respawn también buscan posiciones libres.

## Percepción local y contador de comida

No hay listas estáticas de boids, cazadores u objetos. Cada sensor consulta colliders dentro de su radio. La excepción de memoria que pide la consigna es la presa fijada en Hunt: puede seguirse fuera de visión hasta morir o provocar una salida válida.

`InterestPoint.ActiveCount` es solamente un número, usado para impedir que se generen más de cinco objetos. No contiene referencias ni posiciones y los boids no lo consultan. No sirve para localizar comida ni coordinar al grupo.

## Configuración en el Inspector

En el prefab Prey, ajustar radios de percepción y consumo en BoidAgent; velocidades, pesos y radios de flocking en BoidSteering; colores en AgentFeedback.

En Hunter, ajustar velocidad, waypoints, TBA, radios y recolección en HunterAgent; energía, consumo y recarga en HunterEnergy; intervalo y prefab de comida en InterestPointSpawner. Los componentes ya están agregados a ambos prefabs.

Los Gizmos al seleccionar agentes muestran separación, vecinos, visión, comida y ataques. El mapa contiene los cuerpos dentro de sus límites. Al intentar cruzar un borde, el boid invierte la componente de velocidad correspondiente para seguir moviéndose dentro del mapa.

## Comprobación

Abrir Window > General > Test Runner y ejecutar las pruebas de PlayMode de `SimulationTests`. Incluyen daño y TBA, percepción local, presa fija, prioridades, descanso, consumo, superposiciones y dos minutos simulados de la escena real.

Para una revisión manual, observar que suban Kills y Recolectados; que un cadáver quede inmóvil; que desaparezcan también los ojos al recolectarlo; y que reaparezca luego. Para observar Rest más seguido, aumentar el consumo de energía en HunterEnergy. Revisar también los cambios de colores cerca de comida y cazador.

## Preguntas para practicar la defensa

1. ¿Por qué no basta con filtrar por distancia una lista global de boids?
2. ¿Qué diferencia hay entre Seek y Pursuit, y entre Flee y Evade?
3. ¿Dónde se evalúa la transición de un boid a Dead?
4. ¿Por qué el TBA se actualiza fuera de los estados, pero sus transiciones se evalúan dentro?
5. ¿Por qué Separation no garantiza por sí sola evitar superposiciones?
6. ¿Por qué un cadáver no puede sacar al cazador de Rest?

GitHub y el PDF de entrega quedan a cargo del alumno, según lo acordado.


## Resultado de la verificación automática

Verificado el 23/10/2026 con Unity **6000.3.17f1**, en PlayMode sin ventana gráfica: **11 pruebas aprobadas, 0 fallidas**.

En la escena Lvl1, con semilla 12345 durante 7200 frames a 1/60 s (120 segundos simulados), el cazador realizó **15 ataques, 7 kills y 7 recolecciones**. Se observó el estado Respawn. El número exacto puede variar en otras ejecuciones por las posiciones iniciales aleatorias.

También se verificaron dos golpes a distancia, ataque melee, bloqueo por TBA, sensores locales sin duplicados, presa fija fuera de visión, prioridades de estados, descanso y recarga, consumo de comida, prevención de superposiciones y giro en los bordes.

Los resultados detallados de esta ejecución local están en `Logs/recuperatorio-final.xml`; la carpeta Logs está excluida del repositorio. Las pruebas reproducibles están en `Assets/Tests/PlayMode/SimulationTests.cs`.
