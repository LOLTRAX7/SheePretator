using UnityEngine;

public class HunterDebugDisplay : MonoBehaviour
{
    [SerializeField] private bool showOnScreenDebug = true;
    private HunterAgent _hunter;
    private void Awake() => _hunter = GetComponent<HunterAgent>();
    private void OnGUI()
    {
        if (!showOnScreenDebug || _hunter.Sensor == null) return;
        int living = 0, dead = 0;
        foreach (BoidAgent boid in _hunter.Sensor.Detect<BoidAgent>(_hunter.VisionRadius))
        {
            if (boid.IsPendingGather) dead++;
            else if (!boid.IsEliminated) living++;
        }
        string target = _hunter.Target != null ? _hunter.Target.name : "-";
        GUI.Box(new Rect(10, 10, 370, 225), "Cazador");
        GUI.Label(new Rect(22, 35, 350, 195),
            $"Estado: {_hunter.CurrentState}\nObjetivo: {target}\n"
            + $"Energía: {_hunter.Energy:0.0}/{_hunter.MaxEnergy:0}\nTBA restante: {_hunter.Cooldown:0.0}s\n"
            + $"Detectados: {living} vivos / {dead} caídos\nÚltima acción: {_hunter.LastAction}\n"
            + $"Ataques: {_hunter.Attacks} | Kills: {_hunter.Kills} | Recolectados: {_hunter.GatheredCount}\n"
            + "Boids: blanco flocking / naranja evade\nCeleste seek / verde consume / gris dead");
    }
}
