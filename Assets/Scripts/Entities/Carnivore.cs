using System.Collections.Generic;
using Data;
using Managers;
using UnityEngine;
using Utils;
using World;

namespace Entities
{
    [RequireComponent(typeof(SpriteRenderer))]
    public class Carnivore : MonoBehaviour
    {
        private static readonly List<Carnivore> NearC2C = new(32);
        private static readonly List<Herbivore> NearH2C = new(64);

        private float steerTimer;

        public static readonly List<Carnivore> All = new();

        public AgentGenome genome;

        public float speed = 2.2f;
        public float metabolism = 0.6f;
        public float sightRadius = 6f;

        public float energy = 5f;
        public float attackRange = 0.7f;
        public float attackDamage = 100f;
        public float reproduceThreshold = 10f;
        public float childCost = 4.5f;

        private Vector2 dir;
        private float retargetTimer;
        private SpriteRenderer sr;

        private static readonly List<Herbivore> NearH = new(64);

        private void OnEnable()
        {
            All.Add(this);
            EcosystemManager.Instance?.Increment("Carnivores");
        }

        private void OnDisable()
        {
            All.Remove(this);
            EcosystemManager.Instance?.Decrement("Carnivores");
        }

        private void Awake()
        {
            sr = GetComponent<SpriteRenderer>() ?? gameObject.AddComponent<SpriteRenderer>();
            if (genome.speed == 0f)
            {
                genome = AgentGenome.RandomFor(SpeciesId.Carnivore);
            }

            ApplyGenomeAndConfig();
            dir = Random.insideUnitCircle.normalized;
        }

        public void SetupOnSpawn(Vector2 pos, AgentGenome g)
        {
            transform.position = pos;
            genome = g;
            ApplyGenomeAndConfig();
            energy = 5f;
            dir = Random.insideUnitCircle.normalized;
            retargetTimer = 0f;
        }

        private void ApplyGenomeAndConfig()
        {
            var def = ConfigService.Instance?.GetSpecies(SpeciesId.Carnivore);

            var baseSpeed = def ? def.speed : speed;
            var baseMetab = def ? def.metabolism : metabolism;
            var baseVision = def ? def.sightRadius : sightRadius;

            speed = baseSpeed * Mathf.Max(0.05f, genome.speed);
            metabolism = baseMetab * Mathf.Max(0.01f, genome.metabolism);
            sightRadius = baseVision * Mathf.Max(0.1f, genome.vision);

            if (def)
            {
                attackRange = def.attackRange;
                attackDamage = def.attackDamage;
                reproduceThreshold = def.reproduceThreshold;
                childCost = def.childCost;
            }

            if (sr != null)
            {
                // Keep the white sprite so tint does the coloring
                sr.sprite = SpriteFactory.CreateDiscSprite(Color.white);

                // ---- RED-ADJACENT HUE BAND ----
                // Base species color 
                var baseSpecies = ConfigService.Instance?.GetSpeciesColor(SpeciesId.Carnivore, Color.red) ?? Color.red;

                // Convert base to HSV to find the "red" reference hue
                Color.RGBToHSV(baseSpecies, out var hBase, out var sBase, out var vBase);

                // Limit carnivore hue to a small window centered on the base hue.
                // 0.06 ≈ ±22° around red; adjust to taste (smaller = more uniform red).
                const float hueWidth = 0.06f;
                var mappedHue = Mathf.Repeat(hBase + (genome.hue - 0.5f) * (hueWidth * 2f), 1f);

                // Keep them vivid and bright (override low S/V from base if needed)
                var s = Mathf.Max(sBase, 0.95f);
                var v = Mathf.Max(vBase, 0.97f);

                // Final color strictly "red family"
                sr.color = Color.HSVToRGB(mappedHue, s, v);

                var sizeMult = Mathf.Clamp(genome.size, 0.4f, 2f);
                transform.localScale = Vector3.one * Mathf.Lerp(0.75f, 1.5f, Mathf.InverseLerp(0.8f, 1.3f, sizeMult));
            }
        }

        private void Update()
        {
            var dt = Time.deltaTime * EcosystemManager.SimulationSpeed;
            var sim = ConfigService.Instance?.Sim;

            // --- Recompute steering at a fixed cadence ---
            steerTimer -= Time.deltaTime;
            var steerInterval = sim?.steerUpdateInterval ?? 0.2f;
            var maxTurn = sim?.maxTurnDegPerSec ?? 300f;

            if (steerTimer <= 0f)
            {
                steerTimer = steerInterval;

                // Goal: nearest herbivore
                var goalDir = Vector2.zero;
                var prey = FindNearestHerbivore();
                if (prey != null)
                {
                    goalDir = ((Vector2)prey.transform.position - (Vector2)transform.position).normalized;
                }

                // Separation from other carnivores
                var sepDir = Vector2.zero;
                var idx = SpatialIndex.Instance;
                var sepRadius = sim?.separationRadius ?? 1.2f;
                if (idx != null)
                {
                    idx.QueryCarnivores(transform.position, sepRadius, NearC2C);
                    for (var i = 0; i < NearC2C.Count; i++)
                    {
                        var other = NearC2C[i];
                        if (other == null || other == this) continue;

                        var toMe = (Vector2)transform.position - (Vector2)other.transform.position;
                        var d2 = toMe.sqrMagnitude;
                        if (d2 < 1e-6f) continue;

                        sepDir += toMe / d2;
                    }
                }

                if (sepDir.sqrMagnitude > 1e-6f) sepDir.Normalize();

                // Shelter avoidance via gradient
                var grid = ShelterGrid.Instance;
                var grad = Vector2.zero;
                if (grid != null && grid.Enabled)
                {
                    grad = grid.SampleGradient(transform.position);
                }

                // Heading sampler with mild prey bias
                var sampleDir = Vector2.zero;
                if (sim?.headingSamplesEnabled ?? true)
                {
                    var count = Mathf.Clamp(sim?.headingSampleCount ?? 7, 3, 11);
                    var fov = sim?.headingSampleFovDeg ?? 100f;
                    var look = sim?.headingSampleLookahead ?? 1.2f;

                    var best = float.NegativeInfinity;
                    var baseAngle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

                    // Pre-fetch local herbivores for proximity-to-prey bias
                    if (idx != null) idx.QueryHerbivores(transform.position, sepRadius * 1.5f, NearH2C);

                    for (var i = 0; i < count; i++)
                    {
                        var t = count == 1 ? 0f : i / (float)(count - 1);
                        var angle = baseAngle + Mathf.Lerp(-fov * 0.5f, fov * 0.5f, t);
                        var h = Steering.FromAngleDeg(angle).normalized;

                        var probe = (Vector2)transform.position + h * look;
                        var shelter = grid != null && grid.Enabled ? grid.Sample01(probe) : 0f;

                        var preyBias = 0f;
                        if (NearH2C.Count > 0)
                        {
                            var minD2 = float.PositiveInfinity;
                            for (var j = 0; j < NearH2C.Count; j++)
                            {
                                var o = NearH2C[j];
                                if (o == null) continue;
                                var d2 = ((Vector2)o.transform.position - probe).sqrMagnitude;
                                if (d2 < minD2) minD2 = d2;
                            }

                            var d = Mathf.Sqrt(Mathf.Max(1e-3f, minD2));
                            preyBias = 1f - Mathf.Clamp01(d / (sepRadius * 1.5f));
                        }

                        var score = (1f - shelter) + 0.25f * preyBias;
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
                    (sim?.shelterAvoidWeight ?? 0.6f) * (-grad) +
                    (sim?.headingSampleWeight ?? 1.0f) * sampleDir;

                var jitter = sim?.jitterStrength ?? 0.15f;
                if (jitter > 0f) desired += Random.insideUnitCircle * jitter;

                if (desired.sqrMagnitude > 1e-6f) desired.Normalize();
                dir = Steering.RotateTowards(dir, desired, maxTurn * dt);
            }

            // Move with shelter movement cost
            var grid2 = ShelterGrid.Instance;
            var cost = 1f;
            if (grid2 != null && grid2.Enabled) cost = grid2.GetMovementCost(transform.position);
            transform.position += (Vector3)(dir * (speed / Mathf.Max(0.001f, cost)) * dt);

            // Metabolism
            energy -= metabolism * dt;

            // Attack if in range
            var victim = FindNearestHerbivore();
            if (victim != null && Vector2.Distance(transform.position, victim.transform.position) < attackRange)
            {
                Spawner.DespawnHerbivore(victim);
                energy += 3.5f;
            }

            // Reproduce
            if (energy >= reproduceThreshold)
            {
                energy -= childCost;
                var evo = sim != null && sim.useEvolution;
                var ms = sim?.mutationScale ?? 1f;
                var childGenome = evo ? genome.Mutated(SpeciesId.Carnivore, ms) : genome;
                Spawner.SpawnCarnivore((Vector2)transform.position + Random.insideUnitCircle * 0.5f, childGenome);
            }

            // Die
            if (energy <= 0f)
            {
                Spawner.DespawnCarnivore(this);
            }
        }

        private Herbivore FindNearestHerbivore()
        {
            Herbivore best = null;
            var bestD = sightRadius;
            var idx = SpatialIndex.Instance;
            if (idx != null)
            {
                idx.QueryHerbivores(transform.position, sightRadius, NearH);
                for (var i = 0; i < NearH.Count; i++)
                {
                    var h = NearH[i];
                    if (h == null)
                    {
                        continue;
                    }

                    var d = Vector2.Distance(transform.position, h.transform.position);
                    if (d < bestD)
                    {
                        bestD = d;
                        best = h;
                    }
                }

                return best;
            }

            foreach (var h in Herbivore.All)
            {
                var d = Vector2.Distance(transform.position, h.transform.position);
                if (d < bestD)
                {
                    bestD = d;
                    best = h;
                }
            }

            return best;
        }
    }
}