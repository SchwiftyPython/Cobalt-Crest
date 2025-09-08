using System.Collections.Generic;
using UnityEngine;

/// <summary>Global simulation controller: simulation speed and per-species counts.</summary>
public class EcosystemManager : MonoBehaviour
{
    public static EcosystemManager Instance { get; private set; }

    /// <summary>Global simulation speed multiplier (0=paused, 1=normal, >1 fast).</summary>
    public static float SimulationSpeed
    {
        get => _simSpeed;
        set => _simSpeed = Mathf.Max(0f, value);
    }
    private static float _simSpeed = 1f;

    private readonly Dictionary<string, int> _counts = new Dictionary<string, int> {
        { "Producers", 0 }, { "Herbivores", 0 }, { "Carnivores", 0 }
    };

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void Increment(string speciesId)
    {
        if (string.IsNullOrEmpty(speciesId)) return;
        if (!_counts.ContainsKey(speciesId)) _counts[speciesId] = 0;
        _counts[speciesId]++;
    }

    public void Decrement(string speciesId)
    {
        if (string.IsNullOrEmpty(speciesId)) return;
        if (!_counts.ContainsKey(speciesId)) _counts[speciesId] = 0;
        _counts[speciesId] = Mathf.Max(0, _counts[speciesId] - 1);
    }

    public int GetCount(string speciesId)
    {
        if (string.IsNullOrEmpty(speciesId)) return 0;
        return _counts.TryGetValue(speciesId, out var v) ? v : 0;
    }
}
