// Clase base abstracta para todos los estados de una FSM. Genérica en T (el tipo usado como ID).
public abstract class State<T>
{
    // Referencia a la FSM dueña de este estado.
    protected FiniteStateMachine<T> _fsm;

    // La FSM llama esto al registrar el estado (AddState), para que sepa a qué máquina pertenece.
    public void SetFSM(FiniteStateMachine<T> fsm) => _fsm = fsm;

    // Cada estado concreto define su propia lógica acá.
    public abstract void Enter();  // al entrar al estado
    public abstract void Update(); // cada frame mientras está activo
    public abstract void Exit();   // al salir del estado
}