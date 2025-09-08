using UnityEngine;

/// <summary>Spawns initial populations and provides helpers for reproduction.</summary>
public static class Spawner
{
    public static int initialProducers = 600;
    public static int initialHerbivores = 20;
    public static int initialCarnivores = 1;

    public static Vector2 worldMin = new Vector2(-12, -7);
    public static Vector2 worldMax = new Vector2(12, 7);

    public static void SpawnInitial()
    {
        for (int i=0;i<initialProducers;i++) SpawnProducer(RandomPos());
        for (int i=0;i<initialHerbivores;i++) SpawnHerbivore(RandomPos());
        for (int i=0;i<initialCarnivores;i++) SpawnCarnivore(RandomPos());
    }

    public static void SpawnProducer(Vector2 at)
    {
        var go = new GameObject("Producer");
        go.transform.position = at;
        go.AddComponent<Producer>();
    }

    public static void SpawnHerbivore(Vector2 at)
    {
        var go = new GameObject("Herbivore");
        go.transform.position = at;
        go.AddComponent<Herbivore>();
    }

    public static void SpawnCarnivore(Vector2 at)
    {
        var go = new GameObject("Carnivore");
        go.transform.position = at;
        go.AddComponent<Carnivore>();
    }

    private static Vector2 RandomPos()
    {
        return new Vector2(Random.Range(worldMin.x, worldMax.x), Random.Range(worldMin.y, worldMax.y));
    }
}
