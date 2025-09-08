using Data;
using UnityEngine;
[CreateAssetMenu(menuName="Ecosphere/Active Config", fileName="ActiveConfig")]
public class ActiveConfig : ScriptableObject {
  public SimulationConfig simulation; public EnvironmentDefinition environment; public SpeciesDefinition[] species;
}
