using System.Collections.Generic;
using UnityEngine;

/// <summary>Simple herbivore: wanders, seeks nearest producer periodically, eats, reproduces, dies.</summary>
[RequireComponent(typeof(SpriteRenderer))]
public class Herbivore : MonoBehaviour
{
    public static readonly List<Herbivore> All = new List<Herbivore>();

    [Header("Genome (Phase 1)")]
    public float speed = 1.8f;
    public float metabolism = 0.4f;

    [Header("Life")]
    public float energy = 4.0f;
    public float eatRate = 1.2f;
    public float reproduceThreshold = 8.0f;
    public float childCost = 3.5f;
    public float sightRadius = 4f;

    private Vector2 _dir;
    private float _retargetTimer;
    private float _wanderTimer;
    private SpriteRenderer _sr;

    private void OnEnable()
    {
        All.Add(this);
        EcosystemManager.Instance?.Increment("Herbivores");
    }

    private void OnDisable()
    {
        All.Remove(this);
        if (EcosystemManager.Instance != null) EcosystemManager.Instance.Decrement("Herbivores");
    }

    void Awake()
    {
        _sr = GetComponent<SpriteRenderer>();
        _sr.sprite = SpriteFactory.CreateDiscSprite(Color.cyan, 14);
        _dir = Random.insideUnitCircle.normalized;
    }

    void Update()
    {
        float dt = Time.deltaTime * EcosystemManager.SimulationSpeed;
        _retargetTimer -= dt;
        _wanderTimer -= dt;

        // Movement / targeting
        if (_retargetTimer <= 0f)
        {
            _retargetTimer = 0.5f + Random.value * 0.5f;
            var target = FindNearestProducer();
            if (target != null)
            {
                _dir = ((Vector2)target.transform.position - (Vector2)transform.position).normalized;
            }
            else if (_wanderTimer <= 0f)
            {
                _dir = Vector2.Lerp(_dir, Random.insideUnitCircle.normalized, 0.5f);
                _wanderTimer = 1f + Random.value;
            }
        }

        transform.position += (Vector3)(_dir * speed * dt);
        energy -= metabolism * dt;

        // Try to eat any producer in range
        var p = FindNearestProducer();
        if (p != null && Vector2.Distance(transform.position, p.transform.position) < 0.6f)
        {
            float gained = p.Consume(eatRate * dt);
            energy += gained;
        }

        // Reproduce
        if (energy >= reproduceThreshold)
        {
            energy -= childCost;
            Spawner.SpawnHerbivore(transform.position + (Vector3)Random.insideUnitCircle * 0.5f);
        }

        // Die
        if (energy <= 0f)
        {
            Destroy(gameObject);
        }
    }

    private Producer FindNearestProducer()
    {
        Producer best = null;
        float bestD = sightRadius;
        foreach (var p in Producer.All)
        {
            float d = Vector2.Distance(transform.position, p.transform.position);
            if (d < bestD) { bestD = d; best = p; }
        }
        return best;
    }
}
