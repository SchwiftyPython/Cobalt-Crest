using Data;
using Entities;
using UnityEngine;

namespace Managers
{
    public static class Spawner
    {
        public static Vector2 WorldMin => (ConfigService.Instance?.Sim?.worldMin) ?? new Vector2(-12, -7);
        public static Vector2 WorldMax => (ConfigService.Instance?.Sim?.worldMax) ?? new Vector2(12, 7);

        // Pooling toggle + safety (only use pool if the flag is on AND the service exists)
        private static bool UsePooling => (ConfigService.Instance?.Sim?.usePooling) ?? false;
        private static bool Pooled => UsePooling && PoolingService.Instance != null;

        public static void SpawnInitial()
        {
            var cfg = ConfigService.Instance;
            var prodN = cfg?.GetSpecies(SpeciesId.Producer)?.initialCount ?? 60;
            var herbN = cfg?.GetSpecies(SpeciesId.Herbivore)?.initialCount ?? 20;
            var carnN = cfg?.GetSpecies(SpeciesId.Carnivore)?.initialCount ?? 6;

            for (var i = 0; i < prodN; i++) SpawnProducer(RandomPos());
            for (var i = 0; i < herbN; i++) SpawnHerbivore(RandomPos());
            for (var i = 0; i < carnN; i++) SpawnCarnivore(RandomPos());
        }

        public static void SpawnProducer(Vector2 at, AgentGenome? genome = null)
        {
            if (Pooled)
            {
                PoolingService.Instance.SpawnProducer(at, genome);
                return;
            }

            var go = new GameObject("Producer");
            var c = go.AddComponent<Producer>();
            c.SetupOnSpawn(at, genome ?? AgentGenome.RandomFor(SpeciesId.Producer));
        }

        public static void SpawnHerbivore(Vector2 at, AgentGenome? genome = null)
        {
            if (Pooled)
            {
                PoolingService.Instance.SpawnHerbivore(at, genome);
                return;
            }

            var go = new GameObject("Herbivore");
            var c = go.AddComponent<Herbivore>();
            c.SetupOnSpawn(at, genome ?? AgentGenome.RandomFor(SpeciesId.Herbivore));
        }

        public static void SpawnCarnivore(Vector2 at, AgentGenome? genome = null)
        {
            if (Pooled)
            {
                PoolingService.Instance.SpawnCarnivore(at, genome);
                return;
            }

            var go = new GameObject("Carnivore");
            var c = go.AddComponent<Carnivore>();
            c.SetupOnSpawn(at, genome ?? AgentGenome.RandomFor(SpeciesId.Carnivore));
        }


        // ✅ Despawn helpers
        public static void DespawnProducer(Producer p)
        {
            if (p == null)
            {
                return;
            }

            if (Pooled)
            {
                PoolingService.Instance.Despawn(p);
            }
            else
            {
                Object.Destroy(p.gameObject);
            }
        }

        public static void DespawnHerbivore(Herbivore h)
        {
            if (h == null)
            {
                return;
            }

            if (Pooled)
            {
                PoolingService.Instance.Despawn(h);
            }
            else
            {
                Object.Destroy(h.gameObject);
            }
        }

        public static void DespawnCarnivore(Carnivore c)
        {
            if (c == null)
            {
                return;
            }

            if (Pooled)
            {
                PoolingService.Instance.Despawn(c);
            }
            else
            {
                Object.Destroy(c.gameObject);
            }
        }

        static Vector2 RandomPos()
            => new Vector2(Random.Range(WorldMin.x, WorldMax.x), Random.Range(WorldMin.y, WorldMax.y));
    }
}
