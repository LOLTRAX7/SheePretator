using UnityEngine;

public class HunterEnergy : MonoBehaviour
{
    [SerializeField, Min(1f)] private float maxEnergy = 18f;
    [SerializeField, Min(0.01f)] private float drainRate = 1f;
    [SerializeField, Min(0.01f)] private float rechargeRate = 6f;

    public float Current { get; private set; }
    public float Maximum => maxEnergy;
    public bool IsEmpty => Current <= 0f;
    public bool IsFull => Current >= maxEnergy;
    
    private void Awake() => Current = maxEnergy;
    public void Spend() => Current = Mathf.Max(0f, Current - drainRate * Time.deltaTime);
    public void Recharge() => Current = Mathf.Min(maxEnergy, Current + rechargeRate * Time.deltaTime);
}
