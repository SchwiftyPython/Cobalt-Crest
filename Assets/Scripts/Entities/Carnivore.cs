using System.Collections.Generic;
using Data;
using UnityEngine;

namespace Entities
{
    [RequireComponent(typeof(SpriteRenderer))]
    public class Carnivore : MonoBehaviour
    {
        public static readonly List<Carnivore> All = new List<Carnivore>();

        public AgentGenome genome;

        public float speed = 2.2f;
        public float metabolism = 0.6f;
        public float sightRadius = 6f;

        public float energy = 5f;
        public float attackRange = 0.7f;
        public float attackDamage = 100f;
        public float reproduceThreshold = 10f;
        public float childCost = 4.5f;

        private Vector2 _dir;
        private float _retargetTimer;
        private SpriteRenderer _sr;

        static readonly List<Herbivore> _nearH = new List<Herbivore>(64);

        private void OnEnable()  { All.Add(this);  EcosystemManager.Instance?.Increment("Carnivores"); }
        private void OnDisable() { All.Remove(this); EcosystemManager.Instance?.Decrement("Carnivores"); }

        void Awake()
        {
            _sr = GetComponent<SpriteRenderer>() ?? gameObject.AddComponent<SpriteRenderer>();
            if (genome.speed == 0f) genome = AgentGenome.RandomFor(SpeciesId.Carnivore);
            ApplyGenomeAndConfig();
            _dir = Random.insideUnitCircle.normalized;
        }

        public void SetupOnSpawn(Vector2 pos, AgentGenome g)
        {
            transform.position = pos;
            genome = g;
            ApplyGenomeAndConfig();
            energy = 5f;
            _dir = Random.insideUnitCircle.normalized;
            _retargetTimer = 0f;
        }

        private void ApplyGenomeAndConfig()
        {
            var def = ConfigService.Instance?.GetSpecies(SpeciesId.Carnivore);

            float baseSpeed   = def ? def.speed       : speed;
            float baseMetab   = def ? def.metabolism  : metabolism;
            float baseVision  = def ? def.sightRadius : sightRadius;

            speed      = baseSpeed * Mathf.Max(0.05f, genome.speed);
            metabolism = baseMetab * Mathf.Max(0.01f, genome.metabolism);
            sightRadius= baseVision * Mathf.Max(0.1f,  genome.vision);

            if (def) { attackRange = def.attackRange; attackDamage = def.attackDamage; reproduceThreshold = def.reproduceThreshold; childCost = def.childCost; }

            if (_sr != null)
            {
                _sr.sprite = SpriteFactory.CreateDiscSprite(ConfigService.Instance?.GetSpeciesColor(SpeciesId.Carnivore, Color.red) ?? Color.red, 16);
                _sr.color  = AgentGenome.HueToColor(genome.hue, 0.9f, 1f);
                float sizeMult = Mathf.Clamp(genome.size, 0.4f, 2f);
                transform.localScale = Vector3.one * Mathf.Lerp(0.75f, 1.5f, Mathf.InverseLerp(0.8f, 1.3f, sizeMult));
            }
        }

        void Update()
        {
            float dt = Time.deltaTime * EcosystemManager.SimulationSpeed;
            _retargetTimer -= dt;

            float seekInterval = (ConfigService.Instance?.Sim?.carnivoreSeekInterval) ?? 0.35f;

            if (_retargetTimer <= 0f)
            {
                _retargetTimer = seekInterval;
                var prey = FindNearestHerbivore();
                if (prey != null) _dir = ((Vector2)prey.transform.position - (Vector2)transform.position).normalized;
                else _dir = Vector2.Lerp(_dir, Random.insideUnitCircle.normalized, 0.5f);
            }

            transform.position += (Vector3)(_dir * speed * dt);
            energy -= metabolism * dt;

            var h = FindNearestHerbivore();
            if (h != null && Vector2.Distance(transform.position, h.transform.position) < attackRange)
            {
                Spawner.DespawnHerbivore(h);
                energy += 3.5f;
            }

            if (energy >= reproduceThreshold)
            {
                energy -= childCost;

                var sim   = ConfigService.Instance?.Sim;
                bool evo  = sim != null && sim.useEvolution;
                float ms  = sim != null ? sim.mutationScale : 1f;

                var childGenome = evo ? genome.Mutated(SpeciesId.Carnivore, ms) : genome;
                Spawner.SpawnCarnivore((Vector2)transform.position + Random.insideUnitCircle * 0.5f, childGenome);
            }

            if (energy <= 0f) Spawner.DespawnCarnivore(this);
        }

        private Herbivore FindNearestHerbivore()
        {
            Herbivore best = null; float bestD = sightRadius;
            var idx = SpatialIndex.Instance;
            if (idx != null)
            {
                idx.QueryHerbivores(transform.position, sightRadius, _nearH);
                for (int i = 0; i < _nearH.Count; i++)
                {
                    var h = _nearH[i]; if (h == null) continue;
                    float d = Vector2.Distance(transform.position, h.transform.position);
                    if (d < bestD) { bestD = d; best = h; }
                }
                return best;
            }
            foreach (var h in Herbivore.All)
            {
                float d = Vector2.Distance(transform.position, h.transform.position);
                if (d < bestD) { bestD = d; best = h; }
            }
            return best;
        }
    }
}
