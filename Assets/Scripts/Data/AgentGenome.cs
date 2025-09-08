using Managers;
using UnityEngine;

namespace Data
{
    [System.Serializable]
    public struct AgentGenome
    {
        public float speed;         // multiplier vs SpeciesDefinition defaults
        public float metabolism;    // multiplier
        public float size;          // multiplier
        public float vision;        // multiplier
        public float hue;           // 0..1 (HSV)
        public float mutationSigma; // per-gene base sigma

        // Draw a random genome inside species ranges
        public static AgentGenome RandomFor(SpeciesId id)
        {
            var def = ConfigService.Instance?.GetSpecies(id);
            var r = def?.genomeRanges ?? DefaultRanges();
            return new AgentGenome
            {
                speed         = Rand(r.speed),
                metabolism    = Rand(r.metabolism),
                size          = Rand(r.size),
                vision        = Rand(r.vision),
                hue           = Rand(r.hue),
                mutationSigma = Rand(r.mutationSigma)
            }.ClampedTo(r);
        }

        // Return a mutated copy (Gaussian noise), then clamp to species ranges
        public AgentGenome Mutated(SpeciesId id, float globalScale = 1f)
        {
            var def = ConfigService.Instance?.GetSpecies(id);
            var r = def?.genomeRanges ?? DefaultRanges();
            var s = Mathf.Max(0f, mutationSigma) * Mathf.Max(0f, globalScale);

            var g = this;
            g.speed         = Mut(speed, s);
            g.metabolism    = Mut(metabolism, s);
            g.size          = Mut(size, s);
            g.vision        = Mut(vision, s);
            g.hue           = Mathf.Repeat(hue + Mut(0f, s * 0.2f), 1f); // keep hue circular, gentler sigma
            g.mutationSigma = Mathf.Max(0.0001f, Mut(mutationSigma, s * 0.25f));
            return g.ClampedTo(r);
        }

        public AgentGenome ClampedTo(GenomeRange r)
        {
            speed         = Mathf.Clamp(speed,      r.speed.x,         r.speed.y);
            metabolism    = Mathf.Clamp(metabolism, r.metabolism.x,    r.metabolism.y);
            size          = Mathf.Clamp(size,       r.size.x,          r.size.y);
            vision        = Mathf.Clamp(vision,     r.vision.x,        r.vision.y);
            hue           = Mathf.Clamp01(hue);
            mutationSigma = Mathf.Clamp(mutationSigma, r.mutationSigma.x, r.mutationSigma.y);
            return this;
        }

        public static Color HueToColor(float hue, float s = 0.85f, float v = 1f)
        {
            return Color.HSVToRGB(Mathf.Repeat(hue, 1f), Mathf.Clamp01(s), Mathf.Clamp01(v));
        }

        // --- helpers ---
        static float Rand(Vector2 range) => Random.Range(range.x, range.y);
        static float Mut(float val, float sigma) => val + Gaussian(0f, sigma);

        // Box–Muller
        static float Gaussian(float mean, float stdDev)
        {
            if (stdDev <= 0f)
            {
                return mean;
            }

            var u1 = 1f - Random.value;
            var u2 = 1f - Random.value;
            
            var z0 = Mathf.Sqrt(-2.0f * Mathf.Log(u1)) * Mathf.Cos(2f * Mathf.PI * u2);
            
            return mean + stdDev * z0;
        }

        static GenomeRange DefaultRanges()
        {
            return new GenomeRange
            {
                speed         = new Vector2(0.8f, 1.2f),
                metabolism    = new Vector2(0.8f, 1.2f),
                size          = new Vector2(0.9f, 1.2f),
                vision        = new Vector2(0.8f, 1.4f),
                hue           = new Vector2(0f, 1f),
                mutationSigma = new Vector2(0.01f, 0.05f)
            };
        }
    }
}
