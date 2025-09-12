using System.Collections.Generic;
using Data;
using Managers;
using UnityEngine;
using Utils;
using World;

namespace Entities
{
    [RequireComponent(typeof(SpriteRenderer))]
    public class Herbivore : MonoBehaviour
    {
        private static readonly List<Herbivore> NearH2 = new(64);
        private static readonly List<Carnivore> NearC2 = new(32);

        private float _steerTimer;

        public static readonly List<Herbivore> All = new();

        // Genome and derived phenotype
        public AgentGenome genome;

        // Phenotype values used by the behavior loop
        public float speed = 1.8f;
        public float metabolism = 0.4f;
        public float sightRadius = 4f;

        public float energy = 4f;
        public float eatRate = 1.2f;
        public float reproduceThreshold = 8f;
        public float childCost = 3.5f;

        private Vector2 _dir;
        private float _retargetTimer, _wanderTimer;
        private SpriteRenderer _sr;

        static readonly List<Producer> _nearP = new(64);

        private void OnEnable()
        {
            All.Add(this);
            EcosystemManager.Instance?.Increment("Herbivores");
        }

        private void OnDisable()
        {
            All.Remove(this);
            EcosystemManager.Instance?.Decrement("Herbivores");
        }

        void Awake()
        {
            _sr = GetComponent<SpriteRenderer>() ?? gameObject.AddComponent<SpriteRenderer>();
            // Default init in case SetupOnSpawn isn't called (editor placed)
            if (genome.Equals(default(AgentGenome)))
            {
                genome = AgentGenome.RandomFor(SpeciesId.Herbivore);
            }

            ApplyGenomeAndConfig();
            _dir = Random.insideUnitCircle.normalized;
        }

        public void SetupOnSpawn(Vector2 pos, AgentGenome g)
        {
            transform.position = pos;
            genome = g;
            ApplyGenomeAndConfig();
            energy = 4f;
            _dir = Random.insideUnitCircle.normalized;
            _retargetTimer = _wanderTimer = 0f;
        }

        private void ApplyGenomeAndConfig()
        {
            var def = ConfigService.Instance?.GetSpecies(SpeciesId.Herbivore);

            // Base values from species def (fall back to current/public defaults)
            var baseSpeed = def ? def.speed : speed;
            var baseMetab = def ? def.metabolism : metabolism;
            var baseVision = def ? def.sightRadius : sightRadius;

            // Multiply by genome (genome ranges should be centered ~1.0 for multipliers)
            speed = baseSpeed * Mathf.Max(0.05f, genome.speed);
            metabolism = baseMetab * Mathf.Max(0.01f, genome.metabolism);
            sightRadius = baseVision * Mathf.Max(0.1f, genome.vision);

            // Other species config (unchanged)
            if (def)
            {
                eatRate = def.eatRate;
                reproduceThreshold = def.reproduceThreshold;
                childCost = def.childCost;
            }

            // Visuals: hue & size
            if (_sr != null)
            {
                _sr.sprite = SpriteFactory.CreateDiscSprite(
                    ConfigService.Instance?.GetSpeciesColor(SpeciesId.Herbivore, Color.cyan) ?? Color.cyan, 14);
                _sr.color = AgentGenome.HueToColor(genome.hue);
                var sizeMult = Mathf.Clamp(genome.size, 0.4f, 2f);
                transform.localScale = Vector3.one * Mathf.Lerp(0.7f, 1.4f, Mathf.InverseLerp(0.8f, 1.3f, sizeMult));
            }
        }

        private void Update()
        {
            var dt = Time.deltaTime * EcosystemManager.SimulationSpeed;
            var sim = ConfigService.Instance?.Sim;

            // --- Recompute steering at a fixed cadence ---
            _steerTimer -= Time.deltaTime;
            var steerInterval = sim?.steerUpdateInterval ?? 0.2f;
            var maxTurn = sim?.maxTurnDegPerSec ?? 300f;

            if (_steerTimer <= 0f)
            {
                _steerTimer = steerInterval;

                // Goal: nearest producer
                var goalDir = Vector2.zero;
                var target = FindNearestProducer();
                if (target != null)
                {
                    goalDir = ((Vector2)target.transform.position - (Vector2)transform.position).normalized;
                }

                // Separation from other herbivores (1/r^2 falloff)
                var sepDir = Vector2.zero;
                var idx = SpatialIndex.Instance;
                var sepRadius = sim?.separationRadius ?? 1.2f;
                if (idx != null)
                {
                    idx.QueryHerbivores(transform.position, sepRadius, NearH2);
                    for (var i = 0; i < NearH2.Count; i++)
                    {
                        var other = NearH2[i];
                        if (other == null || other == this) continue;

                        var toMe = (Vector2)transform.position - (Vector2)other.transform.position;
                        var d2 = toMe.sqrMagnitude;
                        if (d2 < 1e-6f) continue;

                        sepDir += toMe / d2;
                    }
                }

                if (sepDir.sqrMagnitude > 1e-6f) sepDir.Normalize();

                // Predator avoidance (carnivores)
                var fleeDir = Vector2.zero;
                var threatR = sim?.predatorThreatRadius ?? 3.0f;
                if (idx != null)
                {
                    idx.QueryCarnivores(transform.position, threatR, NearC2);
                    for (var i = 0; i < NearC2.Count; i++)
                    {
                        var c = NearC2[i];
                        if (c == null) continue;

                        var away = (Vector2)transform.position - (Vector2)c.transform.position;
                        var d2 = away.sqrMagnitude;
                        if (d2 < 1e-6f) continue;

                        fleeDir += away / d2;
                    }
                }

                if (fleeDir.sqrMagnitude > 1e-6f) fleeDir.Normalize();

                // Shelter avoidance via gradient
                var grid = ShelterGrid.Instance;
                var grad = Vector2.zero;
                if (grid != null && grid.Enabled)
                {
                    grad = grid.SampleGradient(transform.position); // points toward density
                }

                // Optional heading sampling (feelers)
                var sampleDir = Vector2.zero;
                if (sim?.headingSamplesEnabled ?? true)
                {
                    var count = Mathf.Clamp(sim?.headingSampleCount ?? 7, 3, 11);
                    var fov = sim?.headingSampleFovDeg ?? 100f;
                    var look = sim?.headingSampleLookahead ?? 1.2f;

                    var best = float.NegativeInfinity;
                    var baseAngle = Mathf.Atan2(_dir.y, _dir.x) * Mathf.Rad2Deg;

                    for (var i = 0; i < count; i++)
                    {
                        var t = count == 1 ? 0f : i / (float)(count - 1);
                        var angle = baseAngle + Mathf.Lerp(-fov * 0.5f, fov * 0.5f, t);
                        var h = Steering.FromAngleDeg(angle).normalized;

                        var probe = (Vector2)transform.position + h * look;
                        var shelter = grid != null && grid.Enabled ? grid.Sample01(probe) : 0f;

                        // Proximity penalty vs. nearby herbivores at the probe
                        var prox = 0f;
                        if (NearH2.Count > 0)
                        {
                            var minD2 = float.PositiveInfinity;
                            for (var j = 0; j < NearH2.Count; j++)
                            {
                                var o = NearH2[j];
                                if (o == null || o == this) continue;
                                var d2 = ((Vector2)o.transform.position - probe).sqrMagnitude;
                                if (d2 < minD2) minD2 = d2;
                            }

                            if (!float.IsInfinity(minD2))
                            {
                                var d = Mathf.Sqrt(Mathf.Max(1e-4f, minD2));
                                var r = sepRadius;
                                prox = Mathf.Clamp01((r - d) / r); // 1 if inside separation radius
                            }
                        }

                        var score = (1f - shelter) + (1f - prox) * 0.5f;
                        if (score > best)
                        {
                            best = score;
                            sampleDir = h;
                        }
                    }
                }

                // Blend forces
                var desired =
                    (sim?.goalWeight ?? 1f) * goalDir +
                    (sim?.separationWeight ?? 1.2f) * sepDir +
                    (sim?.predatorAvoidWeight ?? 2f) * fleeDir +
                    (sim?.shelterAvoidWeight ?? 0.6f) * (-grad) +
                    (sim?.headingSampleWeight ?? 1.0f) * sampleDir;

                // Small jitter to break symmetry
                var jitter = sim?.jitterStrength ?? 0.15f;
                if (jitter > 0f) desired += Random.insideUnitCircle * jitter;

                if (desired.sqrMagnitude > 1e-6f) desired.Normalize();
                _dir = Steering.RotateTowards(_dir, desired, maxTurn * dt);
            }

            // Move with shelter movement cost
            var grid2 = ShelterGrid.Instance;
            var cost = 1f;
            if (grid2 != null && grid2.Enabled) cost = grid2.GetMovementCost(transform.position);
            transform.position += (Vector3)(_dir * (speed / Mathf.Max(0.001f, cost)) * dt);

            // Metabolism
            energy -= metabolism * dt;

            // Eat producer on contact
            var p = FindNearestProducer();
            if (p != null && Vector2.Distance(transform.position, p.transform.position) < 0.6f)
            {
                energy += p.Consume(eatRate * dt);
            }

            // Reproduce
            if (energy >= reproduceThreshold)
            {
                energy -= childCost;
                var evo = sim != null && sim.useEvolution;
                var ms = sim?.mutationScale ?? 1f;
                var childGenome = evo ? genome.Mutated(SpeciesId.Herbivore, ms) : genome;
                Spawner.SpawnHerbivore((Vector2)transform.position + Random.insideUnitCircle * 0.5f, childGenome);
            }

            // Die
            if (energy <= 0f)
            {
                Spawner.DespawnHerbivore(this);
            }
        }


        private Producer FindNearestProducer()
        {
            Producer best = null;
            var bestD = sightRadius;
            var idx = SpatialIndex.Instance;
            if (idx != null)
            {
                idx.QueryProducers(transform.position, sightRadius, _nearP);
                for (var i = 0; i < _nearP.Count; i++)
                {
                    var p = _nearP[i];
                    if (p == null)
                    {
                        continue;
                    }

                    var d = Vector2.Distance(transform.position, p.transform.position);
                    if (d < bestD)
                    {
                        bestD = d;
                        best = p;
                    }
                }

                return best;
            }

            foreach (var p in Producer.All)
            {
                var d = Vector2.Distance(transform.position, p.transform.position);
                if (d < bestD)
                {
                    bestD = d;
                    best = p;
                }
            }

            return best;
        }
    }
}