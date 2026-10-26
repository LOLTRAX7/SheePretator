using UnityEngine;

public class HunterEnergy : MonoBehaviour
{
    [SerializeField, Min(1f)] private float maxEnergy = 100f;
    [SerializeField, Min(0.1f)] private float drainPerSecond = 8f;
    [SerializeField, Min(0.1f)] private float rechargePerSecond = 20f;
    public float Energy { get; private set; }
    public float MaxEnergy => maxEnergy;
    private void Awake() => Energy = maxEnergy;
    public void Drain() => Energy = Mathf.Max(0f, Energy - drainPerSecond * Time.deltaTime);
    public void Recharge() => Energy = Mathf.Min(maxEnergy, Energy + rechargePerSecond * Time.deltaTime);
}
