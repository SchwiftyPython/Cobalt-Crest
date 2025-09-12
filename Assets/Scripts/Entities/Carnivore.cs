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
            if (genome.speed == 0f)
            {
                genome = AgentGenome.RandomFor(SpeciesId.Carnivore);
            }

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

            var baseSpeed   = def ? def.speed       : speed;
            var baseMetab   = def ? def.metabolism  : metabolism;
            var baseVision  = def ? def.sightRadius : sightRadius;

            speed      = baseSpeed * Mathf.Max(0.05f, genome.speed);
            metabolism = baseMetab * Mathf.Max(0.01f, genome.metabolism);
            sightRadius= baseVision * Mathf.Max(0.1f,  genome.vision);

            if (def) { attackRange = def.attackRange; attackDamage = def.attackDamage; reproduceThreshold = def.reproduceThreshold; childCost = def.childCost; }

            if (_sr != null)
            {
                // Keep the white sprite so tint does the coloring
                _sr.sprite = SpriteFactory.CreateDiscSprite(Color.white, 16);

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
                _sr.color = Color.HSVToRGB(mappedHue, s, v);
                
                var sizeMult = Mathf.Clamp(genome.size, 0.4f, 2f);
                transform.localScale = Vector3.one * Mathf.Lerp(0.75f, 1.5f, Mathf.InverseLerp(0.8f, 1.3f, sizeMult));
            }
        }

        void Update()
        {
            var dt = Time.deltaTime * EcosystemManager.SimulationSpeed;
            _retargetTimer -= dt;

            var seekInterval = (ConfigService.Instance?.Sim?.carnivoreSeekInterval) ?? 0.35f;

            if (_retargetTimer <= 0f)
            {
                _retargetTimer = seekInterval;
                var prey = FindNearestHerbivore();
                if (prey != null)
                {
                    _dir = ((Vector2)prey.transform.position - (Vector2)transform.position).normalized;
                }
                else
                {
                    _dir = Vector2.Lerp(_dir, Random.insideUnitCircle.normalized, 0.5f);
                }
            }
            
            energy -= metabolism * dt;
            
            // --- Shelter steering (avoid dense regions) ---
            var grid = ShelterGrid.Instance;
            if (grid != null && grid.Enabled)
            {
                var avoid = (ConfigService.Instance?.Sim?.movementAvoidStrength) ?? 0.6f;
                if (avoid > 0f)
                {
                    var grad = grid.SampleGradient(transform.position);
                    _dir = (_dir - grad * avoid).normalized;
                }
            }

            // --- Movement with cost ---
            var cost = 1f;
            if (grid != null && grid.Enabled) cost = grid.GetMovementCost(transform.position);
            transform.position += (Vector3)(_dir * (speed / Mathf.Max(0.001f, cost)) * dt);
            
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
                var evo  = sim != null && sim.useEvolution;
                var ms  = sim != null ? sim.mutationScale : 1f;

                var childGenome = evo ? genome.Mutated(SpeciesId.Carnivore, ms) : genome;
                Spawner.SpawnCarnivore((Vector2)transform.position + Random.insideUnitCircle * 0.5f, childGenome);
            }

            if (energy <= 0f)
            {
                Spawner.DespawnCarnivore(this);
            }
        }

        private Herbivore FindNearestHerbivore()
        {
            Herbivore best = null; var bestD = sightRadius;
            var idx = SpatialIndex.Instance;
            if (idx != null)
            {
                idx.QueryHerbivores(transform.position, sightRadius, _nearH);
                for (var i = 0; i < _nearH.Count; i++)
                {
                    var h = _nearH[i]; if (h == null)
                    {
                        continue;
                    }

                    var d = Vector2.Distance(transform.position, h.transform.position);
                    if (d < bestD) { bestD = d; best = h; }
                }
                return best;
            }
            foreach (var h in Herbivore.All)
            {
                var d = Vector2.Distance(transform.position, h.transform.position);
                if (d < bestD) { bestD = d; best = h; }
            }
            return best;
        }
    }
}
