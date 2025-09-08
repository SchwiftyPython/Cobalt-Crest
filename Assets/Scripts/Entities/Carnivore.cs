using System.Collections.Generic;
using UnityEngine;

/// <summary>Simple carnivore: hunts nearest herbivore, gains energy on kill.</summary>
[RequireComponent(typeof(SpriteRenderer))]
public class Carnivore : MonoBehaviour
{
    public static readonly List<Carnivore> All = new List<Carnivore>();

    [Header("Genome (Phase 1)")]
    public float speed = 2.2f;
    public float metabolism = 0.6f;

    [Header("Life")]
    public float energy = 5.0f;
    public float attackRange = 0.7f;
    public float attackDamage = 100f; // instant kill for Phase 1
    public float reproduceThreshold = 10.0f;
    public float childCost = 4.5f;
    public float sightRadius = 6f;

    private Vector2 _dir;
    private float _retargetTimer;
    private SpriteRenderer _sr;

    private void OnEnable()
    {
        All.Add(this);
        EcosystemManager.Instance?.Increment("Carnivores");
    }

    private void OnDisable()
    {
        All.Remove(this);
        if (EcosystemManager.Instance != null) EcosystemManager.Instance.Decrement("Carnivores");
    }

    void Awake()
    {
        _sr = GetComponent<SpriteRenderer>();
        _sr.sprite = SpriteFactory.CreateDiscSprite(Color.red, 16);
        _dir = Random.insideUnitCircle.normalized;
    }

    void Update()
    {
        float dt = Time.deltaTime * EcosystemManager.SimulationSpeed;
        _retargetTimer -= dt;

        if (_retargetTimer <= 0f)
        {
            _retargetTimer = 0.35f + Random.value * 0.35f;
            var prey = FindNearestHerbivore();
            if (prey != null)
                _dir = ((Vector2)prey.transform.position - (Vector2)transform.position).normalized;
            else
                _dir = Vector2.Lerp(_dir, Random.insideUnitCircle.normalized, 0.5f);
        }

        transform.position += (Vector3)(_dir * speed * dt);
        energy -= metabolism * dt;

        var h = FindNearestHerbivore();
        if (h != null && Vector2.Distance(transform.position, h.transform.position) < attackRange)
        {
            // "Kill" herbivore
            Destroy(h.gameObject);
            energy += 3.5f;
        }

        // Reproduce
        if (energy >= reproduceThreshold)
        {
            energy -= childCost;
            Spawner.SpawnCarnivore(transform.position + (Vector3)Random.insideUnitCircle * 0.5f);
        }

        if (energy <= 0f)
        {
            Destroy(gameObject);
        }
    }

    private Herbivore FindNearestHerbivore()
    {
        Herbivore best = null;
        float bestD = sightRadius;
        foreach (var h in Herbivore.All)
        {
            float d = Vector2.Distance(transform.position, h.transform.position);
            if (d < bestD) { bestD = d; best = h; }
        }
        return best;
    }
}
