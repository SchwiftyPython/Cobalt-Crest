using System.Collections.Generic;
using Data;
using Managers;
using UnityEngine;

namespace Entities
{
    [RequireComponent(typeof(SpriteRenderer))]
    public class Herbivore : MonoBehaviour
    {
        public static readonly List<Herbivore> All = new List<Herbivore>();

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

        static readonly List<Producer> _nearP = new List<Producer>(64);

        private void OnEnable()  { All.Add(this);  EcosystemManager.Instance?.Increment("Herbivores"); }
        private void OnDisable() { All.Remove(this); EcosystemManager.Instance?.Decrement("Herbivores"); }

        void Awake()
        {
            _sr = GetComponent<SpriteRenderer>() ?? gameObject.AddComponent<SpriteRenderer>();
            // Default init in case SetupOnSpawn isn't called (editor placed)
            if (genome.speed == 0f)
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
            var baseVision= def ? def.sightRadius : sightRadius;

            // Multiply by genome (genome ranges should be centered ~1.0 for multipliers)
            speed      = baseSpeed * Mathf.Max(0.05f, genome.speed);
            metabolism = baseMetab * Mathf.Max(0.01f, genome.metabolism);
            sightRadius= baseVision * Mathf.Max(0.1f,  genome.vision);

            // Other species config (unchanged)
            if (def) { eatRate = def.eatRate; reproduceThreshold = def.reproduceThreshold; childCost = def.childCost; }

            // Visuals: hue & size
            if (_sr != null)
            {
                _sr.sprite = SpriteFactory.CreateDiscSprite(ConfigService.Instance?.GetSpeciesColor(SpeciesId.Herbivore, Color.cyan) ?? Color.cyan, 14);
                _sr.color  = AgentGenome.HueToColor(genome.hue);
                var sizeMult = Mathf.Clamp(genome.size, 0.4f, 2f);
                transform.localScale = Vector3.one * Mathf.Lerp(0.7f, 1.4f, Mathf.InverseLerp(0.8f, 1.3f, sizeMult));
            }
        }

        void Update()
        {
            var dt = Time.deltaTime * EcosystemManager.SimulationSpeed;
            _retargetTimer -= dt;
            _wanderTimer   -= dt;

            var seekInterval   = (ConfigService.Instance?.Sim?.defaultSeekInterval)    ?? 0.5f;
            var wanderInterval = (ConfigService.Instance?.Sim?.herbivoreWanderInterval)?? 1.0f;

            if (_retargetTimer <= 0f)
            {
                _retargetTimer = seekInterval;
                var target = FindNearestProducer();
                if (target != null)
                {
                    _dir = ((Vector2)target.transform.position - (Vector2)transform.position).normalized;
                }
                else if (_wanderTimer <= 0f)
                {
                    _dir = Vector2.Lerp(_dir, Random.insideUnitCircle.normalized, 0.5f);
                    _wanderTimer = wanderInterval;
                }
            }

            transform.position += (Vector3)(_dir * speed * dt);
            energy -= metabolism * dt;

            var p = FindNearestProducer();
            if (p != null && Vector2.Distance(transform.position, p.transform.position) < 0.6f)
            {
                energy += p.Consume(eatRate * dt);
            }

            if (energy >= reproduceThreshold)
            {
                energy -= childCost;

                var sim   = ConfigService.Instance?.Sim;
                var evo  = sim != null && sim.useEvolution;
                var ms  = sim != null ? sim.mutationScale : 1f;

                var childGenome = evo ? genome.Mutated(SpeciesId.Herbivore, ms) : genome;
                Spawner.SpawnHerbivore((Vector2)transform.position + Random.insideUnitCircle * 0.5f, childGenome);
            }

            if (energy <= 0f)
            {
                Spawner.DespawnHerbivore(this);
            }
        }

        private Producer FindNearestProducer()
        {
            Producer best = null; var bestD = sightRadius;
            var idx = SpatialIndex.Instance;
            if (idx != null)
            {
                idx.QueryProducers(transform.position, sightRadius, _nearP);
                for (var i = 0; i < _nearP.Count; i++)
                {
                    var p = _nearP[i]; if (p == null)
                    {
                        continue;
                    }

                    var d = Vector2.Distance(transform.position, p.transform.position);
                    if (d < bestD) { bestD = d; best = p; }
                }
                return best;
            }
            foreach (var p in Producer.All)
            {
                var d = Vector2.Distance(transform.position, p.transform.position);
                if (d < bestD) { bestD = d; best = p; }
            }
            return best;
        }
    }
}
