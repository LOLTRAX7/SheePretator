#if UNITY_EDITOR
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public class SimulationTests
{
    private GameObject _world;
    private HunterAgent _hunter;
    private BoidAgent _boid;
    private float _oldCapture;

    private static void Set(object target, string field, object value) =>
        target.GetType().GetField(field, BindingFlags.NonPublic | BindingFlags.Instance).SetValue(target, value);
    private static T Prefab<T>(string name) where T : Component =>
        AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/" + name + ".prefab").GetComponent<T>();
    private T Spawn<T>(string name, Vector3 position) where T : Component
    {
        T item = Object.Instantiate(Prefab<T>(name), position, Quaternion.identity);
        item.transform.SetParent(_world.transform);
        Physics.SyncTransforms();
        return item;
    }
    private static IEnumerator Frames(int count)
    {
        for (int i = 0; i < count; i++) yield return null;
    }

    [UnitySetUp]
    public IEnumerator Setup()
    {
        var runner = GameObject.Find("Code-based tests runner");
        if (runner != null) Object.DontDestroyOnLoad(runner);
        var oldScenes = Enumerable.Range(0, SceneManager.sceneCount).Select(SceneManager.GetSceneAt).ToArray();
        var scene = SceneManager.CreateScene("Test_" + System.Guid.NewGuid());
        SceneManager.SetActiveScene(scene);
        foreach (var old in oldScenes) yield return SceneManager.UnloadSceneAsync(old);
        yield return null;
        _oldCapture = Time.captureDeltaTime;
        Time.captureDeltaTime = 1f / 60f;
        _world = new GameObject("Test world");
        _world.AddComponent<Bounds>();
        _hunter = Spawn<HunterAgent>("Hunter", Vector3.zero);
        _boid = Spawn<BoidAgent>("Prey", new Vector3(3f, 0f, 0f));
        // El spawner no interviene en pruebas aisladas.
        Set(_hunter.GetComponent<InterestPointSpawner>(), "poiSpawnInterval", 10000f);
        yield return null;
    }

    [UnityTearDown]
    public IEnumerator Teardown()
    {
        Time.captureDeltaTime = _oldCapture;
        Object.Destroy(_world);
        foreach (InterestPoint food in Object.FindObjectsByType<InterestPoint>(FindObjectsSortMode.None))
            Object.Destroy(food.gameObject);
        yield return null;
    }

    [UnityTest]
    public IEnumerator TwoRangedHitsKillAndCooldownBlocksExtraAttack()
    {
        _hunter.enabled = false;
        _boid.enabled = false;
        _boid.Health.ResetHealth();
        Set(_hunter, "_attackCooldown", 0f);
        Set(_hunter, "moveSpeed", 0f);
        _hunter.StopMoving();
        _hunter.TryAttack(_boid);
        Assert.AreEqual(1, _boid.Health.HitsRemaining);
        _hunter.TryAttack(_boid);
        Assert.AreEqual(1, _boid.Health.HitsRemaining);
        _hunter.enabled = true;
        yield return Frames(125);
        _hunter.TryAttack(_boid);
        Assert.IsTrue(_boid.IsEliminated);
    }

    [UnityTest]
    public IEnumerator MeleeKillsWithTwoHits()
    {
        _hunter.enabled = false;
        Set(_hunter, "_attackCooldown", 0f);
        _boid.Health.ResetHealth();
        _boid.transform.position = Vector3.right;
        _boid.enabled = false;
        _hunter.TryAttack(_boid);
        Assert.IsTrue(_boid.IsEliminated);
        yield return null;
    }

    [UnityTest]
    public IEnumerator SensorsExcludeFarAgentsAndDeduplicateChildColliders()
    {
        _hunter.enabled = _boid.enabled = false;
        Assert.AreEqual(1, _hunter.Sensor.Detect<BoidAgent>(8f).Count);
        _boid.transform.position = Vector3.right * 20f;
        Physics.SyncTransforms();
        Assert.AreEqual(0, _hunter.Sensor.Detect<BoidAgent>(8f).Count);
        yield return null;
    }

    [UnityTest]
    public IEnumerator HuntKeepsPreyOutsideVisionAndDuringCooldown()
    {
        Set(_hunter, "perceptionRadius", 4f);
        _boid.enabled = false;
        yield return Frames(2);
        Assert.AreEqual(HunterAgent.HunterState.Hunt, _hunter.CurrentState);
        _boid.transform.position = Vector3.right * 16f;
        Physics.SyncTransforms();
        BoidAgent decoy = Spawn<BoidAgent>("Prey", _hunter.transform.position + Vector3.forward * 2f);
        decoy.enabled = false;
        yield return Frames(5);
        Assert.AreEqual(HunterAgent.HunterState.Hunt, _hunter.CurrentState);
        Assert.AreSame(_boid, _hunter.Target);
    }

    [UnityTest]
    public IEnumerator DeadThenGatherThenHiddenRespawnThenFullHealth()
    {
        _boid.ReceiveHits(2);
        yield return Frames(2);
        Assert.AreEqual(BoidAgent.BoidState.Dead, _boid.CurrentState);
        Vector3 deathPosition = _boid.transform.position;
        yield return Frames(240);
        Assert.AreEqual(BoidAgent.BoidState.Respawn, _boid.CurrentState);
        Assert.AreEqual(deathPosition, _boid.transform.position);
        _hunter.enabled = false;
        Assert.IsTrue(_boid.GetComponentsInChildren<Renderer>().All(r => !r.enabled));
        yield return Frames(360);
        Assert.AreEqual(2, _boid.Health.HitsRemaining);
        Assert.IsTrue(Bounds.Instance.Contains(_boid.transform.position));
        Assert.Greater(_hunter.GatheredCount, 0);
    }

    [UnityTest]
    public IEnumerator ExhaustionEntersRestIgnoresCorpsesAndRechargesOnlyThere()
    {
        Set(_hunter.Stamina, "drainPerSecond", 6000f);
        Set(_hunter.Stamina, "rechargePerSecond", 20f);
        _boid.enabled = false;
        yield return Frames(4);
        Assert.AreEqual(HunterAgent.HunterState.Rest, _hunter.CurrentState);
        _boid.ReceiveHits(2);
        Vector3 position = _hunter.transform.position;
        yield return Frames(30);
        Assert.AreEqual(HunterAgent.HunterState.Rest, _hunter.CurrentState);
        Assert.AreEqual(position, _hunter.transform.position);
        Assert.Greater(_hunter.Energy, 0f);
        yield return Frames(290);
        Assert.AreNotEqual(HunterAgent.HunterState.Rest, _hunter.CurrentState);
    }

    [UnityTest]
    public IEnumerator EvadeOverridesFoodAndDeadOverridesEvade()
    {
        Spawn<InterestPoint>("InterestPoint", new Vector3(4f, 0f, 0f));
        yield return Frames(2);
        Assert.AreEqual(BoidAgent.BoidState.Evade, _boid.CurrentState);
        _boid.ReceiveHits(2);
        yield return Frames(2);
        Assert.AreEqual(BoidAgent.BoidState.Dead, _boid.CurrentState);
    }

    [UnityTest]
    public IEnumerator ArriveConsumesFoodWithoutOverlap()
    {
        _hunter.gameObject.SetActive(false);
        _boid.transform.position = Vector3.zero;
        InterestPoint food = Spawn<InterestPoint>("InterestPoint", Vector3.right * 4f);
        Set(_boid, "harvestInterval", 0.1f);
        yield return Frames(360);
        Assert.IsTrue(food == null, "El boid debe llegar, consumir y destruir el objeto.");
    }

    [UnityTest]
    public IEnumerator MovingBoidsDoNotOverlapFoodOrEachOther()
    {
        _hunter.gameObject.SetActive(false);
        BoidAgent other = Spawn<BoidAgent>("Prey", _boid.transform.position + Vector3.right * 1.1f);
        InterestPoint food = Spawn<InterestPoint>("InterestPoint", Vector3.forward * 4f);
        for (int i = 0; i < 300; i++)
        {
            yield return null;
            Assert.GreaterOrEqual(Agent.PlanarDistance(_boid.transform.position, other.transform.position), 0.999f);
            if (food != null)
                Assert.GreaterOrEqual(Agent.PlanarDistance(_boid.transform.position, food.transform.position), _boid.Steering.BodyRadius + food.Radius - 0.001f);
        }
    }

    [UnityTest]
    public IEnumerator BoidsTurnAtBoundsAndRemainInside()
    {
        _hunter.gameObject.SetActive(false);
        _boid.transform.position = new Vector3(29.49f, 0f, 0f);
        Set(_boid, "_velocity", Vector3.right * 4f);
        Physics.SyncTransforms();
        yield return Frames(10);
        Assert.IsTrue(Bounds.Instance.Contains(_boid.transform.position));
        Assert.Less(_boid.transform.position.x, 29.49f);
        Assert.Less(_boid.Velocity.x, 0f);
    }
    [UnityTest]
    public IEnumerator SceneCompletesHuntingCycleInTwoMinutes()
    {
        Object.Destroy(_world);
        yield return null;
        Random.InitState(12345);
        SceneManager.LoadScene("Lvl1");
        yield return Frames(2);
        HunterAgent hunter = Object.FindFirstObjectByType<HunterAgent>();
        Assert.IsNotNull(hunter);
        Assert.GreaterOrEqual(Object.FindObjectsByType<BoidAgent>(FindObjectsSortMode.None).Length, 6);
        bool respawn = false;
        for (int i = 0; i < 7200; i++)
        {
            yield return null;
            Assert.LessOrEqual(InterestPoint.ActiveCount, 5);
            if (i % 30 == 0)
                respawn |= Object.FindObjectsByType<BoidAgent>(FindObjectsSortMode.None).Any(b => b.CurrentState == BoidAgent.BoidState.Respawn);
        }
        Debug.Log($"[Verificación 120s] Ataques={hunter.Attacks}, Kills={hunter.Kills}, Recolectados={hunter.GatheredCount}, Respawn={respawn}");
        Assert.Greater(hunter.Kills, 0, "El cazador debe concretar kills en la escena real.");
        Assert.Greater(hunter.GatheredCount, 0);
        Assert.IsTrue(respawn);
    }
}
#endif



