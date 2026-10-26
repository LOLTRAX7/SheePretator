using UnityEngine;

public class BoidHealth : MonoBehaviour
{
    public int HitsRemaining { get; private set; } = 2;
    public bool IsDead => HitsRemaining == 0;
    public void ReceiveHits(int hits) => HitsRemaining = Mathf.Max(0, HitsRemaining - Mathf.Max(0, hits));
    public void ResetHealth() => HitsRemaining = 2;
}
