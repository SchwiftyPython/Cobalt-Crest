using System.Collections.Generic;
using Data;
using Managers;
using UnityEngine;
using Utils;
using World;

namespace Entities
{
    [RequireComponent(typeof(SpriteRenderer))]
    public class Producer : MonoBehaviour
    {
        public static readonly List<Producer> All = new List<Producer>();

        public AgentGenome genome; // use hue/size only for now

        public float growthRate = 0.6f;
        public float biomass = 1.0f;
        public float maxBiomass = 5.0f;
        public float energyPerBiomass = 2.0f;

        private SpriteRenderer sr;
        private EnvironmentManager env;

        private void OnEnable()  { All.Add(this);  EcosystemManager.Instance?.Increment("Producers"); }
        private void OnDisable() { All.Remove(this); EcosystemManager.Instance?.Decrement("Producers"); }

        void Awake()
        {
            sr = GetComponent<SpriteRenderer>() ?? gameObject.AddComponent<SpriteRenderer>();
            // Defaults
            // Check multiple fields to determine if genome is uninitialized
            if (genome.hue == 0f && genome.size == 0f && genome.speed == 0f)
            {
                genome = AgentGenome.RandomFor(SpeciesId.Producer);
            }

            var def = ConfigService.Instance ? ConfigService.Instance.GetSpecies(SpeciesId.Producer) : null;
            if (def != null)
            {
                growthRate = def.growthRate;
                maxBiomass = def.maxBiomass;
                energyPerBiomass = def.energyPerBiomass;
            }

            var baseColor = ConfigService.Instance ? ConfigService.Instance.GetSpeciesColor(SpeciesId.Producer, Color.green) : Color.green;
            sr.sprite = SpriteFactory.CreateDiscSprite(baseColor, 12);
            sr.color  = AgentGenome.HueToColor(genome.hue, 0.7f, 0.95f);

            var sizeMult = Mathf.Clamp(genome.size, 0.5f, 1.6f);
            transform.localScale = Vector3.one * Mathf.Lerp(0.6f, 1.3f, Mathf.InverseLerp(0.8f, 1.3f, sizeMult));

            env = FindObjectOfType<EnvironmentManager>();
        }

        public void SetupOnSpawn(Vector2 pos, AgentGenome g)
        {
            transform.position = pos;
            genome = g;
            // refresh visuals quickly
            var baseColor = ConfigService.Instance ? ConfigService.Instance.GetSpeciesColor(SpeciesId.Producer, Color.green) : Color.green;
            if (sr == null)
            {
                sr = gameObject.AddComponent<SpriteRenderer>();
            }

            sr.sprite = SpriteFactory.CreateDiscSprite(baseColor, 12);
            sr.color  = AgentGenome.HueToColor(genome.hue, 0.7f, 0.95f);

            var sizeMult = Mathf.Clamp(genome.size, 0.5f, 1.6f);
            transform.localScale = Vector3.one * Mathf.Lerp(0.6f, 1.3f, Mathf.InverseLerp(0.8f, 1.3f, sizeMult));
        }

        void Update()
        {
            if (env == null)
            {
                env = FindObjectOfType<EnvironmentManager>();
            }
            
            var dt = Time.deltaTime * EcosystemManager.SimulationSpeed;
            if (env != null)
            {
                var envMult = 0.5f + 0.5f * Mathf.Min(env.CurrentRain, env.CurrentTemp);

                var shelterMult = 1f;
                var grid = ShelterGrid.Instance;
                if (grid != null && grid.Enabled)
                    shelterMult = grid.GetProducerGrowthMult(transform.position);

                biomass = Mathf.Min(maxBiomass, biomass + growthRate * envMult * shelterMult * dt);

                if (biomass <= 0.05)
                {
                    Spawner.DespawnProducer(this);
                }

                if (sr != null)
                {
                    var baseColor = AgentGenome.HueToColor(genome.hue, 0.7f, 0.95f);
                    sr.color = Color.Lerp(new Color(0.2f, 0.4f, 0.2f), baseColor, biomass / Mathf.Max(0.001f, maxBiomass));
                }
            }

        }

        public float Consume(float amount)
        {
            var taken = Mathf.Min(amount, biomass);
            biomass -= taken;
            return taken * energyPerBiomass;
        }
    }
}
