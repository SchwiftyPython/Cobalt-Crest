#if UNITY_EDITOR
using UnityEditor; using UnityEngine; using System.Linq;
using Data;

[InitializeOnLoad] public static class EcosphereConfigAutoCreator {
  static EcosphereConfigAutoCreator(){ EditorApplication.delayCall += Ensure; }
  static void Ensure(){
    if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling ||
        EditorApplication.isUpdating)
    {
      EditorApplication.delayCall += Ensure; 
      return;
    }
    var resDir = "Assets/Configs/Resources"; if (!System.IO.Directory.Exists(resDir))
    {
      System.IO.Directory.CreateDirectory(resDir);
    }

    var activePath = resDir + "/ActiveConfig.asset"; var active = AssetDatabase.LoadAssetAtPath<ActiveConfig>(activePath); var created=false;
    if (active==null){ active = ScriptableObject.CreateInstance<ActiveConfig>(); AssetDatabase.CreateAsset(active, activePath); created=true; }
    if (active.simulation==null){ var sim = ScriptableObject.CreateInstance<SimulationConfig>(); sim.name="SimulationConfig"; AssetDatabase.AddObjectToAsset(sim, active); active.simulation=sim; created=true; }
    if (active.environment==null){ var env = ScriptableObject.CreateInstance<EnvironmentDefinition>(); env.name="EnvironmentDefinition"; AssetDatabase.AddObjectToAsset(env, active); active.environment=env; created=true; }
    if (active.species==null || active.species.Length<3 || active.species.Any(s=>s==null)){
      var prod = ScriptableObject.CreateInstance<SpeciesDefinition>(); prod.name="Producer"; prod.id=SpeciesId.Producer; prod.speciesName="Producers"; prod.color=Color.green; prod.initialCount=60; prod.growthRate=0.6f; prod.maxBiomass=5f; prod.energyPerBiomass=2f;
      var herb = ScriptableObject.CreateInstance<SpeciesDefinition>(); herb.name="Herbivore"; herb.id=SpeciesId.Herbivore; herb.speciesName="Herbivores"; herb.color=Color.cyan; herb.initialCount=20; herb.speed=1.8f; herb.metabolism=0.4f; herb.eatRate=1.2f; herb.reproduceThreshold=8f; herb.childCost=3.5f; herb.sightRadius=4f;
      var carn = ScriptableObject.CreateInstance<SpeciesDefinition>(); carn.name="Carnivore"; carn.id=SpeciesId.Carnivore; carn.speciesName="Carnivores"; carn.color=Color.red; carn.initialCount=6; carn.speed=2.2f; carn.metabolism=0.6f; carn.attackRange=0.7f; carn.attackDamage=100f; carn.reproduceThreshold=10f; carn.childCost=4.5f; carn.sightRadius=6f;
      AssetDatabase.AddObjectToAsset(prod, active); AssetDatabase.AddObjectToAsset(herb, active); AssetDatabase.AddObjectToAsset(carn, active);
      active.species = new SpeciesDefinition[]{ prod, herb, carn }; created=true;
    }
    if (created){ EditorUtility.SetDirty(active); AssetDatabase.SaveAssets(); AssetDatabase.Refresh(); Debug.Log("[Ecosphere] Config assets ensured at Assets/Configs/Resources/ActiveConfig.asset"); }
  }
}
#endif
