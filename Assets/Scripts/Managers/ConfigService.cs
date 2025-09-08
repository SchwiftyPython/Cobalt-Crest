using System.Collections.Generic; using UnityEngine;
public class ConfigService : MonoBehaviour {
  public static ConfigService Instance { get; private set; }
  public ActiveConfig active;
  private readonly Dictionary<SpeciesId, SpeciesDefinition> _speciesById = new();
  public SimulationConfig Sim => active != null ? active.simulation : _fallbackSim;
  public EnvironmentDefinition Env => active != null ? active.environment : _fallbackEnv;
  private SimulationConfig _fallbackSim; private EnvironmentDefinition _fallbackEnv;
  void Awake(){
    if (Instance!=null && Instance!=this){ Destroy(gameObject); return; }
    Instance=this; DontDestroyOnLoad(gameObject);
    if (active==null) active = Resources.Load<ActiveConfig>("ActiveConfig");
    if (active==null){ _fallbackSim = ScriptableObject.CreateInstance<SimulationConfig>(); _fallbackEnv = ScriptableObject.CreateInstance<EnvironmentDefinition>(); }
    RebuildSpeciesMap();
  }
  public SpeciesDefinition GetSpecies(SpeciesId id){ return _speciesById.TryGetValue(id, out var def) ? def : null; }
  public Color GetSpeciesColor(SpeciesId id, Color fallback){ var s=GetSpecies(id); return s? s.color : fallback; }
  private void RebuildSpeciesMap(){ _speciesById.Clear(); if (active!=null && active.species!=null) foreach (var s in active.species) if (s!=null) _speciesById[s.id]=s; }
}
