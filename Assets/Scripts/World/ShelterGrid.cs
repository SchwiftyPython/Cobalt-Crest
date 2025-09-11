using Managers;
using UnityEngine;

namespace World
{
    /// World shelters: a lightweight scalar field (0..1) over the world that
    /// influences movement and producer growth. Updated via a simple CA at a fixed interval.
    public class ShelterGrid : MonoBehaviour
    {
        public static ShelterGrid Instance { get; private set; }
        public bool Enabled => ConfigService.Instance?.Sim?.sheltersEnabled ?? false;

        public int Width { get; private set; }
        public int Height { get; private set; }
        public float CellSize { get; private set; }
        public Vector2 WorldMin { get; private set; }
        public Vector2 WorldMax { get; private set; }

        private float[,] a; // current field (0..1)
        private float[,] b; // next field
        private float timer;
        public int FrameStamp { get; private set; } // increments when grid changes

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            BuildGrid();
        }

        private void OnEnable()
        {
            BuildGrid();
        }

        private void OnDisable()
        {
        }

        private void Update()
        {
            if (!Enabled) return;
            var sim = ConfigService.Instance?.Sim;
            var stepInterval = sim?.shelterStepInterval ?? 0.5f;

            timer += Time.unscaledDeltaTime;
            if (timer >= stepInterval)
            {
                timer = 0f;
                StepAutomata(); // writes into _b then swaps
                FrameStamp++;
            }
        }

        private void BuildGrid()
        {
            var sim = ConfigService.Instance?.Sim;
            WorldMin = sim?.worldMin ?? new Vector2(-12, -7);
            WorldMax = sim?.worldMax ?? new Vector2(12, 7);
            CellSize = Mathf.Max(0.5f, sim?.shelterCellSize ?? 2f);

            Width = Mathf.Max(2, Mathf.CeilToInt((WorldMax.x - WorldMin.x) / CellSize));
            Height = Mathf.Max(2, Mathf.CeilToInt((WorldMax.y - WorldMin.y) / CellSize));

            a = new float[Width, Height];
            b = new float[Width, Height];
            SeedInitial();
            FrameStamp++;
        }

        private void SeedInitial()
        {
            var sim = ConfigService.Instance?.Sim;
            var fill = Mathf.Clamp01(sim?.shelterInitialFill ?? 0.2f);

            // random seed + soft Perlin bias so it forms bands, not pure salt-and-pepper
            var perlinScale = 0.25f / Mathf.Max(0.1f, CellSize);
            var pBias = 0.35f;

            for (var y = 0; y < Height; y++)
            for (var x = 0; x < Width; x++)
            {
                var nx = (x + WorldMin.x) * perlinScale;
                var ny = (y + WorldMin.y) * perlinScale;
                var n = Mathf.PerlinNoise(nx, ny) * 0.7f + pBias;
                var r = Random.value;
                a[x, y] = r * n < fill ? Random.Range(0.35f, 0.8f) : 0f;
            }
        }

        // --- Public sampling API ---------------------------------------------------

        public float Sample01(Vector2 worldPos)
        {
            // bilinear sample in grid space (clamped)
            var gx = Mathf.InverseLerp(WorldMin.x, WorldMax.x, worldPos.x) * (Width - 1);
            var gy = Mathf.InverseLerp(WorldMin.y, WorldMax.y, worldPos.y) * (Height - 1);

            var x0 = Mathf.Clamp(Mathf.FloorToInt(gx), 0, Width - 1);
            var y0 = Mathf.Clamp(Mathf.FloorToInt(gy), 0, Height - 1);
            var x1 = Mathf.Min(x0 + 1, Width - 1);
            var y1 = Mathf.Min(y0 + 1, Height - 1);

            var tx = Mathf.Clamp01(gx - x0);
            var ty = Mathf.Clamp01(gy - y0);

            var aSample = a[x0, y0];
            var bSample = a[x1, y0];
            var cSample = a[x0, y1];
            var dSample = a[x1, y1];

            var u = Mathf.Lerp(aSample, bSample, tx);
            var v = Mathf.Lerp(cSample, dSample, tx);
            return Mathf.Lerp(u, v, ty);
        }

        /// Gradient (∂density/∂x, ∂density/∂y) in world space approx. Useful for steering away from dense shelters.
        public Vector2 SampleGradient(Vector2 worldPos)
        {
            var eps = CellSize * 0.5f;
            var fx1 = Sample01(new Vector2(worldPos.x + eps, worldPos.y));
            var fx0 = Sample01(new Vector2(worldPos.x - eps, worldPos.y));
            var fy1 = Sample01(new Vector2(worldPos.x, worldPos.y + eps));
            var fy0 = Sample01(new Vector2(worldPos.x, worldPos.y - eps));
            
            // convert from cell-space difference to normalized world gradient
            var g = new Vector2((fx1 - fx0) / (2f * eps), (fy1 - fy0) / (2f * eps));
            if (g.sqrMagnitude > 1f) g = g.normalized;
            return g;
        }

        public float GetMovementCost(Vector2 worldPos)
        {
            var sim = ConfigService.Instance?.Sim;
            var d = Sample01(worldPos);
            var maxCost = Mathf.Max(1f, sim?.movementCostInShelter ?? 1.5f);
            
            // linear blend: 1 at d=0, maxCost at d=1
            return Mathf.Lerp(1f, maxCost, d);
        }

        public float GetProducerGrowthMult(Vector2 worldPos)
        {
            var sim = ConfigService.Instance?.Sim;
            var d = Sample01(worldPos);
            var target = sim?.producerGrowthInShelter ?? 1.2f; // <1 = penalty, >1 = buff
            return Mathf.Lerp(1f, target, d);
        }

        // --- CA step ---------------------------------------------------------------

        private void StepAutomata()
        {
            var sim = ConfigService.Instance?.Sim;
            var spread = sim?.shelterSpread ?? 0.18f; // diffusion/aggregation strength
            var decay = sim?.shelterDecay ?? 0.03f; // natural decay
            var noise = sim?.shelterNoise ?? 0.00f; // random re-seeding per step
            var maxAdd = 0.25f; // cap growth contribution per step

            for (var y = 0; y < Height; y++)
            for (var x = 0; x < Width; x++)
            {
                var cur = a[x, y];
                var nAvg = NeighbourAverage(x, y);

                // Tend toward neighbours (clustering), with logistic-ish growth & decay
                var grow = Mathf.Clamp01(nAvg - 0.45f) * maxAdd; // only if neighbourhood is reasonably full
                var next = cur + spread * (nAvg - cur) + grow - decay * cur;

                if (noise > 0f && Random.value < noise) next = Mathf.Max(next, Random.Range(0.2f, 0.6f));
                b[x, y] = Mathf.Clamp01(next);
            }

            // swap
            (a, b) = (b, a);
        }

        private float NeighbourAverage(int cx, int cy)
        {
            // Moore (8-neighbour) average including self at half weight
            var sum = a[cx, cy] * 0.5f;
            var count = 1; // weighted count
            for (var y = -1; y <= 1; y++)
            for (var x = -1; x <= 1; x++)
            {
                if (x == 0 && y == 0) continue;
                int ix = cx + x, iy = cy + y;
                if (ix < 0 || iy < 0 || ix >= Width || iy >= Height) continue;
                sum += a[ix, iy];
                count++;
            }

            return sum / Mathf.Max(1, count);
        }
    }
}