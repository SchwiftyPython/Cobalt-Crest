using System.Collections.Generic;
using Data;
using Managers;
using UnityEngine;

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

        private SpriteRenderer _sr;
        private EnvironmentManager _env;

        private void OnEnable()  { All.Add(this);  EcosystemManager.Instance?.Increment("Producers"); }
        private void OnDisable() { All.Remove(this); EcosystemManager.Instance?.Decrement("Producers"); }

        void Awake()
        {
            _sr = GetComponent<SpriteRenderer>() ?? gameObject.AddComponent<SpriteRenderer>();
            // Defaults
            if (genome.speed == 0f)
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
            _sr.sprite = SpriteFactory.CreateDiscSprite(baseColor, 12);
            _sr.color  = AgentGenome.HueToColor(genome.hue, 0.7f, 0.95f);

            var sizeMult = Mathf.Clamp(genome.size, 0.5f, 1.6f);
            transform.localScale = Vector3.one * Mathf.Lerp(0.6f, 1.3f, Mathf.InverseLerp(0.8f, 1.3f, sizeMult));

            _env = Object.FindObjectOfType<EnvironmentManager>();
        }

        public void SetupOnSpawn(Vector2 pos, AgentGenome g)
        {
            transform.position = pos;
            genome = g;
            // refresh visuals quickly
            var baseColor = ConfigService.Instance ? ConfigService.Instance.GetSpeciesColor(SpeciesId.Producer, Color.green) : Color.green;
            if (_sr == null)
            {
                _sr = gameObject.AddComponent<SpriteRenderer>();
            }

            _sr.sprite = SpriteFactory.CreateDiscSprite(baseColor, 12);
            _sr.color  = AgentGenome.HueToColor(genome.hue, 0.7f, 0.95f);

            var sizeMult = Mathf.Clamp(genome.size, 0.5f, 1.6f);
            transform.localScale = Vector3.one * Mathf.Lerp(0.6f, 1.3f, Mathf.InverseLerp(0.8f, 1.3f, sizeMult));
        }

        void Update()
        {
            var dt = Time.deltaTime * EcosystemManager.SimulationSpeed;
            if (_env == null)
            {
                _env = Object.FindObjectOfType<EnvironmentManager>();
            }

            if (_env != null)
            {
                var envMult = 0.5f + 0.5f * Mathf.Min(_env.CurrentRain, _env.CurrentTemp);
                biomass = Mathf.Min(maxBiomass, biomass + growthRate * envMult * dt);

                // tint deeper as biomass grows
                if (_sr != null)
                {
                    var baseColor = AgentGenome.HueToColor(genome.hue, 0.7f, 0.95f);
                    _sr.color = Color.Lerp(new Color(0.2f, 0.4f, 0.2f), baseColor, biomass / Mathf.Max(0.001f, maxBiomass));
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
