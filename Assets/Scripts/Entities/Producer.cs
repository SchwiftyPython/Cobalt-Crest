using System.Collections.Generic; using UnityEngine;
[RequireComponent(typeof(SpriteRenderer))] public class Producer:MonoBehaviour{
  public static readonly List<Producer> All=new();
  public float growthRate=0.6f, biomass=1f, maxBiomass=5f, energyPerBiomass=2f;
  SpriteRenderer _sr; EnvironmentManager _env;
  void OnEnable(){ All.Add(this); EcosystemManager.Instance?.Increment("Producers"); }
  void OnDisable(){ All.Remove(this); EcosystemManager.Instance?.Decrement("Producers"); }
  void Awake(){
    _sr = GetComponent<SpriteRenderer>();
    if (_sr == null) _sr = gameObject.AddComponent<SpriteRenderer>();

    var color = ConfigService.Instance? ConfigService.Instance.GetSpeciesColor(SpeciesId.Producer, Color.green): Color.green;
    _sr.sprite=SpriteFactory.CreateDiscSprite(color,12); _env=FindObjectOfType<EnvironmentManager>();
    var def = ConfigService.Instance? ConfigService.Instance.GetSpecies(SpeciesId.Producer): null;
    if(def!=null){ growthRate=def.growthRate; maxBiomass=def.maxBiomass; energyPerBiomass=def.energyPerBiomass; }
  }
  void Update(){
    float dt=Time.deltaTime*EcosystemManager.SimulationSpeed; if(_env!=null){
      float envMult=0.5f+0.5f*Mathf.Min(_env.CurrentRain,_env.CurrentTemp);
      biomass=Mathf.Min(maxBiomass, biomass + growthRate*envMult*dt);

      var cfg = ConfigService.Instance;
      var prodColor = (cfg != null) ? cfg.GetSpeciesColor(SpeciesId.Producer, Color.green) : Color.green;
      var t = Mathf.Clamp01(biomass / Mathf.Max(0.0001f, maxBiomass)); // robust against 0 maxBiomass
      
      if (_sr != null)
        _sr.color = Color.Lerp(new Color(0.2f, 0.4f, 0.2f), prodColor, t);
      
      transform.localScale=Vector3.one*(0.5f+0.5f*(biomass/maxBiomass));
    }
  }
  public float Consume(float amount){ float taken=Mathf.Min(amount, biomass); biomass-=taken; return taken*energyPerBiomass; }
}
