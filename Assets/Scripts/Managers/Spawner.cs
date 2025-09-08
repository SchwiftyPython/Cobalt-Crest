using Data;
using Entities;
using UnityEngine;

public static class Spawner
{
    public static Vector2 worldMin => (ConfigService.Instance?.Sim?.worldMin) ?? new Vector2(-12, -7);
    public static Vector2 worldMax => (ConfigService.Instance?.Sim?.worldMax) ?? new Vector2(12, 7);

    // Pooling toggle + safety (only use pool if the flag is on AND the service exists)
    static bool UsePooling => (ConfigService.Instance?.Sim?.usePooling) ?? false;
    static bool Pooled => UsePooling && PoolingService.Instance != null;

    public static void SpawnInitial()
    {
        var cfg = ConfigService.Instance;
        int prodN = cfg?.GetSpecies(SpeciesId.Producer)?.initialCount ?? 60;
        int herbN = cfg?.GetSpecies(SpeciesId.Herbivore)?.initialCount ?? 20;
        int carnN = cfg?.GetSpecies(SpeciesId.Carnivore)?.initialCount ?? 6;

        for (int i = 0; i < prodN; i++) SpawnProducer(RandomPos());
        for (int i = 0; i < herbN; i++) SpawnHerbivore(RandomPos());
        for (int i = 0; i < carnN; i++) SpawnCarnivore(RandomPos());
    }

    public static void SpawnProducer(Vector2 at, AgentGenome? genome = null)
    {
        if (Pooled) { PoolingService.Instance.SpawnProducer(at, genome); return; }
        var go = new GameObject("Producer");
        var c = go.AddComponent<Producer>();
        c.SetupOnSpawn(at, genome ?? AgentGenome.RandomFor(SpeciesId.Producer));
    }

    public static void SpawnHerbivore(Vector2 at, AgentGenome? genome = null)
    {
        if (Pooled) { PoolingService.Instance.SpawnHerbivore(at, genome); return; }
        var go = new GameObject("Herbivore");
        var c = go.AddComponent<Herbivore>();
        c.SetupOnSpawn(at, genome ?? AgentGenome.RandomFor(SpeciesId.Herbivore));
    }

    public static void SpawnCarnivore(Vector2 at, AgentGenome? genome = null)
    {
        if (Pooled) { PoolingService.Instance.SpawnCarnivore(at, genome); return; }
        var go = new GameObject("Carnivore");
        var c = go.AddComponent<Carnivore>();
        c.SetupOnSpawn(at, genome ?? AgentGenome.RandomFor(SpeciesId.Carnivore));
    }


    // ✅ Despawn helpers
    public static void DespawnProducer(Producer p)
    {
        if (p == null) return;
        if (Pooled) PoolingService.Instance.Despawn(p);
        else Object.Destroy(p.gameObject);
    }

    public static void DespawnHerbivore(Herbivore h)
    {
        if (h == null) return;
        if (Pooled) PoolingService.Instance.Despawn(h);
        else Object.Destroy(h.gameObject);
    }

    public static void DespawnCarnivore(Carnivore c)
    {
        if (c == null) return;
        if (Pooled) PoolingService.Instance.Despawn(c);
        else Object.Destroy(c.gameObject);
    }

    static Vector2 RandomPos()
        => new Vector2(Random.Range(worldMin.x, worldMax.x), Random.Range(worldMin.y, worldMax.y));
}
