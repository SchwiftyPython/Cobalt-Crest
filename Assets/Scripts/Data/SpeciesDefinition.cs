using UnityEngine;
public enum SpeciesId { Producer, Herbivore, Carnivore }
[System.Serializable] public struct GenomeRange { public Vector2 speed, metabolism, size, vision, hue, mutationSigma; }
[CreateAssetMenu(menuName="Ecosphere/Species Definition", fileName="SpeciesDefinition")]
public class SpeciesDefinition : ScriptableObject {
  public SpeciesId id; public string speciesName="Herbivore"; public Color color=Color.white;
  [Min(0)] public int initialCount=10;
  public GenomeRange genomeRanges;
  public float speed=2f, metabolism=0.4f, sightRadius=5f, reproduceThreshold=8f, childCost=3.5f;
  public float eatRate=1.2f; public float attackRange=0.7f, attackDamage=100f;
  public float growthRate=0.6f, maxBiomass=5f, energyPerBiomass=2f;
}
