using UnityEngine;

public class BoidHealth : MonoBehaviour
{
    [SerializeField, Min(1)] private int maxHits = 2;

    public int Current { get; private set; }
    public bool IsDead => Current <= 0;
    private void Awake() => Restore();
    public void Restore() => Current = maxHits;
    
    public void TakeHits(int hits) => Current = Mathf.Max(0, Current - Mathf.Max(0, hits));
}
