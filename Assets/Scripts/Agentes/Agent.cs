using UnityEngine;

public class Agent : MonoBehaviour
{
    protected Vector3 _velocity;
    public Vector3 Velocity => _velocity;
    public void StopMoving() => _velocity = Vector3.zero;

    public static float PlanarDistance(Vector3 a, Vector3 b)
    {
        a.y = b.y = 0f;
        return Vector3.Distance(a, b);
    }

    protected void DisablePhysicsInterference()
    {
        if (TryGetComponent(out Rigidbody rb))
        {
            rb.isKinematic = true;
            rb.useGravity = false;
        }
    }
}
