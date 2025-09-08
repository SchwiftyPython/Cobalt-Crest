using UnityEngine;

/// <summary>Simple environment driver for Phase 1: temperature & rainfall cycles.</summary>
public class EnvironmentManager : MonoBehaviour
{
    [Header("Year Cycle")]
    public float yearLengthSeconds = 120f;
    public AnimationCurve temperatureOverYear = AnimationCurve.EaseInOut(0, 0.5f, 1, 1.0f);
    public AnimationCurve rainfallOverYear = AnimationCurve.EaseInOut(0, 0.6f, 1, 0.4f);

    [Header("Noise")]
    public float perlinScale = 0.1f;

    private float _t;

    public float CurrentTemp { get; private set; } = 0.5f;   // 0..1
    public float CurrentRain { get; private set; } = 0.5f;   // 0..1

    void Update()
    {
        float dt = Time.deltaTime * Mathf.Max(0.0001f, EcosystemManager.SimulationSpeed);
        _t += dt;
        float season = (_t % yearLengthSeconds) / Mathf.Max(0.0001f, yearLengthSeconds);

        CurrentTemp = Mathf.Clamp01(temperatureOverYear.Evaluate(season));
        CurrentRain = Mathf.Clamp01(rainfallOverYear.Evaluate(season));
    }
}
