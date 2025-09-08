using System.Collections.Generic;
using UnityEngine;

/// <summary>Producers accumulate biomass based on rainfall & temperature.</summary>
[RequireComponent(typeof(SpriteRenderer))]
public class Producer : MonoBehaviour
{
    public static readonly List<Producer> All = new List<Producer>();

    [Header("Growth")]
    public float growthRate = 0.6f; // base growth per second at ideal env
    public float biomass = 1.0f;
    public float maxBiomass = 5.0f;

    private SpriteRenderer _sr;
    private EnvironmentManager _env;

    private void OnEnable()
    {
        All.Add(this);
        EcosystemManager.Instance?.Increment("Producers");
    }

    private void OnDisable()
    {
        All.Remove(this);
        if (EcosystemManager.Instance != null) EcosystemManager.Instance.Decrement("Producers");
    }

    void Awake()
    {
        _sr = GetComponent<SpriteRenderer>();
        _sr.sprite = SpriteFactory.CreateDiscSprite(Color.green, 12);
        _env = Object.FindObjectOfType<EnvironmentManager>();
    }

    void Update()
    {
        float dt = Time.deltaTime * EcosystemManager.SimulationSpeed;
        if (_env != null)
        {
            float envMult = 0.5f + 0.5f * Mathf.Min(_env.CurrentRain, _env.CurrentTemp); // 0.5..1
            biomass = Mathf.Min(maxBiomass, biomass + growthRate * envMult * dt);
            _sr.color = Color.Lerp(new Color(0.2f,0.4f,0.2f), Color.green, biomass / maxBiomass);
            transform.localScale = Vector3.one * (0.5f + 0.5f * (biomass / maxBiomass));
        }
    }

    /// <summary>Consume biomass and return energy gained.</summary>
    public float Consume(float amount)
    {
        float taken = Mathf.Min(amount, biomass);
        biomass -= taken;
        return taken * 2.0f; // energy per biomass unit
    }
}
